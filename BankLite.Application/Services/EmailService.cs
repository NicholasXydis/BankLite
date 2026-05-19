using BankLite.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BankLite.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {     
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromEmail"];
            var fromName = _configuration["SendGrid:FromName"];

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail);
            var subject = "BankLite — Reset Your Password";
            var plainText = $"Click the link to reset your password: {resetLink}";
            var html = $@"
                <div style='font-family: Inter, sans-serif; max-width: 480px; margin: 0 auto; text-align:center;'>
                    <h2 style='color: #1a3a5c;'>Reset Your Password</h2>
                    <p>We received a request to reset your BankLite password.</p>
                    <a href='{resetLink}' style='display:inline-block; background:#1a3a5c; color:#fff; padding:12px 24px; border-radius:6px; text-decoration:none; margin: 16px 0;'>Reset Password</a>
                    <p style='color:#6b7280; font-size:0.85rem;'>This link expires in 15 minutes. If you didn't request this, ignore this email.</p>
                </div>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, html);
            var response = await client.SendEmailAsync(msg);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SendGrid failed to send email to {Email}. Status: {Status}", toEmail, response.StatusCode);
                throw new InvalidOperationException("Failed to send reset email. Please try again later.");
            }
            _logger.LogInformation("Password reset email sent successfully to {Email}", toEmail);
        }
    }
}