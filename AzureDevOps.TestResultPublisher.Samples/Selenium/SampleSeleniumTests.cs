using AzureDevOps.TestResultPublisher.Samples.NUnitHooks;
using NUnit.Framework;

namespace AzureDevOps.TestResultPublisher.Samples.Selenium
{
    [TestFixture]
    public sealed class SampleSeleniumTests : SeleniumNUnitBase
    {
        [Test]
        [AzureDevOpsTestCaseId(12345)]
        public void HomePageTitle_IsDisplayed()
        {
            Driver.Navigate().GoToUrl("https://example.com");
            Assert.That(Driver.Title, Does.Contain("Example"));
        }
    }
}
