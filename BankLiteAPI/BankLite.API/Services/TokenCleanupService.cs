using BankLite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankLite.API.Services
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredTokensAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Token cleanup failed");
                }
            }
        }

        private async Task CleanupExpiredTokensAsync(CancellationToken stoppingToken)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            BankLiteDbContext context = scope.ServiceProvider.GetRequiredService<BankLiteDbContext>();

            DateTime now = DateTime.UtcNow;

            int expiredRefreshTokenCount = await context.RefreshTokens
                .Where(rt => rt.ExpiresAt < now || rt.IsRevoked)
                .ExecuteDeleteAsync(stoppingToken);

            int expiredResetTokenCount = await context.PasswordResetTokens
                .Where(pt => pt.ExpiresAt < now || pt.IsUsed)
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("Token cleanup: removed {RefreshCount} refresh tokens and {ResetCount} reset tokens",
                expiredRefreshTokenCount, expiredResetTokenCount);
        }
    }
}