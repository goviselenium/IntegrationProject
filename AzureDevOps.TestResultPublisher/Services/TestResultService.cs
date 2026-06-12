using AzureDevOps.TestResultPublisher.Configuration;
using AzureDevOps.TestResultPublisher.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureDevOps.TestResultPublisher.Services
{
    internal sealed class TestResultService
    {
        private readonly AzureDevOpsConfig _config;
        private readonly AzureDevOpsClient _client;

        public TestResultService(AzureDevOpsConfig config, AzureDevOpsClient client)
        {
            _config = config;
            _client = client;
        }

        public async Task<TestCaseResult> AddResultAsync(int runId, TestPoint point, AutomationTestResult result, CancellationToken cancellationToken)
        {
            var outcome = ResultMapper.ToAzureDevOpsOutcome(result.ExecutionStatus);
            var testCaseTitle = ResolveTestCaseTitle(point, result);
            var testCaseId = result.TestCaseId;
            var testCaseRevision = ResolveTestCaseRevision(point);
            var body = new[]
            {
                new PlannedTestResultModel
                {
                    State = "Completed",
                    Outcome = outcome,
                    ErrorMessage = result.ErrorMessage,
                    StackTrace = result.StackTrace,
                    StartedDate = result.StartedDate,
                    CompletedDate = result.CompletedDate,
                    DurationInMs = result.DurationInMs,
                    TestPointId = point.Id,
                    TestCaseId = testCaseId,
                    TestCaseReferenceId = testCaseId,
                    TestCaseRevision = testCaseRevision,
                    TestCaseTitle = testCaseTitle,
                    TestPlanId = _config.TestPlanId,
                    TestSuiteId = ResolveIntReference(point.TestSuite?.Id ?? point.Suite?.Id, _config.TestSuiteId),
                    TestPoint = new ShallowReference { Id = point.Id.ToString(CultureInfo.InvariantCulture) },
                    TestCase = new ShallowReference { Id = testCaseId.ToString(CultureInfo.InvariantCulture), Name = testCaseTitle },
                    AutomatedTestName = result.AutomationTestName,
                    Comment = $"Browser={result.BrowserName}; Environment={result.EnvironmentName}; Build={result.BuildNumber}; Release={result.ReleaseName}"
                }
            };

            var response = await _client.PostAsync<AzureListResponse<TestCaseResult>>($"_apis/test/Runs/{runId}/results?api-version=7.1", body, cancellationToken).ConfigureAwait(false);
            if (response.Value.Count == 0)
            {
                throw new AzureDevOpsApiException("POST", $"_apis/test/Runs/{runId}/results", 200, "Azure DevOps returned no added test result.");
            }

            return response.Value[0];
        }

        private static int ResolveIntReference(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
                ? parsed
                : fallback;
        }

        private static int ResolveTestCaseRevision(TestPoint point)
        {
            var revision = ResolveWorkItemProperty(point, "System.Rev");
            if (int.TryParse(revision, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedRevision) && parsedRevision > 0)
            {
                return parsedRevision;
            }

            return point.Revision > 0 ? point.Revision : 1;
        }

        private static string ResolveTestCaseTitle(TestPoint point, AutomationTestResult result)
        {
            return point.TestCase?.Name
                ?? point.TestCaseReference?.Name
                ?? ResolveWorkItemProperty(point, "System.Title")
                ?? result.TestCaseTitle
                ?? result.AutomationTestName
                ?? "";
        }

        private static string ResolveWorkItemProperty(TestPoint point, string key)
        {
            var property = point.WorkItemProperties?.FirstOrDefault(p =>
                string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.WorkItem?.Key, key, StringComparison.OrdinalIgnoreCase));

            return property?.Value ?? property?.WorkItem?.Value;
        }
    }
}
