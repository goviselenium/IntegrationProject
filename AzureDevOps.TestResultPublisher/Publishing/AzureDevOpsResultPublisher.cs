using AzureDevOps.TestResultPublisher.Configuration;
using AzureDevOps.TestResultPublisher.Models;
using AzureDevOps.TestResultPublisher.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AzureDevOps.TestResultPublisher.Publishing
{
    public sealed class AzureDevOpsResultPublisher : IDisposable
    {
        private readonly AzureDevOpsConfig _config;
        private readonly ILogger _logger;
        private readonly AzureDevOpsClient _client;
        private readonly TestPointService _testPointService;
        private readonly TestRunService _testRunService;
        private readonly TestResultService _testResultService;
        private readonly AttachmentService _attachmentService;

        public AzureDevOpsResultPublisher(AzureDevOpsConfig config, ILogger logger = null)
        {
            _config = config;
            _config.Validate();
            _logger = logger ?? NullLogger.Instance;
            _client = new AzureDevOpsClient(config, _logger);
            _testPointService = new TestPointService(config, _client, _logger);
            _testRunService = new TestRunService(config, _client);
            _testResultService = new TestResultService(config, _client);
            _attachmentService = new AttachmentService(_client, _logger);
        }

        public async Task<PublishedTestResult> PublishAsync(AutomationTestResult result, CancellationToken cancellationToken = default)
        {
            ValidateResult(result);
            EnrichFromConfig(result);

            if (result.ExecutionStatus == AutomationExecutionStatus.Skipped && !_config.PublishSkippedTests)
            {
                _logger.LogInformation("Skipping Azure DevOps publish for skipped test {TestName}", result.AutomationTestName);
                return null;
            }

            var point = await _testPointService.GetByTestCaseIdAsync(result.TestCaseId, cancellationToken).ConfigureAwait(false);
            var run = await _testRunService.CreateRunAsync(result, point.Id, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Created Azure DevOps test run {RunId}", run.Id);

            TestCaseResult publishedResult = null;
            try
            {
                publishedResult = await _testResultService.AddResultAsync(run.Id, point, result, cancellationToken).ConfigureAwait(false);

                if (result.ExecutionStatus == AutomationExecutionStatus.Failed)
                {
                    await _attachmentService.AttachFileAsync(run.Id, publishedResult.Id, result.ScreenshotPath, "Failure screenshot", cancellationToken).ConfigureAwait(false);
                }

                if (_config.UpdateTestPointOutcome)
                {
                    await _testPointService.UpdateOutcomeAsync(point, ResultMapper.ToAzureDevOpsOutcome(result.ExecutionStatus), cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("Updated Azure DevOps test point {PointId} outcome to {Outcome}", point.Id, ResultMapper.ToAzureDevOpsOutcome(result.ExecutionStatus));
                }

                await TryCompleteRunAsync(run.Id, $"Completed by automation. Outcome={publishedResult.Outcome}", cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Published {Outcome} result to Azure DevOps run {RunId}, result {ResultId}, point {PointId}", publishedResult.Outcome, run.Id, publishedResult.Id, point.Id);

                return new PublishedTestResult
                {
                    RunId = run.Id,
                    ResultId = publishedResult.Id,
                    TestPointId = point.Id,
                    Outcome = publishedResult.Outcome
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure DevOps publishing failed for run {RunId}.", run.Id);
                throw;
            }
        }

        private async Task TryCompleteRunAsync(int runId, string comment, CancellationToken cancellationToken)
        {
            if (!_config.CompleteTestRun)
            {
                _logger.LogInformation("Azure DevOps test run {RunId} was left open because completeTestRun is false.", runId);
                return;
            }

            try
            {
                await _testRunService.CompleteRunAsync(runId, comment, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not complete Azure DevOps test run {RunId}. The published result may still be available on the run.", runId);
            }
        }

        private void ValidateResult(AutomationTestResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.TestCaseId <= 0) throw new InvalidOperationException("Automation result must include a valid Azure DevOps testCaseId.");
            if (result.StartedDate == default) result.StartedDate = DateTimeOffset.UtcNow;
            if (result.CompletedDate == default) result.CompletedDate = DateTimeOffset.UtcNow;
            if (result.DurationInMs <= 0) result.DurationInMs = (long)(result.CompletedDate - result.StartedDate).TotalMilliseconds;
        }

        private void EnrichFromConfig(AutomationTestResult result)
        {
            if (string.IsNullOrWhiteSpace(result.EnvironmentName)) result.EnvironmentName = _config.EnvironmentName;
            if (string.IsNullOrWhiteSpace(result.BuildNumber)) result.BuildNumber = _config.BuildNumber;
            if (string.IsNullOrWhiteSpace(result.ReleaseName)) result.ReleaseName = _config.ReleaseName;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
