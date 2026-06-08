# Troubleshooting Guide

## No Test Point Found

Error:

```text
No Azure DevOps test point was found for testCaseId ...
```

Check:

- The manual test case is inside the configured Test Suite.
- `testPlanId` and `testSuiteId` are from the same Azure DevOps project.
- The automation attribute uses the Test Case work item ID, not the Test Point ID.
- The test case is not only present in a different configuration or child suite.

## 401 or 403

Check:

- `AZDO_PAT` exists on the agent.
- The PAT has Test Management read/write permissions. Microsoft describes this as `vso.test_write` for OAuth scopes; PATs need equivalent Test Plans access.
- The user behind the PAT has permission to view and update the Test Plan.
- The PAT has not expired.

## Result Updates But Attachment Is Missing

Check:

- Screenshot path exists before publishing.
- The failing test actually entered the screenshot branch.
- Attachment payload size is acceptable for the service.
- The result ID returned by the result update call is used in the attachment URL.

## Result Appears On A New Run Only

This implementation creates one automated run per published test. That is simple and reliable for independent tests. If you prefer a single suite-level run, create the run in `[OneTimeSetUp]`, share the run ID, publish each result into it, then complete the run in `[OneTimeTearDown]`.

## Skipped Tests

Skipped tests are mapped to Azure DevOps `NotApplicable`. Set `publishSkippedTests` to `false` to avoid publishing skipped tests.

## Playwright Browser Install

If Playwright tests fail before publishing because browsers are missing, run:

```powershell
pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps
```

In CI, use the Playwright install step in `azure-pipelines.yml`.

## Security Best Practices

- Do not commit PATs to `appsettings.json`.
- Store PATs as secret pipeline variables or in Azure Key Vault.
- Use the shortest feasible PAT expiration.
- Scope the PAT only to test plan/result write needs.
- Rotate the PAT regularly.
- Never log the PAT. This implementation does not print request authorization headers.
- Prefer a dedicated service account so audit logs are clear.
