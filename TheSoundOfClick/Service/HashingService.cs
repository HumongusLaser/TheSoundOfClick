using System.Data.Common;
using System.Data.SqlTypes;
using System.Security.Cryptography;
using Argon2Sharp;

namespace TheSoundOfClick;

public class HashingService (Argon2Parameters m_argon2Parameters, UserLoginRepository m_userLoginRepo)
{
    public string HashPassword(string password)
    {
        return Argon2PhcFormat.HashToPhcStringWithAutoSalt(password, m_argon2Parameters);
    }

    public (bool, Argon2Parameters?) VerifyPassword(string password, string phcString)
    {
        return Argon2PhcFormat.VerifyPhcString(password, phcString);
    }

    public async Task<UserLogin> Create(User user, string password)
    {
        string phcString = HashPassword(password);
        UserLogin userLogin = new(){ UserID = user.ID, Email = user.Email, PhcString = phcString };

        await m_userLoginRepo.Create(userLogin);

        return userLogin;
    }

    public async Task<string?> GetPhcStringAsync(string email)
    {
        return await m_userLoginRepo.GetPasswordHashAsync(email);
    }
}