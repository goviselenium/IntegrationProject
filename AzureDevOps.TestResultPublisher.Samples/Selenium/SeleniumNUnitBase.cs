using AzureDevOps.TestResultPublisher.Samples.NUnitHooks;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AzureDevOps.TestResultPublisher.Samples.Selenium
{
    public abstract class SeleniumNUnitBase
    {
        private DateTimeOffset _startedDate;
        protected IWebDriver Driver { get; private set; }

        [SetUp]
        public void SetUp()
        {
            _startedDate = DateTimeOffset.UtcNow;
            var options = new ChromeOptions();
            options.AddArgument("--headless=new");
            options.AddArgument("--window-size=1440,1000");
            Driver = new ChromeDriver(options);
        }

        [TearDown]
        public async Task TearDown()
        {
            var screenshotPath = "";
            try
            {
                if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed && Driver is ITakesScreenshot screenshotDriver)
                {
                    var screenshotDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");
                    Directory.CreateDirectory(screenshotDirectory);
                    screenshotPath = Path.Combine(screenshotDirectory, $"{TestContext.CurrentContext.Test.ID}.png");
                    screenshotDriver.GetScreenshot().SaveAsFile(screenshotPath);
                }

                await NUnitResultPublisher.PublishCurrentTestAsync(_startedDate, screenshotPath, "Chrome/Selenium").ConfigureAwait(false);
            }
            finally
            {
                Driver?.Quit();
                Driver?.Dispose();
            }
        }
    }
}
