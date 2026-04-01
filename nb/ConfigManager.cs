using System.Configuration;

namespace NightlyBillingConfigManager
{
    public static class ConfigManager
    {
        public static string GetConnectionString(string environment)
        {
            string normalizedEnvironment = environment.ToLowerInvariant();
            string configKey = string.Empty;

            if (normalizedEnvironment == "qa")
                configKey = "QA";
            else if (normalizedEnvironment == "stg" || normalizedEnvironment == "staging")
                configKey = "STG";
            else
            {
                Console.WriteLine("Value for environment is not valid. Defaulting to QA environment.");
                configKey = "QA";
            }

            string? overrideConnectionString = GetOverrideConnectionString(configKey);
            if (!string.IsNullOrWhiteSpace(overrideConnectionString))
            {
                Console.WriteLine($"Using connection string from environment variable for {configKey}.");
                return overrideConnectionString;
            }

            ConnectionStringSettings? connectionSettings = ConfigurationManager.ConnectionStrings[configKey];
            if (connectionSettings == null || string.IsNullOrWhiteSpace(connectionSettings.ConnectionString))
                throw new InvalidOperationException($"Connection string '{configKey}' was not found.");

            Console.WriteLine($"Using connection string from config for {configKey}.");
            return connectionSettings.ConnectionString;
        }

        private static string? GetOverrideConnectionString(string configKey)
        {
            string primaryKey = $"NB_CONNECTION_STRING_{configKey}";
            string legacyKey = $"{configKey}_CONNECTION_STRING";

            return Environment.GetEnvironmentVariable(primaryKey)
                ?? Environment.GetEnvironmentVariable(legacyKey);
        }
    }
}
