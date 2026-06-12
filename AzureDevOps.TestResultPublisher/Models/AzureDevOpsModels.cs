using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzureDevOps.TestResultPublisher.Models
{
    internal sealed class AzureListResponse<T>
    {
        public int Count { get; set; }
        public List<T> Value { get; set; } = new List<T>();
    }

    internal sealed class TestPoint
    {
        public int Id { get; set; }
        public int Revision { get; set; }
        public ShallowReference TestCase { get; set; }
        public ShallowReference TestCaseReference { get; set; }
        public ShallowReference Suite { get; set; }
        public ShallowReference TestSuite { get; set; }
        public ShallowReference TestPlan { get; set; }
        public List<WorkItemProperty> WorkItemProperties { get; set; } = new List<WorkItemProperty>();
        public string Outcome { get; set; }
    }

    internal sealed class WorkItemProperty
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public WorkItemPropertyValue WorkItem { get; set; }
    }

    internal sealed class WorkItemPropertyValue
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    internal sealed class ShallowReference
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }

    internal sealed class RunCreateModel
    {
        public string Name { get; set; }
        public bool Automated { get; set; }
        [JsonPropertyName("isAutomated")]
        public bool IsAutomated { get; set; }
        public ShallowReference Plan { get; set; }
        public DateTimeOffset StartedDate { get; set; }
        public string Comment { get; set; }
        public string BuildNumber { get; set; }
        public ReleaseReference Release { get; set; }
    }

    internal sealed class TestRun
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
    }

    internal sealed class TestRunUpdateModel
    {
        public string State { get; set; }
        public DateTimeOffset CompletedDate { get; set; }
        public string Comment { get; set; }
    }

    internal sealed class PlannedTestResultModel
    {
        [JsonPropertyName("state")]
        public string State { get; set; }
        [JsonPropertyName("outcome")]
        public string Outcome { get; set; }
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }
        [JsonPropertyName("stackTrace")]
        public string StackTrace { get; set; }
        [JsonPropertyName("startedDate")]
        public DateTimeOffset StartedDate { get; set; }
        [JsonPropertyName("completedDate")]
        public DateTimeOffset CompletedDate { get; set; }
        [JsonPropertyName("durationInMs")]
        public long DurationInMs { get; set; }
        [JsonPropertyName("testPointId")]
        public int TestPointId { get; set; }
        [JsonPropertyName("testCaseId")]
        public int TestCaseId { get; set; }
        [JsonPropertyName("testCaseReferenceId")]
        public int TestCaseReferenceId { get; set; }
        [JsonPropertyName("testCaseRevision")]
        public int TestCaseRevision { get; set; }
        [JsonPropertyName("testCaseTitle")]
        public string TestCaseTitle { get; set; }
        [JsonPropertyName("testPlanId")]
        public int TestPlanId { get; set; }
        [JsonPropertyName("testSuiteId")]
        public int TestSuiteId { get; set; }
        [JsonPropertyName("testPoint")]
        public ShallowReference TestPoint { get; set; }
        [JsonPropertyName("testCase")]
        public ShallowReference TestCase { get; set; }
        [JsonPropertyName("automatedTestName")]
        public string AutomatedTestName { get; set; }
        [JsonPropertyName("comment")]
        public string Comment { get; set; }
    }

    internal sealed class TestCaseResult
    {
        public int Id { get; set; }
        public string Outcome { get; set; }
    }

    internal sealed class TestResultAttachmentRequest
    {
        public string Stream { get; set; }
        public string FileName { get; set; }
        public string Comment { get; set; }
        public string AttachmentType { get; set; } = "GeneralAttachment";
    }

    internal sealed class PointUpdateModel
    {
        public int Id { get; set; }
        public PointResultsUpdateModel Results { get; set; }
    }

    internal sealed class PointResultsUpdateModel
    {
        public string Outcome { get; set; }
    }

    internal sealed class ReleaseReference
    {
        public string Name { get; set; }
    }
}
