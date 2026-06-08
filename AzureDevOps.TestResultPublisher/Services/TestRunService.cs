using AzureDevOps.TestResultPublisher.Configuration;
using AzureDevOps.TestResultPublisher.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AzureDevOps.TestResultPublisher.Services
{
    internal sealed class TestRunService
    {
        private readonly AzureDevOpsConfig _config;
        private readonly AzureDevOpsClient _client;

        public TestRunService(AzureDevOpsConfig config, AzureDevOpsClient client)
        {
            _config = config;
            _client = client;
        }

        public Task<TestRun> CreateRunAsync(AutomationTestResult result, int testPointId, CancellationToken cancellationToken)
        {
            var runName = $"Automated UI Results - Plan {_config.TestPlanId} - {DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
            var body = new RunCreateModel
            {
                Name = runName,
                Automated = true,
                IsAutomated = true,
                Plan = new ShallowReference { Id = _config.TestPlanId.ToString() },
                StartedDate = result.StartedDate,
                BuildNumber = Coalesce(result.BuildNumber, _config.BuildNumber),
                Release = string.IsNullOrWhiteSpace(Coalesce(result.ReleaseName, _config.ReleaseName))
                    ? null
                    : new ReleaseReference { Name = Coalesce(result.ReleaseName, _config.ReleaseName) },
                Comment = $"Created by automation for test point {testPointId}. Browser={result.BrowserName}; Environment={Coalesce(result.EnvironmentName, _config.EnvironmentName)}"
            };

            return _client.PostAsync<TestRun>("_apis/test/runs?api-version=7.1", body, cancellationToken);
        }

        public Task<TestRun> CompleteRunAsync(int runId, string comment, CancellationToken cancellationToken)
        {
            var body = new TestRunUpdateModel
            {
                State = "Completed",
                CompletedDate = DateTimeOffset.UtcNow,
                Comment = comment
            };

            return _client.PatchAsync<TestRun>($"_apis/test/runs/{runId}?api-version=7.1", body, cancellationToken);
        }

        private static string Coalesce(string primary, string fallback)
        {
            return string.IsNullOrWhiteSpace(primary) ? fallback : primary;
        }
    }
}
