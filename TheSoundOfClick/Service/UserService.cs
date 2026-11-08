namespace TheSoundOfClick;

public class UserService(UserLoginRepository m_userLoginRepo, UserRepository m_userRepo)
{
    public async Task CreateAsync(User user, UserLogin userLogin)
    {
        userLogin.UserID = await m_userRepo.CreateCustomerAsync(user);
        user.ID = userLogin.UserID;
        await m_userLoginRepo.CreateAsync(userLogin);
    }

    // public async Task CreateAsync(User user, string userLogin)
    // {
    //     Guid userID = await m_userRepo.CreateCustomerAsync(user);
    //     user.ID = userid;
    //     await m_userLoginRepo.CreateAsync(userLogin);
    // }

}
