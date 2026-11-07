namespace TheSoundOfClick;

public class LoginService
{
    private readonly UserLoginRepository m_userLoginRepo;
    private readonly HashingService m_hashingService;

    public LoginService(
        UserLoginRepository userLoginRepository,
        HashingService hashingService)
    {
        m_userLoginRepo = userLoginRepository;
        m_hashingService = hashingService;
    }

    public async Task<bool> ValidateLoginAsync(string email, string password)
    {
        string? storedHash = await m_userLoginRepo.GetPasswordHashAsync(email);

        if (storedHash == null)
        {
            return false;
        }

        return m_hashingService.VerifyPassword(password, storedHash).Item1;
    }

    public async Task CreateAsync(UserLogin userLogin)
    {
        await m_userLoginRepo.CreateAsync(userLogin);
    }
}