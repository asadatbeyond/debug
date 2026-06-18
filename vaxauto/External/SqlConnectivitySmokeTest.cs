using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Shouldly;
using VaxCare.Core.Helpers;

namespace VaxCare.Tests.External
{
    /// <summary>
    /// SQL login smoke test (same pattern as NightlyBilling SqlConnectivitySmokeTests).
    /// Opens DataEntry only — no WebDriver, no EF fixture startup.
    /// </summary>
    [Trait("Category", "External")]
    [Trait("Category", "SqlConnectivity")]
    public class SqlConnectivitySmokeTest
    {
        [Fact]
        public async Task CanConnectAndExecuteSimpleQueryOnDataEntry()
        {
            var environment = (Environment.GetEnvironmentVariable("ENV") ?? "STG").ToUpperInvariant();
            var query = Environment.GetEnvironmentVariable("QA_SQL_CONNECTIVITY_QUERY")
                ?? "SELECT DB_NAME() AS DatabaseName, @@SERVERNAME AS ServerName, SUSER_SNAME() AS LoginName";

            Console.WriteLine($"SQL smoke test environment: {environment}");
            Console.WriteLine($"SQL smoke test query: {query}");

            var connectionString = ResolveDataEntryConnectionString();
            connectionString.ShouldNotBeNullOrWhiteSpace(
                "ConnectionStrings:DataEntry is not configured. " +
                "Set ConnectionStrings__DataEntry in CI or appsettings.{ENV}.json locally.");

            Console.WriteLine(
                $"[DB config] ConnectionStrings:DataEntry {VaxCare.Data.ConnectionStringDiagnostics.ToDiagnosticSummary(connectionString)}");

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();

            (await reader.ReadAsync()).ShouldBeTrue("Query returned no rows.");

            Console.WriteLine("SQL smoke test query executed successfully. First row:");
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                Console.WriteLine($"  {columnName}: {value ?? "<null>"}");
            }
        }

        private static string? ResolveDataEntryConnectionString()
        {
            // NightlyBilling ConfigManager order: CONNECTIONSTRINGS__{ENV}, then config key.
            var envUpper = (Environment.GetEnvironmentVariable("ENV") ?? "STG").ToUpperInvariant();
            var fromNbStyle = Environment.GetEnvironmentVariable($"CONNECTIONSTRINGS__{envUpper}")
                ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__STG");
            if (!string.IsNullOrWhiteSpace(fromNbStyle))
            {
                return fromNbStyle;
            }

            var configuration = TestConfigurationBuilder.Build();
            var connectionString = configuration.GetConnectionString("DataEntry");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }

            return Environment.GetEnvironmentVariable("ConnectionStrings__DataEntry")
                ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DATAENTRY");
        }
    }
}
