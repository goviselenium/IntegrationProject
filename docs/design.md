# Azure DevOps Manual Test Case Result Publisher

## Goal

Publish Selenium or Playwright automation execution results back to existing Azure DevOps manual test cases in a Test Plan and Test Suite.

The publisher:

1. Reads organization, project, PAT, plan ID, suite ID, and execution metadata from configuration and environment variables.
2. Finds the Azure DevOps Test Point ID for a given manual Test Case ID.
3. Creates an automated test run linked to the Test Plan.
4. Updates the run result as Passed, Failed, or NotApplicable.
5. Uploads failure screenshots as result attachments.
6. Optionally updates the Test Point outcome.
7. Optionally completes the test run when `completeTestRun` is enabled.

## Folder Structure

```text
AzureDevOpsTestResultIntegration.sln
AzureDevOps.TestResultPublisher/
  Configuration/
    AzureDevOpsConfig.cs
  Models/
    AzureDevOpsModels.cs
    ExecutionModels.cs
  Publishing/
    AzureDevOpsResultPublisher.cs
  Services/
    AttachmentService.cs
    AzureDevOpsClient.cs
    ResultMapper.cs
    TestPointService.cs
    TestResultService.cs
    TestRunService.cs
AzureDevOps.TestResultPublisher.Samples/
  appsettings.json
  NUnitHooks/
    AzureDevOpsTestCaseIdAttribute.cs
    NUnitResultPublisher.cs
  Selenium/
    SeleniumNUnitBase.cs
    SampleSeleniumTests.cs
  Playwright/
    PlaywrightNUnitBase.cs
    SamplePlaywrightTests.cs
azure-pipelines.yml
docs/
  design.md
  troubleshooting.md
```

## Test Case ID vs Test Point ID

A Test Case ID is the work item ID of the manual test case. It identifies the test definition.

A Test Point ID is the executable instance of that test case inside a specific Test Plan, Test Suite, configuration, and tester assignment. The same Test Case ID can appear in multiple suites or configurations, so Azure DevOps needs the Test Point ID to know exactly which executable slot should receive the result.

That is why the publisher first calls the Test Plan Test Point API with `testPlanId`, `testSuiteId`, and `testCaseId`, then uses the matched point ID when updating run results.

## Official REST APIs Used

Microsoft Learn references:

- Test points list: `GET https://dev.azure.com/{organization}/{project}/_apis/testplan/Plans/{planId}/Suites/{suiteId}/TestPoint?testCaseId={testCaseId}&api-version=7.1`
- Create test run: `POST https://dev.azure.com/{organization}/{project}/_apis/test/runs?api-version=7.1`
- Add test results: `POST https://dev.azure.com/{organization}/{project}/_apis/test/Runs/{runId}/results?api-version=7.1`
- Create result attachment: `POST https://dev.azure.com/{organization}/{project}/_apis/test/Runs/{runId}/Results/{resultId}/attachments?api-version=7.1`
- Complete test run: `PATCH https://dev.azure.com/{organization}/{project}/_apis/test/runs/{runId}?api-version=7.1`
- Update test point outcome: `PATCH https://dev.azure.com/{organization}/{project}/_apis/testplan/Plans/{planId}/Suites/{suiteId}/TestPoint?api-version=7.1`

## Functional Flow

1. Add `[AzureDevOpsTestCaseId(12345)]` to the NUnit test method.
2. The Selenium or Playwright base fixture records `StartedDate` in `[SetUp]`.
3. After the test, `[TearDown]` reads NUnit outcome, error message, stack trace, and duration.
4. On failed tests, the hook saves a screenshot.
5. `NUnitResultPublisher` builds an `AutomationTestResult`.
6. `AzureDevOpsResultPublisher.PublishAsync` fetches the Test Point.
7. A Test Run is created against the Test Plan.
8. The result is added with planned-run fields: `testPointId`, `testCaseId`, `testCaseReferenceId`, `testCaseRevision`, `testCaseTitle`, `testPlanId`, `testSuiteId`, `testPoint`, `testCase`, dates, duration, outcome, and diagnostic fields.
9. Failure screenshot is uploaded as a result attachment.
10. The point outcome is optionally updated.
11. The run is completed only when `completeTestRun` is `true`.

## Configuration

Use `AzureDevOps.TestResultPublisher.Samples/appsettings.json` as the template. Keep secrets out of JSON:

```json
{
  "azureDevOps": {
    "organization": "your-organization",
    "project": "your-project",
    "personalAccessToken": "env:AZDO_PAT",
    "testPlanId": 100,
    "testSuiteId": 200,
    "environmentName": "QA",
    "buildNumber": "",
    "releaseName": "",
    "maxRetryAttempts": 3,
    "retryDelayMs": 1000,
    "publishSkippedTests": true,
    "updateTestPointOutcome": true,
    "completeTestRun": false
  }
}
```

Set these environment variables in CI:

```powershell
$env:AZDO_PAT = "<secret PAT>"
$env:AZDO_PUBLISH_RESULTS = "true"
```

## Implementation Steps

1. Copy `AzureDevOps.TestResultPublisher` into the existing automation solution.
2. Reference it from the Selenium or Playwright NUnit project.
3. Copy the sample `NUnitHooks` folder or adapt it to the existing hook/listener layer.
4. Add `[AzureDevOpsTestCaseId(<manual test case work item id>)]` to each automation test.
5. Configure `appsettings.json` with organization, project, plan, and suite.
6. Store PAT as a secret variable named `AZDO_PAT`.
7. Run tests with `AZDO_PUBLISH_RESULTS=true`.

## Production Notes

- The PAT is resolved from `AZDO_PAT` first, then `env:<name>` values in JSON.
- Retries are applied for 408, 429, and 5xx responses.
- Missing test point validation is explicit and includes plan, suite, and test case IDs.
- Failed API responses include status code and response body for faster CI troubleshooting.
- Replace the sample test case IDs and target application URL before running against a real Azure DevOps project.
