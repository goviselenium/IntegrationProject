using AzureDevOps.TestResultPublisher.Models;

namespace AzureDevOps.TestResultPublisher.Services
{
    public static class ResultMapper
    {
        public static string ToAzureDevOpsOutcome(AutomationExecutionStatus status)
        {
            switch (status)
            {
                case AutomationExecutionStatus.Passed:
                    return "Passed";
                case AutomationExecutionStatus.Failed:
                    return "Failed";
                case AutomationExecutionStatus.Skipped:
                    return "NotApplicable";
                default:
                    return "Unspecified";
            }
        }

    }
}
