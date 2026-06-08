using AzureDevOps.TestResultPublisher.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AzureDevOps.TestResultPublisher.Services
{
    internal sealed class AttachmentService
    {
        private readonly AzureDevOpsClient _client;
        private readonly ILogger _logger;

        public AttachmentService(AzureDevOpsClient client, ILogger logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task AttachFileAsync(int runId, int resultId, string path, string comment, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (!File.Exists(path))
            {
                _logger.LogWarning("Attachment path does not exist: {Path}", path);
                return;
            }

            var bytes = await ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            var body = new TestResultAttachmentRequest
            {
                Stream = Convert.ToBase64String(bytes),
                FileName = Path.GetFileName(path),
                Comment = comment
            };

            await _client.PostAsync<string>($"_apis/test/Runs/{runId}/Results/{resultId}/attachments?api-version=7.1", body, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Uploaded attachment {FileName} to run {RunId}, result {ResultId}", body.FileName, runId, resultId);
        }

        private static async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
            {
                var buffer = new byte[stream.Length];
                var offset = 0;
                while (offset < buffer.Length)
                {
                    var read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken).ConfigureAwait(false);
                    if (read == 0) break;
                    offset += read;
                }
                return buffer;
            }
        }
    }
}
