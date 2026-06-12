using Microsoft.Extensions.Configuration;

namespace VaxCare.Core.Helpers
{
    internal static class TestConfigurationBuilder
    {
        public static IConfigurationRoot Build()
        {
            DotEnvLoader.TryLoad();

            var environment = Environment.GetEnvironmentVariable("ENV") ?? "STG";
            var fileName = $"appsettings.{environment}.json";
            var appsettingsPath = ResolveAppsettingsPath(fileName);

            var builder = new ConfigurationBuilder();

            if (appsettingsPath is not null)
            {
                builder.SetBasePath(Path.GetDirectoryName(appsettingsPath)!)
                    .AddJsonFile(Path.GetFileName(appsettingsPath), optional: false, reloadOnChange: true);
            }

            // After JSON so CI can override with ConnectionStrings__* / OktaConfiguration__* (same as NightlyBilling).
            builder.AddEnvironmentVariables();

            return builder.Build();
        }

        private static string? ResolveAppsettingsPath(string fileName)
        {
            foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                var directory = new DirectoryInfo(start);
                while (directory is not null)
                {
                    var candidates = new[]
                    {
                        Path.Combine(directory.FullName, fileName),
                        Path.Combine(directory.FullName, "VaxCare.Tests", fileName),
                    };

                    foreach (var candidate in candidates)
                    {
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }
                    }

                    directory = directory.Parent;
                }
            }

            return null;
        }
    }
}
