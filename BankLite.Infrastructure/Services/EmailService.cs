using BankLite.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BankLite.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _apiKey = configuration["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid API key not configured");
            _fromEmail = configuration["SendGrid:FromEmail"] ?? throw new InvalidOperationException("SendGrid from email not configured");
            _fromName = configuration["SendGrid:FromName"] ?? "BankLite";
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink, string lang = "en")
        {

            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);

            var isFrench = lang == "fr";

            var subject = isFrench
                ? "BankLite — Réinitialisation de votre mot de passe"
                : "BankLite — Reset Your Password";

            var plainText = isFrench
                ? $"Cliquez sur le lien pour réinitialiser votre mot de passe : {resetLink}"
                : $"Click the link to reset your password: {resetLink}";

            var html = isFrench ? $@"
                <div style='font-family: Inter, sans-serif; max-width: 480px; margin: 0 auto; text-align:center;'>
                     <h2 style='color: #1a3a5c;'>Réinitialisation du mot de passe</h2>
                     <p>Nous avons reçu une demande de réinitialisation de votre mot de passe BankLite.</p>
                     <a href='{resetLink}' style='display:inline-block; background:#1a3a5c; color:#fff; padding:12px 24px; border-radius:6px; text-decoration:none; margin: 16px 0;'>Réinitialiser le mot de passe</a>
                     <p style='color:#6b7280; font-size:0.85rem;'>Ce lien expire dans 15 minutes. Si vous n'avez pas fait cette demande, ignorez cet email.</p>
                 </div>"
            : $@"
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