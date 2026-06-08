using System;

namespace AzureDevOps.TestResultPublisher.Models
{
    public enum AutomationExecutionStatus
    {
        Passed,
        Failed,
        Skipped
    }

    public sealed class AutomationTestResult
    {
        public int TestCaseId { get; set; }
        public string TestCaseTitle { get; set; } = "";
        public string AutomationTestName { get; set; } = "";
        public AutomationExecutionStatus ExecutionStatus { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string StackTrace { get; set; } = "";
        public string ScreenshotPath { get; set; } = "";
        public DateTimeOffset StartedDate { get; set; }
        public DateTimeOffset CompletedDate { get; set; }
        public long DurationInMs { get; set; }
        public string BrowserName { get; set; } = "";
        public string EnvironmentName { get; set; } = "";
        public string BuildNumber { get; set; } = "";
        public string ReleaseName { get; set; } = "";
    }

    public sealed class PublishedTestResult
    {
        public int RunId { get; set; }
        public int ResultId { get; set; }
        public int TestPointId { get; set; }
        public string Outcome { get; set; } = "";
    }
}
