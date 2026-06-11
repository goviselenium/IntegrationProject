# User Guide: Azure DevOps Test Result Publisher

## Overview

The Azure DevOps Test Result Publisher sends automated Selenium or Playwright NUnit test results to existing manual test cases in Azure DevOps Test Plans.

Use this guide when you want to:

- Link an automated NUnit test to a manual Azure DevOps test case.
- Publish Passed, Failed, or Skipped results into Azure DevOps Test Plans.
- Attach failure screenshots to Azure DevOps test results.
- Run the publishing flow locally or from an Azure DevOps pipeline.

## Prerequisites

Before using the publisher, make sure you have:

- An Azure DevOps organization and project.
- A Test Plan and Test Suite containing the manual test cases you want to update.
- The manual Test Case work item IDs.
- A Personal Access Token with permission to read and update Test Plans and test results.
- .NET SDK installed.
- Chrome available for Selenium tests, or Playwright browsers installed for Playwright tests.

## Project Layout

The solution contains two main projects:

```text
AzureDevOps.TestResultPublisher
AzureDevOps.TestResultPublisher.Samples
```

`AzureDevOps.TestResultPublisher` contains the reusable publishing logic.

`AzureDevOps.TestResultPublisher.Samples` contains sample NUnit integrations for Selenium and Playwright.

## Configure Azure DevOps

Open:

```text
AzureDevOps.TestResultPublisher.Samples/appsettings.json
```

Update the `azureDevOps` section:

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
    "updateTestPointOutcome": true
  }
}
```

### Configuration Fields

| Field | Description |
| --- | --- |
| `organization` | Azure DevOps organization name. |
| `project` | Azure DevOps project name. |
| `personalAccessToken` | PAT value or environment variable reference. Use `env:AZDO_PAT`. |
| `testPlanId` | ID of the Azure DevOps Test Plan. |
| `testSuiteId` | ID of the Azure DevOps Test Suite. |
| `environmentName` | Optional label such as `QA`, `UAT`, or `Production`. |
| `buildNumber` | Optional build number. In CI this is usually read from `BUILD_BUILDNUMBER`. |
| `releaseName` | Optional release name. In CI this is usually read from `RELEASE_RELEASENAME`. |
| `maxRetryAttempts` | Number of retry attempts for transient Azure DevOps API failures. |
| `retryDelayMs` | Delay between retries in milliseconds. |
| `publishSkippedTests` | When `true`, skipped NUnit tests are published as `NotApplicable`. |
| `updateTestPointOutcome` | When `true`, the Azure DevOps Test Point outcome is updated after publishing. |

## Store The PAT Securely

Do not commit a real PAT into `appsettings.json`.

For local execution, set the environment variable:

```powershell
$env:AZDO_PAT = "<your-pat>"
```

To enable publishing locally, also set:

```powershell
$env:AZDO_PUBLISH_RESULTS = "true"
```

If `AZDO_PUBLISH_RESULTS` is not set to `true`, the tests still run but publishing is skipped.

## Link An Automated Test To Azure DevOps

Add the `AzureDevOpsTestCaseId` attribute to the NUnit test method.

```csharp
[Test]
[AzureDevOpsTestCaseId(12345)]
public void HomePageTitle_IsDisplayed()
{
    Driver.Navigate().GoToUrl("https://example.com");
    Assert.That(Driver.Title, Does.Contain("Example"));
}
```

Use the manual Test Case work item ID, not the Test Point ID.

The publisher finds the correct Test Point automatically by using:

- `testPlanId`
- `testSuiteId`
- `testCaseId`

## Run Locally

From the solution root, restore and build:

```powershell
dotnet restore AzureDevOpsTestResultIntegration.sln
dotnet build AzureDevOpsTestResultIntegration.sln
```

Set the publishing variables:

```powershell
$env:AZDO_PAT = "<your-pat>"
$env:AZDO_PUBLISH_RESULTS = "true"
```

Run the sample tests:

```powershell
dotnet test AzureDevOps.TestResultPublisher.Samples/AzureDevOps.TestResultPublisher.Samples.csproj --logger trx
```

Before running against a real Azure DevOps project, replace the sample test case IDs with real Azure DevOps test case IDs.

## Run Playwright Tests

Install Playwright browsers before running Playwright tests:

```powershell
pwsh AzureDevOps.TestResultPublisher.Samples/bin/Debug/net10.0/playwright.ps1 install chromium
```

Then run:

```powershell
dotnet test AzureDevOps.TestResultPublisher.Samples/AzureDevOps.TestResultPublisher.Samples.csproj --logger trx
```

## Run In Azure DevOps Pipeline

Create a secret pipeline variable named:

```text
AZDO_PAT
```

The included `azure-pipelines.yml` already:

- Restores the solution.
- Builds the solution.
- Installs Playwright Chromium.
- Runs NUnit tests.
- Passes `AZDO_PAT`, `AZDO_PUBLISH_RESULTS`, and `BUILD_BUILDNUMBER` to the test process.
- Publishes the pipeline TRX file.

The key pipeline environment settings are:

```yaml
env:
  AZDO_PAT: $(AZDO_PAT)
  AZDO_PUBLISH_RESULTS: $(AZDO_PUBLISH_RESULTS)
  BUILD_BUILDNUMBER: $(Build.BuildNumber)
```

## Check Published Results

After the test run completes:

1. Open Azure DevOps.
2. Go to Test Plans.
3. Open the configured Test Plan.
4. Open the configured Test Suite.
5. Find the linked manual test case.
6. Verify that the latest outcome is updated.
7. For failed tests, open the result and check the attached screenshot.

## Result Mapping

| NUnit Result | Azure DevOps Result |
| --- | --- |
| Passed | Passed |
| Failed | Failed |
| Skipped | NotApplicable |

Skipped tests are only published when `publishSkippedTests` is `true`.

## Failure Screenshots

For failed tests:

- Selenium captures a PNG screenshot from the active browser.
- Playwright captures a full-page PNG screenshot.
- The screenshot is uploaded as an Azure DevOps test result attachment.

Screenshots are stored under the test work directory before upload.

## Common Issues

### Publishing Is Skipped

Check that this variable is set:

```powershell
$env:AZDO_PUBLISH_RESULTS = "true"
```

### PersonalAccessToken Is Required

Check that `AZDO_PAT` exists:

```powershell
$env:AZDO_PAT
```

Also confirm `appsettings.json` uses:

```json
"personalAccessToken": "env:AZDO_PAT"
```

### No Test Point Was Found

Check that:

- The test case is in the configured Test Suite.
- If the configured suite is a parent suite, the test case is in that suite or one of its child suites.
- `testPlanId` is correct.
- `testSuiteId` is correct.
- The attribute uses the manual Test Case work item ID.
- The test case belongs to the same Azure DevOps project configured in `appsettings.json`.
- If the error says `Discovered points: none`, Azure DevOps did not return any point for that plan, suite, and test case ID combination.

### Test Run Is Created But Manual Test Case Is Not Updated

Check that:

- `updateTestPointOutcome` is set to `true`.
- The test output contains `Updated Azure DevOps test point ... outcome to ...`.
- You are viewing the same Test Plan and Test Suite configured in `appsettings.json`.
- The test case has only one matching point in the configured suite, or you are checking the same configuration/tester point that was matched in the log.

### 401 Or 403 From Azure DevOps

Check that:

- The PAT has not expired.
- The PAT has Test Plans read/write access.
- The PAT owner can access the configured Test Plan and Test Suite.
- The pipeline secret variable is named `AZDO_PAT`.

## Recommended User Workflow

1. Create or identify the manual test cases in Azure DevOps.
2. Add those test cases to the required Test Suite.
3. Update `appsettings.json` with organization, project, plan ID, and suite ID.
4. Store the PAT in `AZDO_PAT`.
5. Add `[AzureDevOpsTestCaseId(<id>)]` to each NUnit test.
6. Run tests locally with `AZDO_PUBLISH_RESULTS=true`.
7. Verify results in Azure DevOps Test Plans.
8. Move the same settings into the Azure DevOps pipeline.

## Related Documentation

- `docs/design.md` explains the internal publishing flow.
- `docs/troubleshooting.md` lists detailed failure checks.
- `azure-pipelines.yml` shows the CI setup.
