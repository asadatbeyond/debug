using Microsoft.Data.SqlClient;

namespace VaxCare.Data
{
    /// <summary>
    /// Safe connection string logging for CI/debug (server/catalog only — no credentials).
    /// Enable with LOG_CONNECTION_STRINGS=Y (always on when ENV is set in Docker/CI).
    /// </summary>
    public static class ConnectionStringDiagnostics
    {
        private static readonly HashSet<string> LoggedNames = new(StringComparer.OrdinalIgnoreCase);

        public static bool ShouldLog()
        {
            var flag = Environment.GetEnvironmentVariable("LOG_CONNECTION_STRINGS")?.Trim();
            if (string.Equals(flag, "Y", StringComparison.OrdinalIgnoreCase)
                || string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(flag, "1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Default on in CI/Docker when ENV is set (GHA workflows always pass ENV=QA|STG).
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ENV"));
        }

        public static void LogResolved(string name, string connectionString)
        {
            if (!ShouldLog())
            {
                return;
            }

            lock (LoggedNames)
            {
                if (!LoggedNames.Add(name))
                {
                    return;
                }
            }

            Console.WriteLine($"[DB config] ConnectionStrings:{name} {ToDiagnosticSummary(connectionString)}");
        }

        public static string ToDiagnosticSummary(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return "<empty>";
            }

            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                var sqlAuthConfigured = !builder.IntegratedSecurity
                    && !string.IsNullOrEmpty(builder.UserID)
                    && !string.IsNullOrEmpty(builder.Password);

                return string.Join(
                    ";",
                    $"DataSource={builder.DataSource}",
                    $"InitialCatalog={builder.InitialCatalog}",
                    $"IntegratedSecurity={builder.IntegratedSecurity}",
                    $"SqlAuthConfigured={sqlAuthConfigured}");
            }
            catch (Exception ex)
            {
                return $"parse-error={ex.Message}";
            }
        }
    }
}
