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
        public ShallowReference TestCase { get; set; }
        public ShallowReference Suite { get; set; }
        public ShallowReference TestPlan { get; set; }
        public string Outcome { get; set; }
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

    internal sealed class TestCaseResultUpdateModel
    {
        public int Id { get; set; }
        public string State { get; set; }
        public string Outcome { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public DateTimeOffset StartedDate { get; set; }
        public DateTimeOffset CompletedDate { get; set; }
        public long DurationInMs { get; set; }
        public ShallowReference TestCase { get; set; }
        public int[] PointIds { get; set; }
        public string AutomatedTestName { get; set; }
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
        public string Outcome { get; set; }
    }

    internal sealed class ReleaseReference
    {
        public string Name { get; set; }
    }
}
