using System.Net;
using System.Net.Mail;

namespace GardenCompanion.Api.Infrastructure.Email;

public class SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default)
    {
        var smtpSection = configuration.GetSection("Smtp");
        var host = smtpSection["Host"] ?? "localhost";
        var port = int.TryParse(smtpSection["Port"], out var p) ? p : 25;
        var username = smtpSection["Username"];
        var password = smtpSection["Password"];
        var fromEmail = smtpSection["FromEmail"] ?? "noreply@gardencompanion.local";
        var fromName = smtpSection["FromName"] ?? "Garden Companion";

        var body = $"""
            <html>
            <body>
              <p>You requested a password reset for your Garden Companion account.</p>
              <p><a href="{resetLink}">Reset your password</a></p>
              <p>This link expires in 1 hour. If you did not request a reset, ignore this email.</p>
            </body>
            </html>
            """;

        using var client = new SmtpClient(host, port);
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        var message = new MailMessage(
            from: new MailAddress(fromEmail, fromName),
            to: new MailAddress(to))
        {
            Subject = "Reset your Garden Companion password",
            Body = body,
            IsBodyHtml = true
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation("Password reset email sent to {Email}", to);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email to {Email}", to);
            throw;
        }
    }
}
