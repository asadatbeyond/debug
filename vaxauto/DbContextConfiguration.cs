using Microsoft.Extensions.Configuration;

namespace VaxCare.Data
{
    public abstract class DbContextConfiguration()
    {
        public int CommandTimeoutInSeconds { get; set; } = 30;
        public required string ConnectionString { get; set; }

        protected static string RequireConnectionString(IConfiguration configuration, string name)
        {
            var value = configuration.GetConnectionString(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                value = Environment.GetEnvironmentVariable($"ConnectionStrings__{name}")
                    ?? Environment.GetEnvironmentVariable($"CONNECTIONSTRINGS__{name.ToUpperInvariant()}");
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                ConnectionStringDiagnostics.LogResolved(name, value);
                return value;
            }

            var env = Environment.GetEnvironmentVariable("ENV") ?? "STG";
            throw new InvalidOperationException(
                $"Connection string '{name}' is not configured. " +
                $"Ensure VaxCare.Tests/appsettings.{env}.json exists (or is supplied in CI) " +
                $"or set environment variable ConnectionStrings__{name}.");
        }
    }

    public class DataEntryDbContextConfiguration() : DbContextConfiguration()
    {
        public DataEntryDbContextConfiguration(IConfigurationRoot configuration) : this()
        {
            ConnectionString = RequireConnectionString(configuration, "DataEntry");
        }
    }

    public class HealthSystemsDbContextConfiguration() : DbContextConfiguration()
    {
        public HealthSystemsDbContextConfiguration(IConfigurationRoot configuration) : this()
        {
            ConnectionString = RequireConnectionString(configuration, "HealthSystems");
        }
    }

    public class ReportingDbContextConfiguration() : DbContextConfiguration()
    {
        public ReportingDbContextConfiguration(IConfigurationRoot configuration) : this()
        {
            ConnectionString = RequireConnectionString(configuration, "Reporting");
        }
    }

    public class RiskDbContextConfiguration() : DbContextConfiguration()
    {
        public RiskDbContextConfiguration(IConfigurationRoot configuration) : this()
        {
            ConnectionString = RequireConnectionString(configuration, "Risk");
        }
    }

    public class SalesDbContextConfiguration() : DbContextConfiguration()
    {
        public SalesDbContextConfiguration(IConfigurationRoot configuration) : this()
        {
            ConnectionString = RequireConnectionString(configuration, "Sales");
        }
    }

    public class RealMedContextConfiguration() : DbContextConfiguration()
    {
        public RealMedContextConfiguration(IConfigurationRoot configuration) : this()
        {
            ConnectionString = RequireConnectionString(configuration, "RealMed");
        }
    }
}
