using AzureDevOps.TestResultPublisher.Samples.NUnitHooks;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AzureDevOps.TestResultPublisher.Samples.Playwright
{
    public abstract class PlaywrightNUnitBase
    {
        private DateTimeOffset _startedDate;
        private IPlaywright _playwright;
        private IBrowser _browser;

        protected IPage Page { get; private set; }

        [SetUp]
        public async Task SetUp()
        {
            _startedDate = DateTimeOffset.UtcNow;
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true }).ConfigureAwait(false);
            Page = await _browser.NewPageAsync().ConfigureAwait(false);
        }

        [TearDown]
        public async Task TearDown()
        {
            var screenshotPath = "";
            try
            {
                if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed && Page != null)
                {
                    var screenshotDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");
                    Directory.CreateDirectory(screenshotDirectory);
                    screenshotPath = Path.Combine(screenshotDirectory, $"{TestContext.CurrentContext.Test.ID}.png");
                    await Page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true }).ConfigureAwait(false);
                }

                await NUnitResultPublisher.PublishCurrentTestAsync(_startedDate, screenshotPath, "Chromium/Playwright").ConfigureAwait(false);
            }
            finally
            {
                if (_browser != null) await _browser.CloseAsync().ConfigureAwait(false);
                _playwright?.Dispose();
            }
        }
    }
}
