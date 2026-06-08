using AzureDevOps.TestResultPublisher.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AzureDevOps.TestResultPublisher.Services
{
    internal sealed class AzureDevOpsClient : IDisposable
    {
        private readonly AzureDevOpsConfig _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private bool _disposeClient;

        public AzureDevOpsClient(AzureDevOpsConfig config, ILogger logger, HttpClient httpClient = null)
        {
            _config = config;
            _logger = logger;
            _httpClient = httpClient ?? new HttpClient();
            _disposeClient = httpClient == null;
            _httpClient.BaseAddress = config.BaseUri;

            var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{config.PersonalAccessToken}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", token);
        }

        public Task<T> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken)
        {
            return SendAsync<T>(HttpMethod.Get, relativeUrl, null, cancellationToken);
        }

        public Task<T> PostAsync<T>(string relativeUrl, object body, CancellationToken cancellationToken)
        {
            return SendAsync<T>(HttpMethod.Post, relativeUrl, body, cancellationToken);
        }

        public Task<T> PatchAsync<T>(string relativeUrl, object body, CancellationToken cancellationToken)
        {
            return SendAsync<T>(new HttpMethod("PATCH"), relativeUrl, body, cancellationToken);
        }

        private async Task<T> SendAsync<T>(HttpMethod method, string relativeUrl, object body, CancellationToken cancellationToken)
        {
            Exception lastException = null;

            for (var attempt = 1; attempt <= _config.MaxRetryAttempts; attempt++)
            {
                using (var request = new HttpRequestMessage(method, relativeUrl))
                {
                    if (body != null)
                    {
                        var json = JsonSerializer.Serialize(body, JsonDefaults.Options);
                        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    }

                    try
                    {
                        _logger.LogInformation("{Method} {Url} attempt {Attempt}", method, relativeUrl, attempt);
                        using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                        {
                            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (response.IsSuccessStatusCode)
                            {
                                if (typeof(T) == typeof(string))
                                {
                                    return (T)(object)responseText;
                                }

                                return JsonSerializer.Deserialize<T>(responseText, JsonDefaults.Options);
                            }

                            if (!ShouldRetry(response.StatusCode) || attempt == _config.MaxRetryAttempts)
                            {
                                throw new AzureDevOpsApiException(method.Method, relativeUrl, (int)response.StatusCode, responseText);
                            }
                        }
                    }
                    catch (Exception ex) when (!(ex is AzureDevOpsApiException) && attempt < _config.MaxRetryAttempts)
                    {
                        lastException = ex;
                        _logger.LogWarning(ex, "Azure DevOps request failed, retrying.");
                    }
                }

                await Task.Delay(ComputeDelay(attempt), cancellationToken).ConfigureAwait(false);
            }

            throw new InvalidOperationException("Azure DevOps request failed after retries.", lastException);
        }

        private bool ShouldRetry(HttpStatusCode statusCode)
        {
            var code = (int)statusCode;
            return code == 408 || code == 429 || code >= 500;
        }

        private TimeSpan ComputeDelay(int attempt)
        {
            return TimeSpan.FromMilliseconds(_config.RetryDelayMs * attempt);
        }

        public void Dispose()
        {
            if (_disposeClient)
            {
                _httpClient.Dispose();
            }
        }
    }

    public sealed class AzureDevOpsApiException : Exception
    {
        public AzureDevOpsApiException(string method, string url, int statusCode, string responseBody)
            : base($"Azure DevOps API call failed: {method} {url} returned HTTP {statusCode}. Body: {responseBody}")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        public int StatusCode { get; }
        public string ResponseBody { get; }
    }
}
