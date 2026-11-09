namespace TheSoundOfClick;

public class UserService(UserLoginRepository m_userLoginRepo, UserRepository m_userRepo, HashingService m_hashingService)
{
    public async Task CreateCustomerAsync(User user, UserLogin userLogin)
    {
        user.Role = UserRole.Customer;
        user.DiscountRole = DiscountRole.Customer;
        userLogin.UserID = await m_userRepo.CreateAsync(user);
        user.ID = userLogin.UserID;
        await m_userLoginRepo.CreateAsync(userLogin);
    }

    public async Task CreateAdminAsync(Guid requestingAdminID, User user, UserLogin userLogin)
    {
        ArgumentNullException.ThrowIfNull(requestingAdminID);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(userLogin);

        User Admin = await GetByID(requestingAdminID);

        if (Admin.Role != UserRole.Admin)
        {
            throw new RankException("An admin needs to do this action");
        }

        user.Role = UserRole.Admin;
        user.DiscountRole = DiscountRole.Admin;
        userLogin.UserID = await m_userRepo.CreateAsync(user);
        user.ID = userLogin.UserID;
        await m_userLoginRepo.CreateAsync(userLogin);
    }

    public async Task<User> GetByEmail(string email)
    {
        return await m_userRepo.GetByEmail(email);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await m_userRepo.EmailExistsAsync(email);
    }

    public async Task<User> GetByEmailWithAuth(string email, string password)
    {
        User user = await m_userRepo.GetByEmail(email);
        UserLogin userLogin = await m_userLoginRepo.GetByUserIDAsync(user.ID);
        var verify = m_hashingService.VerifyPassword(password, userLogin.PhcString);
        return verify.Item1 ? user : throw new NotFoundException("Invalid email or password");
    }


    public async Task<User> GetByID(Guid id)
    {
        return await m_userRepo.GetByID(id);
    }

    // public async Task CreateAsync(User user, string userLogin)
    // {
    //     Guid userID = await m_userRepo.CreateCustomerAsync(user);
    //     user.ID = userid;
    //     await m_userLoginRepo.CreateAsync(userLogin);
    // }

}
