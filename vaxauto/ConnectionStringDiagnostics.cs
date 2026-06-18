namespace VaxCare.Data
{
    /// <summary>
    /// Safe connection string logging for CI/debug (credentials show first 2 chars only).
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

            Console.WriteLine(
                $"[DB config] ConnectionStrings:{name} => {ToMaskedLogString(connectionString)}");
        }

        public static string ToMaskedLogString(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return "<empty>";
            }

            var parts = new List<string>();
            foreach (var fragment in connectionString.Split(';'))
            {
                var piece = fragment.Trim();
                if (string.IsNullOrEmpty(piece))
                {
                    continue;
                }

                var separatorIndex = piece.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    parts.Add(piece);
                    continue;
                }

                var key = piece[..separatorIndex].Trim();
                var value = Unquote(piece[(separatorIndex + 1)..].Trim());
                parts.Add(IsCredentialKey(key)
                    ? $"{key}={MaskPrefix(value)}"
                    : $"{key}={value}");
            }

            return string.Join(";", parts);
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            {
                return value[1..^1].Replace("\"\"", "\"");
            }

            return value;
        }

        private static bool IsCredentialKey(string key)
        {
            var normalized = key.Replace(" ", "", StringComparison.Ordinal).ToLowerInvariant();
            return normalized is "userid" or "uid" or "password" or "pwd";
        }

        private static string MaskPrefix(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "<empty>";
            }

            if (value.Length <= 2)
            {
                return new string('*', value.Length);
            }

            return value[..2] + "***";
        }
    }
}
