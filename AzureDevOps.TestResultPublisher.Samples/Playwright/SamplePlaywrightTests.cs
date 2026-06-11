using AzureDevOps.TestResultPublisher.Samples.NUnitHooks;
using NUnit.Framework;
using System.Threading.Tasks;

namespace AzureDevOps.TestResultPublisher.Samples.Playwright
{
    [TestFixture]
    public sealed class SamplePlaywrightTests : PlaywrightNUnitBase
    {
        [Test]
        [AzureDevOpsTestCaseId(12346)]
        public async Task HomePageTitle_IsDisplayed()
        {
            await Page.GotoAsync("https://example.com").ConfigureAwait(false);
            var title = await Page.TitleAsync().ConfigureAwait(false);
            Assert.That(title, Does.Contain("Example"));
        }
    }
}
