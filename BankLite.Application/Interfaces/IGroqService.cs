namespace BankLite.Application.Interfaces
{
    public interface IGroqService
    {
        Task<string> GetChatResponseAsync(string userMessage);
    }
}