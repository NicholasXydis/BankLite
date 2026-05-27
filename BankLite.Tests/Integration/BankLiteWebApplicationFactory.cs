using BankLite.Application.Interfaces;
using BankLite.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
using Respawn;
using System.Threading.RateLimiting;
using Xunit;


namespace BankLite.Tests.Integration
{
    [CollectionDefinition("Integration")]
    public class IntegrationTestCollection : ICollectionFixture<BankLiteWebApplicationFactory>
    {
    }

    public class BankLiteWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private Respawner _respawner = null!;
        private NpgsqlConnection _connection = null!;
        public Mock<IEmailService> EmailServiceMock { get; } = new Mock<IEmailService>();
        public Mock<IGroqService> GroqServiceMock { get; } = new Mock<IGroqService>();
        public string? LastResetToken { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile(
                    Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Testing.json"),
                    optional: false);
            });

            builder.ConfigureServices(services =>
            {
                var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
                if (emailDescriptor != null) services.Remove(emailDescriptor);

                var groqDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IGroqService));
                if (groqDescriptor != null) services.Remove(groqDescriptor);

                services.AddScoped(_ => EmailServiceMock.Object);
                services.AddScoped(_ => GroqServiceMock.Object);

                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BankLiteDbContext>));
                if (dbDescriptor != null) services.Remove(dbDescriptor);

                var config = services.BuildServiceProvider()
                    .GetRequiredService<IConfiguration>();

                services.AddDbContext<BankLiteDbContext>(options =>
                    options.UseNpgsql(config.GetConnectionString("DefaultConnection")));
            });
        }

        public async Task InitializeAsync()
        {
            var config = Services.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("DefaultConnection")!;

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BankLiteDbContext>();
            await db.Database.EnsureCreatedAsync();

            _connection = new NpgsqlConnection(connectionString);
            await _connection.OpenAsync();

            _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" }
            });

            GroqServiceMock.Setup(g => g.GetChatResponseAsync(It.IsAny<string>()))
                .ReturnsAsync("This is Alfred's test response.");

            EmailServiceMock.Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((email, resetLink, lang) =>
        {
            var uri = new Uri(resetLink);
            var query = uri.Query.TrimStart('?');
            LastResetToken = query.Split('&')
               .Select(p => p.Split('='))
               .Where(p => p[0] == "token")
               .Select(p => Uri.UnescapeDataString(p[1]))
               .FirstOrDefault();
        })
            .Returns(Task.CompletedTask);
        }

        public async Task ResetDatabaseAsync()
        {
            if (_respawner != null && _connection != null)
                await _respawner.ResetAsync(_connection);
        }

        public new async Task DisposeAsync()
        {
            if (_connection != null)
                await _connection.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}