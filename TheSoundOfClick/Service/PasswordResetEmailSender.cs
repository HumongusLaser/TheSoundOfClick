using System.Net;
using System.Net.Mail;

namespace TheSoundOfClick;

public class PasswordResetEmailSender(IConfiguration configuration)
{
    public async Task SendResetLinkAsync(string recipientEmail, string resetUrl)
    {
        IConfigurationSection smtp = configuration.GetSection("PasswordReset:Smtp");
        string host = smtp["Host"] ?? throw new InvalidOperationException("Password reset SMTP host is not configured.");
        string fromAddress = smtp["FromAddress"] ?? throw new InvalidOperationException("Password reset sender address is not configured.");
        string fromName = smtp["FromName"] ?? "The Sound Of Click";
        if (!int.TryParse(smtp["Port"], out int port) || port is < 1 or > 65535)
        {
            throw new InvalidOperationException("Password reset SMTP port is not configured correctly.");
        }
        if (!bool.TryParse(smtp["EnableSsl"], out bool enableSsl) || !enableSsl)
        {
            throw new InvalidOperationException("Password reset SMTP must use TLS. Set PasswordReset:Smtp:EnableSsl to true.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = "Nulstil din adgangskode",
            IsBodyHtml = true,
            Body = $"""
                <p>Vi har modtaget en anmodning om at nulstille adgangskoden til din konto.</p>
                <p><a href="{WebUtility.HtmlEncode(resetUrl)}"
                      style="display:inline-block;padding:12px 20px;background:#087f9b;color:#fff;text-decoration:none;border-radius:6px">
                    Opret ny adgangskode
                </a></p>
                <p>Linket udløber om 30 minutter og kan kun bruges én gang.</p>
                <p>Hvis du ikke har bedt om dette, kan du ignorere denne e-mail.</p>
                """,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8
        };
        message.To.Add(new MailAddress(recipientEmail));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        string? username = smtp["Username"];
        string? password = smtp["Password"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password ?? "");
        }

        await client.SendMailAsync(message);
    }
}
