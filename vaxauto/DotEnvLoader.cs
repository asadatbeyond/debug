namespace VaxCare.Core.Helpers
{
    internal static class DotEnvLoader
    {
        private const string DotEnvFileName = ".env";

        public static void TryLoad()
        {
            var primary = ResolveDotEnvFilePath();
            if (primary is not null)
            {
                ApplyFileToEnvironment(primary);
                return;
            }

            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var path = FindFirstDotEnvWalkingUp(start);
                if (path is null)
                {
                    continue;
                }

                ApplyFileToEnvironment(path);
                return;
            }
        }

        /// <summary>
        /// Reads a key from .env. Prefers the file next to a solution (.sln), then the first .env walking up from cwd / base.
        /// </summary>
        public static string? TryGetValue(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            var primary = ResolveDotEnvFilePath();
            if (primary is not null)
            {
                var fromPrimary = ReadKeyFromFile(primary, key);
                if (fromPrimary is not null)
                {
                    return fromPrimary;
                }
            }

            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var path = FindFirstDotEnvWalkingUp(start);
                if (path is null)
                {
                    continue;
                }

                var value = ReadKeyFromFile(path, key);
                if (value is not null)
                {
                    return value;
                }
            }

            return null;
        }

        /// <summary>
        /// Prefer <c>.env</c> in the same directory as a <c>*.sln</c> (repo root), so Test Explorer / dotnet test do not pick a different <c>.env</c> higher in the tree.
        /// </summary>
        private static string? ResolveDotEnvFilePath()
        {
            foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                var directory = new DirectoryInfo(start);
                while (directory is not null)
                {
                    if (directory.GetFiles("*.sln").Length > 0)
                    {
                        var candidate = Path.Combine(directory.FullName, DotEnvFileName);
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

        private static void ApplyFileToEnvironment(string path)
        {
            foreach (var line in File.ReadAllLines(path))
            {
                if (!TryParseLine(line, out var key, out var value))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    continue;
                }

                Environment.SetEnvironmentVariable(key, value);
            }
        }

        private static string? ReadKeyFromFile(string path, string key)
        {
            foreach (var line in File.ReadAllLines(path))
            {
                if (!TryParseLine(line, out var parsedKey, out var value))
                {
                    continue;
                }

                if (string.Equals(parsedKey, key, StringComparison.Ordinal))
                {
                    return value;
                }
            }

            return null;
        }

        private static string? FindFirstDotEnvWalkingUp(string startDirectory)
        {
            var directory = new DirectoryInfo(startDirectory);
            while (directory is not null)
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

        private static bool TryParseLine(string line, out string key, out string value)
        {
            key = string.Empty;
            value = string.Empty;

            var trimmed = line.TrimStart('\ufeff').Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                return false;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                return false;
            }

            var rawKey = trimmed[..separatorIndex].TrimStart('\ufeff').Trim();
            if (rawKey.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                rawKey = rawKey["export ".Length..].TrimStart('\ufeff').Trim();
            }

            if (string.IsNullOrWhiteSpace(rawKey))
            {
                return false;
            }

            key = rawKey;
            value = trimmed[(separatorIndex + 1)..].Trim().Trim('"').Trim('\'');
            return true;
        }
    }
}
