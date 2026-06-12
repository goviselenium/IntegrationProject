using AzureDevOps.TestResultPublisher.Configuration;
using AzureDevOps.TestResultPublisher.Models;
using AzureDevOps.TestResultPublisher.Publishing;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AzureDevOps.TestResultPublisher.Samples.NUnitHooks
{
    public static class NUnitResultPublisher
    {
        private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddSimpleConsole());
        private static readonly ILogger Logger = LoggerFactory.CreateLogger("AzureDevOpsPublisher");

        public static async Task PublishCurrentTestAsync(DateTimeOffset startedDate, string screenshotPath, string browserName)
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("AZDO_PUBLISH_RESULTS"), "true", StringComparison.OrdinalIgnoreCase))
            {
                TestContext.Progress.WriteLine("AZDO_PUBLISH_RESULTS is not true. Azure DevOps publishing skipped.");
                return;
            }

            var testCaseId = ResolveTestCaseId();
            if (testCaseId <= 0)
            {
                throw new InvalidOperationException("Add [AzureDevOpsTestCaseId(<id>)] to publish this NUnit test.");
            }

            var completedDate = DateTimeOffset.UtcNow;
            var result = TestContext.CurrentContext.Result;
            var automationResult = new AutomationTestResult
            {
                TestCaseId = testCaseId,
                TestCaseTitle = TestContext.CurrentContext.Test.Name,
                AutomationTestName = TestContext.CurrentContext.Test.FullName,
                ExecutionStatus = FromNUnit(result.Outcome.Status),
                ErrorMessage = result.Message ?? "",
                StackTrace = result.StackTrace ?? "",
                ScreenshotPath = screenshotPath ?? "",
                StartedDate = startedDate,
                CompletedDate = completedDate,
                DurationInMs = (long)(completedDate - startedDate).TotalMilliseconds,
                BrowserName = browserName,
                BuildNumber = Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER") ?? "",
                ReleaseName = Environment.GetEnvironmentVariable("RELEASE_RELEASENAME") ?? ""
            };

            var configPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "appsettings.json");
            TestContext.Progress.WriteLine($"Loading Azure DevOps config from: {configPath}");
            var config = AzureDevOpsConfig.Load(configPath);

            using (var publisher = new AzureDevOpsResultPublisher(config, Logger))
            {
                await publisher.PublishAsync(automationResult).ConfigureAwait(false);
            }
        }

        private static int ResolveTestCaseId()
        {
            var methodName = TestContext.CurrentContext.Test.MethodName;
            var fixtureTypeName = TestContext.CurrentContext.Test.ClassName;
            if (string.IsNullOrWhiteSpace(methodName) || string.IsNullOrWhiteSpace(fixtureTypeName))
            {
                return 0;
            }

            var fixtureType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(fixtureTypeName, false))
                .FirstOrDefault(t => t != null);

            var method = fixtureType?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return method?.GetCustomAttribute<AzureDevOpsTestCaseIdAttribute>(true)?.TestCaseId
                ?? fixtureType?.GetCustomAttribute<AzureDevOpsTestCaseIdAttribute>(true)?.TestCaseId
                ?? 0;
        }

        private static AutomationExecutionStatus FromNUnit(TestStatus status)
        {
            if (status == TestStatus.Passed) return AutomationExecutionStatus.Passed;
            if (status == TestStatus.Skipped) return AutomationExecutionStatus.Skipped;
            return AutomationExecutionStatus.Failed;
        }
    }
}
