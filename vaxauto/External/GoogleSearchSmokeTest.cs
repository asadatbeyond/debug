using OpenQA.Selenium;
using VaxCare.Core;
using VaxCare.Core.WebDriver;
using Xunit.Abstractions;

namespace VaxCare.Tests.External
{
    [Trait("Category", "External")]
    [Trait("Category", "Google")]
    public class GoogleSearchSmokeTest(ITestOutputHelper output) : BaseTest(output)
    {
        [Fact]
        public async Task CanSearchGoogle()
        {
            const string searchTerm = "Cursor IDE";

            await RunTestAsync("Google search smoke test", async () =>
            {
                var driver = Driver!;

                await driver.NavigateAsync("https://www.google.com/ncr");
                await driver.SendKeysAsync(By.Name("q"), $"{searchTerm}{Keys.Enter}");

                await Task.Delay(1500);

                var url = await driver.ExecuteAsync(webDriver => Task.FromResult(webDriver.Url));
                Log!.Information("Google search completed. Current URL: {Url}", url);
            });
        }
    }
}
