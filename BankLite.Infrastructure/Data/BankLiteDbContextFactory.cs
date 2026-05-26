using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BankLite.Infrastructure.Data
{
    public class BankLiteDbContextFactory : IDesignTimeDbContextFactory<BankLiteDbContext>
    {
        public BankLiteDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                 .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../BankLite.API"))
                 .AddJsonFile("appsettings.json", optional: false)
                 .AddEnvironmentVariables()
                 .Build();

            var optionsBuilder = new DbContextOptionsBuilder<BankLiteDbContext>();
            optionsBuilder.UseNpgsql(config.GetConnectionString("DefaultConnection"));

            return new BankLiteDbContext(optionsBuilder.Options);
        }
    }
}