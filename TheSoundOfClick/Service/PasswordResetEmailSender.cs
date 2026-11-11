using System.Net;

namespace TheSoundOfClick;

public class PasswordResetEmailSender(SmtpEmailSender emailSender)
{
    public async Task SendResetLinkAsync(string recipientEmail, string resetUrl)
    {
        string htmlBody = $"""
                <p>Vi har modtaget en anmodning om at nulstille adgangskoden til din konto.</p>
                <p><a href="{WebUtility.HtmlEncode(resetUrl)}"
                      style="display:inline-block;padding:12px 20px;background:#087f9b;color:#fff;text-decoration:none;border-radius:6px">
                    Opret ny adgangskode
                </a></p>
                <p>Linket udløber om 30 minutter og kan kun bruges én gang.</p>
                <p>Hvis du ikke har bedt om dette, kan du ignorere denne e-mail.</p>
                """;

        await emailSender.SendAsync(recipientEmail, "Nulstil din adgangskode", htmlBody);
    }
}
