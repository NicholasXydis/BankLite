using BankLite.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BankLite.API.Hubs
{
    public class SignalRBalanceNotifier : IBalanceNotifier
    {
        private readonly IHubContext<BankHub> _hubContext;

        public SignalRBalanceNotifier(IHubContext<BankHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyBalanceUpdatedAsync(Guid userId, Guid accountId, decimal newBalance)
        {
            await _hubContext.Clients.Group(userId.ToString()).SendAsync("BalanceUpdated", accountId, newBalance);
        }
    }
}