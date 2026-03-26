using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var connectionString = ResolveConnectionString();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "No se encontro la cadena de conexion para migraciones. Configure ConnectionStrings__DefaultConnection en variables de entorno o en un archivo .env (src/API/.env o raiz del repo).");
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }

        private static string? ResolveConnectionString()
        {
            // Prioridad 1: variable de entorno del proceso.
            var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return fromEnvironment;
            }

            // Prioridad 2: archivo .env local (util cuando se ejecuta dotnet ef sin exportar variables).
            return TryReadConnectionStringFromDotEnv();
        }

        private static string? TryReadConnectionStringFromDotEnv()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var directory = new DirectoryInfo(currentDirectory);
            while (directory != null)
            {
                var directDotEnv = Path.Combine(directory.FullName, ".env");
                if (visited.Add(directDotEnv))
                {
                    var value = TryReadConnectionStringFromFile(directDotEnv);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }

                var apiDotEnv = Path.Combine(directory.FullName, "src", "API", ".env");
                if (visited.Add(apiDotEnv))
                {
                    var value = TryReadConnectionStringFromFile(apiDotEnv);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static string? TryReadConnectionStringFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            foreach (var rawLine in File.ReadLines(filePath))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line.Substring(0, separatorIndex).Trim();
                if (!string.Equals(key, "ConnectionStrings__DefaultConnection", StringComparison.Ordinal))
                {
                    continue;
                }

                var value = line.Substring(separatorIndex + 1).Trim();
                if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                return string.IsNullOrWhiteSpace(value) ? null : value;
            }

            return null;
        }
    }
}

