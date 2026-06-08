using System;

namespace AzureDevOps.TestResultPublisher.Samples.NUnitHooks
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class AzureDevOpsTestCaseIdAttribute : Attribute
    {
        public AzureDevOpsTestCaseIdAttribute(int testCaseId)
        {
            TestCaseId = testCaseId;
        }

        public int TestCaseId { get; }
    }
}
