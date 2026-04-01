using Microsoft.Data.SqlClient;
using NightlyBillingConfigManager;

namespace NightlyBillingUnitTests
{
    public class SqlConnectivitySmokeTests
    {
        [Test]
        public async Task CanConnectAndExecuteSimpleQuery()
        {
            string environment = (Environment.GetEnvironmentVariable("ENV") ?? "stg").ToLowerInvariant();
            string query = Environment.GetEnvironmentVariable("NB_SQL_CONNECTIVITY_QUERY")
                ?? "SELECT DB_NAME() AS DatabaseName, @@SERVERNAME AS ServerName, SUSER_SNAME() AS LoginName";

            Console.WriteLine($"SQL smoke test environment: {environment}");
            Console.WriteLine($"SQL smoke test query: {query}");

            string connectionString = ConfigManager.GetConnectionString(environment);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();

            Assert.That(await reader.ReadAsync(), Is.True, "Query returned no rows.");

            Console.WriteLine("SQL smoke test query executed successfully. First row:");
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                Console.WriteLine($"  {columnName}: {value ?? "<null>"}");
            }
        }
    }
}
