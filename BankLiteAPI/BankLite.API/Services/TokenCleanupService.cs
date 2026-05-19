using BankLite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankLite.API.Services
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupService> _logger;

        public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanupExpiredTokensAsync();
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task CleanupExpiredTokensAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BankLiteDbContext>();

            var now = DateTime.UtcNow;

            var expiredRefreshTokens = await context.RefreshTokens
                .Where(rt => rt.ExpiresAt < now || rt.IsRevoked)
                .ToListAsync();

            var expiredResetTokens = await context.PasswordResetTokens
                .Where(pt => pt.ExpiresAt < now || pt.IsUsed)
                .ToListAsync();

            context.RefreshTokens.RemoveRange(expiredRefreshTokens);
            context.PasswordResetTokens.RemoveRange(expiredResetTokens);

            await context.SaveChangesAsync();
            _logger.LogInformation("Token cleanup: removed {RefreshCount} refresh tokens and {ResetCount} reset tokens",
                expiredRefreshTokens.Count, expiredResetTokens.Count);
        }
    }
}