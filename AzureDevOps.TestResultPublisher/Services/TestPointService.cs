using AzureDevOps.TestResultPublisher.Configuration;
using AzureDevOps.TestResultPublisher.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureDevOps.TestResultPublisher.Services
{
    internal sealed class TestPointService
    {
        private readonly AzureDevOpsConfig _config;
        private readonly AzureDevOpsClient _client;
        private readonly ILogger _logger;

        public TestPointService(AzureDevOpsConfig config, AzureDevOpsClient client, ILogger logger)
        {
            _config = config;
            _client = client;
            _logger = logger;
        }

        public async Task<TestPoint> GetByTestCaseIdAsync(int testCaseId, CancellationToken cancellationToken)
        {
            var url = $"_apis/testplan/Plans/{_config.TestPlanId}/Suites/{_config.TestSuiteId}/TestPoint?testCaseId={testCaseId}&includePointDetails=true&isRecursive=true&api-version=7.1";
            var response = await _client.GetAsync<AzureListResponse<TestPoint>>(url, cancellationToken).ConfigureAwait(false);
            var point = response.Value.FirstOrDefault(p => string.Equals(GetTestCaseId(p), testCaseId.ToString(), StringComparison.OrdinalIgnoreCase));

            if (point == null && response.Value.Count == 1)
            {
                point = response.Value[0];
                _logger.LogWarning(
                    "Azure DevOps returned one test point for testCaseId {TestCaseId}, but the response did not expose a matching test case reference. Using returned testPointId {TestPointId}.",
                    testCaseId,
                    point.Id);
            }

            if (point == null)
            {
                var discovered = string.Join(", ", response.Value.Select(DescribePoint));
                if (string.IsNullOrWhiteSpace(discovered))
                {
                    discovered = "none";
                }

                throw new InvalidOperationException($"No Azure DevOps test point was found for testCaseId {testCaseId} in plan {_config.TestPlanId}, suite {_config.TestSuiteId}. Discovered points: {discovered}. Verify the manual test case is assigned to this suite or one of its child suites, and that the attribute uses the manual Test Case work item ID.");
            }

            _logger.LogInformation("Matched Azure DevOps testCaseId {TestCaseId} to testPointId {TestPointId}", testCaseId, point.Id);
            return point;
        }

        private static string GetTestCaseId(TestPoint point)
        {
            return point.TestCase?.Id ?? point.TestCaseReference?.Id;
        }

        private static string DescribePoint(TestPoint point)
        {
            return $"pointId={point.Id}, testCaseId={GetTestCaseId(point) ?? "unknown"}, suiteId={GetSuiteId(point) ?? "unknown"}";
        }

        private static string GetSuiteId(TestPoint point)
        {
            return point.TestSuite?.Id ?? point.Suite?.Id;
        }

        public Task UpdateOutcomeAsync(TestPoint point, string outcome, CancellationToken cancellationToken)
        {
            var suiteId = GetSuiteId(point) ?? _config.TestSuiteId.ToString(CultureInfo.InvariantCulture);
            var url = $"_apis/testplan/Plans/{_config.TestPlanId}/Suites/{suiteId}/TestPoint?api-version=7.1";
            var body = new[] { new PointUpdateModel { Id = point.Id, Results = new PointResultsUpdateModel { Outcome = outcome } } };
            return _client.PatchAsync<string>(url, body, cancellationToken);
        }
    }
}
