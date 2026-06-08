using AzureDevOps.TestResultPublisher.Configuration;
using AzureDevOps.TestResultPublisher.Models;
using Microsoft.Extensions.Logging;
using System;
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
            var url = $"_apis/testplan/Plans/{_config.TestPlanId}/Suites/{_config.TestSuiteId}/TestPoint?testCaseId={testCaseId}&includePointDetails=true&api-version=7.1";
            var response = await _client.GetAsync<AzureListResponse<TestPoint>>(url, cancellationToken).ConfigureAwait(false);
            var point = response.Value.FirstOrDefault(p => string.Equals(p.TestCase?.Id, testCaseId.ToString(), StringComparison.OrdinalIgnoreCase));

            if (point == null)
            {
                throw new InvalidOperationException($"No Azure DevOps test point was found for testCaseId {testCaseId} in plan {_config.TestPlanId}, suite {_config.TestSuiteId}. Verify the manual test case is assigned to the suite.");
            }

            _logger.LogInformation("Matched Azure DevOps testCaseId {TestCaseId} to testPointId {TestPointId}", testCaseId, point.Id);
            return point;
        }

        public Task UpdateOutcomeAsync(int testPointId, string outcome, CancellationToken cancellationToken)
        {
            var url = $"_apis/testplan/Plans/{_config.TestPlanId}/Suites/{_config.TestSuiteId}/TestPoint?api-version=7.1";
            var body = new[] { new PointUpdateModel { Id = testPointId, Outcome = outcome } };
            return _client.PatchAsync<string>(url, body, cancellationToken);
        }
    }
}
