using BankLite.Application.Interfaces;
using BankLite.Application.Options;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BankLite.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ISendGridClient _client;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ILogger<EmailService> _logger;

    public EmailService(ISendGridClient client, IOptions<SendGridSettings> settings, ILogger<EmailService> logger)
    {
        _client = client;
        _fromEmail = settings.Value.FromEmail;
        _fromName = settings.Value.FromName;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink, string lang = "en")
    {
        var from = new EmailAddress(_fromEmail, _fromName);
        var to = new EmailAddress(toEmail);

        var isFrench = lang == "fr";

        var subject = isFrench
            ? "BankLite — Réinitialisation de votre mot de passe"
            : "BankLite — Reset Your Password";

        var plainText = isFrench
            ? $"Cliquez sur le lien pour réinitialiser votre mot de passe : {resetLink}"
            : $"Click the link to reset your password: {resetLink}";

        var html = BuildPasswordResetHtml(resetLink, isFrench);

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, html);
        var response = await _client.SendEmailAsync(msg);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("SendGrid failed to send password reset email. Status: {Status}", response.StatusCode);
            throw new InvalidOperationException("Failed to send reset email. Please try again later.");
        }

        _logger.LogInformation("Password reset email sent successfully");
    }

    private static string BuildPasswordResetHtml(string resetLink, bool isFrench)
    {
        var encodedResetLink = WebUtility.HtmlEncode(resetLink);
        var title = isFrench ? "Réinitialisez votre mot de passe" : "Reset your password";
        var intro = isFrench
            ? "Nous avons reçu une demande de réinitialisation de votre mot de passe BankLite."
            : "We received a request to reset your BankLite password.";
        var buttonText = isFrench ? "Réinitialiser le mot de passe" : "Reset password";
        var expiry = isFrench
            ? "Ce lien expire dans 15 minutes."
            : "This link expires in 15 minutes.";
        var ignore = isFrench
            ? "Si vous n'avez pas fait cette demande, vous pouvez ignorer cet email en toute sécurité."
            : "If you did not request this, you can safely ignore this email.";
        var security = isFrench
            ? "BankLite ne vous demandera jamais votre mot de passe par email."
            : "BankLite will never ask for your password by email.";

        return $@"
<!doctype html>
<html lang=""{(isFrench ? "fr" : "en")}"">
  <head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
  </head>
  <body style=""margin:0; padding:0; background:#eef4fb; font-family:Inter, Arial, sans-serif; color:#0f2340;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background:#eef4fb; padding:32px 16px;"">
      <tr>
        <td align=""center"">
          <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""max-width:560px; background:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 18px 45px rgba(15,35,64,0.14);"">
            <tr>
              <td style=""background:#0f2340; padding:32px 32px 30px; text-align:center;"">
                <img src=""https://banklite.ca/favicon.ico"" width=""42"" height=""42"" alt=""BankLite"" style=""display:inline-block; width:42px; height:42px; border-radius:10px; margin-bottom:12px;"">
                <div style=""color:#ffffff; font-size:26px; font-weight:800; letter-spacing:0;"">BankLite</div>
              </td>
            </tr>
            <tr>
              <td style=""padding:38px 38px 12px; text-align:center;"">
                <h1 style=""margin:0; color:#0f2340; font-size:28px; line-height:1.2; font-weight:800;"">{title}</h1>
                <p style=""margin:18px 0 0; color:#0f2340; font-size:16px; line-height:1.6;"">{intro}</p>
              </td>
            </tr>
            <tr>
              <td align=""center"" style=""padding:26px 38px 16px;"">
                <a href=""{encodedResetLink}"" style=""display:inline-block; background:#1a3a5c; color:#ffffff; padding:15px 30px; border-radius:9px; text-decoration:none; font-size:16px; font-weight:800;"">{buttonText}</a>
              </td>
            </tr>
            <tr>
              <td style=""padding:10px 46px 38px; text-align:center;"">
                <p style=""margin:0; color:#1a3a5c; font-size:14px; line-height:1.7;"">{expiry}<br>{ignore}</p>
              </td>
            </tr>
            <tr>
              <td style=""background:#eef4fb; padding:20px 34px; text-align:center; border-top:1px solid #dbe7f3;"">
                <p style=""margin:0; color:#1a3a5c; font-size:12px; line-height:1.6;"">{security}</p>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";
    }
}
