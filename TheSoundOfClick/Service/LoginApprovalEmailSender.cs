using System.Net;

namespace TheSoundOfClick;

public class LoginApprovalEmailSender(SmtpEmailSender emailSender)
{
    public async Task SendApprovalLinkAsync(string recipientEmail, string approvalUrl)
    {
        string htmlBody = $"""
            <p>Der har været tre mislykkede loginforsøg på din konto.</p>
            <p>Hvis det var dig, skal du godkende oplåsningen af kontoen her:</p>
            <p><a href="{WebUtility.HtmlEncode(approvalUrl)}"
                  style="display:inline-block;padding:12px 20px;background:#087f9b;color:#fff;text-decoration:none;border-radius:6px">
                Godkend oplåsning
            </a></p>
            <p>Du skal stadig logge ind med din adgangskode efter godkendelsen.</p>
            <p>Hvis du ikke har forsøgt at logge ind, kan du ignorere denne e-mail.</p>
            """;

        await emailSender.SendAsync(recipientEmail, "Godkend oplåsning af din konto", htmlBody);
    }
}
