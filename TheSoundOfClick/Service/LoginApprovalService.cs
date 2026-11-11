using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace TheSoundOfClick;

public class LoginApprovalService(
    LoginSecurityRepository repository,
    LoginApprovalEmailSender emailSender,
    IConfiguration configuration)
{
    public Task<bool> IsLockedAsync(Guid userId) => repository.IsLockedAsync(userId);

    public Task ClearAsync(Guid userId) => repository.ClearAsync(userId);

    public async Task RecordFailedAttemptAsync(User user)
    {
        string token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        string tokenHash = HashToken(token);
        if (!await repository.RecordFailedAttemptAsync(user.ID, tokenHash))
        {
            return;
        }

        try
        {
            string baseUrl = configuration["Application:BaseUrl"]
                ?? throw new InvalidOperationException("Application base URL is not configured.");
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
            {
                throw new InvalidOperationException("Application base URL must be an absolute HTTP or HTTPS URL.");
            }

            string approvalUrl = new Uri(
                baseUri,
                $"approve-login?token={Uri.EscapeDataString(token)}").ToString();
            await emailSender.SendApprovalLinkAsync(user.Email, approvalUrl);
        }
        catch (Exception exception)
        {
            await repository.CancelLockAsync(user.ID, tokenHash);
            throw new LoginApprovalEmailException("Could not send the login approval email.", exception);
        }
    }

    public Task<bool> ApproveAsync(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return repository.ApproveAsync(HashToken(token));
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public class LoginApprovalEmailException(string message, Exception innerException)
    : Exception(message, innerException)
{
}
