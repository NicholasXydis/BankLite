using BankLite.Application.Interfaces;
using BankLite.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
using Respawn;

namespace BankLite.Tests.Integration;

[CollectionDefinition("Integration", DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<BankLiteWebApplicationFactory>;

public class BankLiteWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private NpgsqlConnection _connection = null!;
    private Respawner _respawner = null!;
    private Mock<IEmailService> EmailServiceMock { get; } = new();
    public Mock<IGroqService> GroqServiceMock { get; } = new();
    public string? LastResetToken { get; private set; }

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
            SchemasToInclude = ["public"]
        });

        GroqServiceMock.Setup(g => g.GetChatResponseAsync(It.IsAny<string>()))
            .ReturnsAsync("This is Alfred's test response.");

        EmailServiceMock.Setup(e =>
                e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, resetLink, _) =>
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

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Testing.json"),
                false);
            config.AddEnvironmentVariables();
        });

        builder.ConfigureServices(services =>
        {
            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null) services.Remove(emailDescriptor);

            var groqDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IGroqService));
            if (groqDescriptor != null) services.Remove(groqDescriptor);

            services.AddScoped(_ => EmailServiceMock.Object);
            services.AddScoped(_ => GroqServiceMock.Object);

            var dbDescriptor =
                services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BankLiteDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            services.AddDbContext<BankLiteDbContext>((serviceProvider, options) =>
            {
                var config = serviceProvider.GetRequiredService<IConfiguration>();
                options.UseNpgsql(config.GetConnectionString("DefaultConnection"));
            });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_connection);
    }
}
