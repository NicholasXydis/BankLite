using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BankLite.Infrastructure.Data;

public class BankLiteDbContextFactory : IDesignTimeDbContextFactory<BankLiteDbContext>
{
    public BankLiteDbContext CreateDbContext(string[] args)
    {
        var settingsPath = FindSettingsPath();
        var config = new ConfigurationBuilder()
            .AddJsonFile(settingsPath, false)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<BankLiteDbContext>();
        optionsBuilder.UseNpgsql(config.GetConnectionString("DefaultConnection"));

        return new BankLiteDbContext(optionsBuilder.Options);
    }

    private static string FindSettingsPath()
    {
        var directories = new[]
        {
            new DirectoryInfo(Directory.GetCurrentDirectory()),
            new DirectoryInfo(AppContext.BaseDirectory)
        };

        foreach (var directory in directories)
        {
            for (var current = directory; current != null; current = current.Parent)
            {
                var paths = new[]
                {
                    Path.Combine(current.FullName, "BankLite.API", "appsettings.json"),
                    Path.Combine(current.FullName, "BankLiteAPI", "BankLite.API", "appsettings.json")
                };

                foreach (var path in paths)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }
        }

        throw new FileNotFoundException("Could not locate BankLite.API appsettings.json.");
    }
}
