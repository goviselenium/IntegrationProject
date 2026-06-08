using AzureDevOps.TestResultPublisher.Samples.NUnitHooks;
using NUnit.Framework;

namespace AzureDevOps.TestResultPublisher.Samples.Selenium
{
    [TestFixture]
    public sealed class SampleSeleniumTests : SeleniumNUnitBase
    {
        [Test]
        [Explicit("Sample only. Remove Explicit when wired to a real application and Azure DevOps test case.")]
        [AzureDevOpsTestCaseId(12345)]
        public void HomePageTitle_IsDisplayed()
        {
            Driver.Navigate().GoToUrl("https://example.com");
            Assert.That(Driver.Title, Does.Contain("Example"));
        }
    }
}
