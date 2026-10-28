namespace TheSoundOfClick;

public class LoginService
{
    private readonly UserLoginRepository _userLoginRepository;
    private readonly HashingService _hashingService;

    public LoginService(
        UserLoginRepository userLoginRepository,
        HashingService hashingService)
    {
        _userLoginRepository = userLoginRepository;
        _hashingService = hashingService;
    }

    public async Task<bool> ValidateLoginAsync(string email, string password)
    {
        byte[]? storedHash =
            await _userLoginRepository.GetPasswordHashAsync(email);

        if (storedHash == null)
        {
            return false;
        }

        return _hashingService.VerifyPassword(password, storedHash);
    }
}