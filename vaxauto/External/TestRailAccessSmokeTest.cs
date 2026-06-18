using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Shouldly;
using VaxCare.Core;
using Xunit.Abstractions;

namespace VaxCare.Tests.External
{
    [Trait("Category", "External")]
    [Trait("Category", "TestRail")]
    public class TestRailAccessSmokeTest(ITestOutputHelper output) : BaseTest(output)
    {
        private const string DotEnvFileName = ".env";
        private const string TestRailBaseUrlEnvVar = "TESTRAIL_BASE_URL";
        private const string TestRailUsernameEnvVar = "TESTRAIL_USERNAME";
        private const string TestRailApiKeyEnvVar = "TESTRAIL_API_KEY";

        [Fact]
        public async Task CanAccessTestRailProjectsApi()
        {
            await RunTestAsync("TestRail access smoke test", async () =>
            {
                LoadDotEnvIfPresent();

                var baseUrl = Environment.GetEnvironmentVariable(TestRailBaseUrlEnvVar);
                var username = Environment.GetEnvironmentVariable(TestRailUsernameEnvVar);
                var apiKey = Environment.GetEnvironmentVariable(TestRailApiKeyEnvVar);

                baseUrl.ShouldNotBeNullOrWhiteSpace($"{TestRailBaseUrlEnvVar} is required.");
                username.ShouldNotBeNullOrWhiteSpace($"{TestRailUsernameEnvVar} is required.");
                apiKey.ShouldNotBeNullOrWhiteSpace($"{TestRailApiKeyEnvVar} is required.");

                var normalizedBaseUrl = baseUrl!.TrimEnd('/');
                var endpoint = $"{normalizedBaseUrl}/index.php?/api/v2/get_projects";

                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                var basicAuthToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{apiKey}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthToken);

                var response = await client.GetAsync(endpoint);
                var responseBody = await response.Content.ReadAsStringAsync();

                response.IsSuccessStatusCode.ShouldBeTrue(
                    $"TestRail API returned {(int)response.StatusCode}: {responseBody}");

                using var jsonDocument = JsonDocument.Parse(responseBody);
                jsonDocument.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);

                var projectCount = jsonDocument.RootElement.GetArrayLength();
                projectCount.ShouldBeGreaterThan(0);

                Log!.Information("TestRail access verified. Retrieved {ProjectCount} projects.", projectCount);
            });
        }

        private static void LoadDotEnvIfPresent()
        {
            var dotEnvPath = FindDotEnvPath(Directory.GetCurrentDirectory());
            if (string.IsNullOrWhiteSpace(dotEnvPath))
            {
                return;
            }

            foreach (var line in File.ReadAllLines(dotEnvPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                {
                    continue;
                }

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = trimmed[..separatorIndex].Trim();
                if (key.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                {
                    key = key["export ".Length..].Trim();
                }

                var value = trimmed[(separatorIndex + 1)..].Trim();
                value = value.Trim().Trim('"').Trim('\'');
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        private static string? FindDotEnvPath(string startDirectory)
        {
            var directory = new DirectoryInfo(startDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, DotEnvFileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }
    }
}
