using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureDevOps.TestResultPublisher.Configuration
{
    public sealed class AzureDevOpsConfig
    {
        public string Organization { get; set; } = "";
        public string Project { get; set; } = "";
        public string PersonalAccessToken { get; set; } = "";
        public int TestPlanId { get; set; }
        public int TestSuiteId { get; set; }
        public string EnvironmentName { get; set; } = "";
        public string BuildNumber { get; set; } = "";
        public string ReleaseName { get; set; } = "";
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        public bool PublishSkippedTests { get; set; } = true;
        public bool UpdateTestPointOutcome { get; set; } = true;

        [JsonIgnore]
        public Uri BaseUri => new Uri($"https://dev.azure.com/{Organization}/{Project}/");

        public static AzureDevOpsConfig Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Azure DevOps configuration file was not found.", path);
            }

            var json = File.ReadAllText(path);
            var root = JsonSerializer.Deserialize<AppSettingsRoot>(json, JsonDefaults.Options);
            var config = root?.AzureDevOps ?? throw new InvalidOperationException("Missing AzureDevOps section in configuration.");
            config.PersonalAccessToken = ResolvePat(config.PersonalAccessToken);
            config.Validate();
            return config;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Organization)) throw new InvalidOperationException("Organization is required.");
            if (string.IsNullOrWhiteSpace(Project)) throw new InvalidOperationException("Project is required.");
            if (string.IsNullOrWhiteSpace(PersonalAccessToken)) throw new InvalidOperationException("PersonalAccessToken is required. Prefer the AZDO_PAT environment variable.");
            if (TestPlanId <= 0) throw new InvalidOperationException("TestPlanId must be greater than zero.");
            if (TestSuiteId <= 0) throw new InvalidOperationException("TestSuiteId must be greater than zero.");
            if (MaxRetryAttempts < 1) MaxRetryAttempts = 1;
            if (RetryDelayMs < 0) RetryDelayMs = 0;
        }

        private static string ResolvePat(string configuredValue)
        {
            var envPat = Environment.GetEnvironmentVariable("AZDO_PAT");
            if (!string.IsNullOrWhiteSpace(envPat))
            {
                return envPat;
            }

            if (string.IsNullOrWhiteSpace(configuredValue))
            {
                return "";
            }

            const string envPrefix = "env:";
            if (configuredValue.StartsWith(envPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var envName = configuredValue.Substring(envPrefix.Length);
                return Environment.GetEnvironmentVariable(envName) ?? "";
            }

            return configuredValue;
        }
    }

    internal sealed class AppSettingsRoot
    {
        public AzureDevOpsConfig AzureDevOps { get; set; }
    }

    internal static class JsonDefaults
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        static JsonDefaults()
        {
            Options.Converters.Add(new FlexibleStringConverter());
        }
    }

    internal sealed class FlexibleStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out var longValue))
                {
                    return longValue.ToString(CultureInfo.InvariantCulture);
                }

                return reader.GetDouble().ToString(CultureInfo.InvariantCulture);
            }

            if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
            {
                return reader.GetBoolean().ToString();
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            throw new JsonException($"Cannot convert token type {reader.TokenType} to string.");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
