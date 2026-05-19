using BankLite.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace BankLite.Infrastructure.Services
{
    public class GroqService : IGroqService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GroqService> _logger;

        public GroqService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq API key not configured");
            _logger = logger;
        }

        public async Task<string> GetChatResponseAsync(string userMessage)
        {
            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are Alfred, assistant for BankLite. This is a demo banking website (all money is virtual). Accounts: One Chequing + one Savings max. Create from Dashboard by selecting type and clicking Create Account. Each has a copyable account number. Click account card to view transactions. Dashboard shows spending chart of past 30 days deposits vs withdrawals. Deposit/Withdraw: Go to Deposit or Withdraw page. Select account, enter amount, click action. Min $0.01. Cannot withdraw more than balance. Transfer: Go to Transfer page. My Accounts tab = between your own accounts. Send to Someone tab = enter another BankLite user's account number + amount. Cannot transfer to same account. Transactions: Go to Transactions page. Select account, filter by All/Deposits/Withdrawals/Transfers, paginated 10 per page. CSV export icon appears next to header when transactions exist. Settings (gear icon in sidebar): View profile, change password, toggle dark mode, toggle french button, view Privacy Policy, Terms of Service and permanently delete account. Session: Expires after 60 minutes. Warning shown at 59 minutes click Stay Logged In to extend. Built By: Nicholas Xydis (full stack portfolio project) Reply under 80 words. Be friendly and concise. Only answer BankLite and banking questions. Politely redirect anything unrelated."
                    },
                    new
                    {
                        role = "user",
                        content = userMessage
                    }
                },
                max_tokens = 150
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Groq API request failed with status {Status}", response.StatusCode);
                throw new HttpRequestException("AI assistant is temporarily unavailable. Please try again later.");
            }
            _logger.LogInformation("Groq API request completed successfully");

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

            return responseObj
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Sorry I could not process your request.";
        }
    }
}