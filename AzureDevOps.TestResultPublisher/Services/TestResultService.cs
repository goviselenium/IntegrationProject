using AzureDevOps.TestResultPublisher.Models;
using System.Threading;
using System.Threading.Tasks;

namespace AzureDevOps.TestResultPublisher.Services
{
    internal sealed class TestResultService
    {
        private readonly AzureDevOpsClient _client;

        public TestResultService(AzureDevOpsClient client)
        {
            _client = client;
        }

        public async Task<TestCaseResult> AddOrUpdateResultAsync(int runId, int testPointId, AutomationTestResult result, CancellationToken cancellationToken)
        {
            var outcome = ResultMapper.ToAzureDevOpsOutcome(result.ExecutionStatus);
            var body = new[]
            {
                new TestCaseResultUpdateModel
                {
                    State = "Completed",
                    Outcome = outcome,
                    ErrorMessage = result.ErrorMessage,
                    StackTrace = result.StackTrace,
                    StartedDate = result.StartedDate,
                    CompletedDate = result.CompletedDate,
                    DurationInMs = result.DurationInMs,
                    TestCase = new ShallowReference { Id = result.TestCaseId.ToString(), Name = result.TestCaseTitle },
                    PointIds = new[] { testPointId },
                    AutomatedTestName = result.AutomationTestName,
                    Comment = $"Browser={result.BrowserName}; Environment={result.EnvironmentName}; Build={result.BuildNumber}; Release={result.ReleaseName}"
                }
            };

            var response = await _client.PatchAsync<AzureListResponse<TestCaseResult>>($"_apis/test/Runs/{runId}/results?api-version=7.1", body, cancellationToken).ConfigureAwait(false);
            if (response.Value.Count == 0)
            {
                throw new AzureDevOpsApiException("PATCH", $"_apis/test/Runs/{runId}/results", 200, "Azure DevOps returned no updated test result.");
            }

            return response.Value[0];
        }
    }
}
