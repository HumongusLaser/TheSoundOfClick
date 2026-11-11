using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace TheSoundOfClick;

public class SmtpEmailSender(IConfiguration configuration)
{
    public async Task SendAsync(string recipientEmail, string subject, string htmlBody)
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
            Subject = subject,
            IsBodyHtml = true,
            Body = htmlBody,
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
        using JsonDocument secretJson = JsonDocument.Parse(File.ReadAllText("secret.json"));
        string password = secretJson.RootElement.GetProperty("PasswordReset").GetProperty("Password").GetString()
            ?? throw new InvalidOperationException("The password reset email password is missing.");
        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        await client.SendMailAsync(message);
    }
}
