namespace TheSoundOfClick;

public class UserService(
    UserLoginRepository m_userLoginRepo,
    UserRepository m_userRepo,
    HashingService m_hashingService,
    ValidationService m_validationService)
{
    public async Task CreateCustomerAsync(User user, UserLogin userLogin)
    {
        user.Role = UserRole.Customer;
        user.DiscountRole = DiscountRole.Customer;
        userLogin.UserID = await m_userRepo.CreateAsync(user);
        user.ID = userLogin.UserID;
        await m_userLoginRepo.CreateAsync(userLogin);
    }

    public async Task CreateEmployeeAsync(Guid requestingUserId, User employee, string password)
    {
        await CreateStaffAsync(requestingUserId, employee, password, UserRole.Employee, DiscountRole.Employee);
    }

    public async Task CreateAdminAsync(Guid requestingUserId, User admin, string password)
    {
        await CreateStaffAsync(requestingUserId, admin, password, UserRole.Admin, DiscountRole.Admin);
    }

    public async Task UpdateStaffAsync(Guid requestingUserId, User user, UserRole expectedRole)
    {
        await EnsureAdminAsync(requestingUserId);
        ArgumentNullException.ThrowIfNull(user);
        EnsureStaffRole(expectedRole);

        User existingUser = await m_userRepo.GetByID(user.ID);
        if (existingUser.Role != expectedRole)
        {
            throw new InvalidOperationException("The selected user is no longer in this staff role.");
        }

        if (await m_userRepo.EmailExistsForAnotherUserAsync(user.Email, user.ID))
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        user.Role = expectedRole;
        user.DiscountRole = existingUser.DiscountRole;
        await m_userRepo.UpdateStaffAsync(user, expectedRole);
    }

    public async Task UpdateProfileAsync(Guid userId, string firstName, string lastName, string email, string phoneNumber)
    {
        User user = await m_userRepo.GetByID(userId);

        if (await m_userRepo.EmailExistsForAnotherUserAsync(email, userId))
        {
            throw new InvalidOperationException("Denne e-mailadresse er allerede knyttet til en anden konto.");
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
        user.PhoneNumber = phoneNumber;

        await m_userRepo.UpdateProfileAsync(user);
    }

    public async Task UpdateProfileAddressAsync(
        Guid userId,
        string streetAddress,
        string? apartment,
        string postalCode,
        string city)
    {
        await m_userRepo.GetByID(userId);
        await m_userRepo.UpdateProfileAddressAsync(
            userId,
            streetAddress,
            apartment,
            postalCode,
            city);
    }

    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string confirmNewPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmNewPassword);

        if (!m_validationService.IsValidPassword(newPassword))
        {
            throw new InvalidOperationException("Den nye adgangskode opfylder ikke kravene.");
        }

        if (newPassword != confirmNewPassword)
        {
            throw new InvalidOperationException("De to nye adgangskoder er ikke ens.");
        }

        UserLogin login = await m_userLoginRepo.GetByUserIDAsync(userId);
        if (!m_hashingService.VerifyPassword(currentPassword, login.PhcString).Item1)
        {
            throw new InvalidOperationException("Den nuværende adgangskode er forkert.");
        }

        if (m_hashingService.VerifyPassword(newPassword, login.PhcString).Item1)
        {
            throw new InvalidOperationException("Den nye adgangskode skal være forskellig fra den nuværende.");
        }

        string passwordHash = m_hashingService.HashPassword(newPassword);
        await m_userLoginRepo.UpdatePasswordHashAsync(userId, passwordHash);
    }

    public async Task DeleteStaffAsync(Guid requestingUserId, Guid userId, UserRole expectedRole)
    {
        await EnsureAdminAsync(requestingUserId);
        EnsureStaffRole(expectedRole);

        if (requestingUserId == userId)
        {
            throw new InvalidOperationException("Du kan ikke fjerne din egen konto.");
        }

        await m_userRepo.DeleteStaffAsync(userId, expectedRole);
    }

    public async Task DeleteCustomerAsync(Guid requestingUserId, Guid customerId)
    {
        await EnsureAdminAsync(requestingUserId);
        await m_userRepo.DeleteCustomerOlderThan30DaysAsync(customerId);
    }

    public async Task UpdateCustomerDiscountRoleAsync(
        Guid requestingUserId,
        Guid customerId,
        DiscountRole discountRole)
    {
        await EnsureAdminAsync(requestingUserId);
        if (!Enum.IsDefined(discountRole))
        {
            throw new ArgumentOutOfRangeException(nameof(discountRole), "The discount role is invalid.");
        }

        User customer = await m_userRepo.GetByID(customerId);
        if (customer.Role != UserRole.Customer)
        {
            throw new NotFoundException("Customer not found.");
        }

        await m_userRepo.UpdateCustomerDiscountRoleAsync(customerId, discountRole);
    }

    public async Task DeleteOwnCustomerAccountAsync(Guid requestingUserId, Guid customerId)
    {
        if (requestingUserId != customerId)
        {
            throw new UnauthorizedAccessException("Customers can only delete their own account.");
        }

        await m_userRepo.DeleteCustomerAccountAsync(customerId);
    }

    private async Task CreateStaffAsync(
        Guid requestingUserId,
        User user,
        string password,
        UserRole role,
        DiscountRole discountRole)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        await EnsureAdminAsync(requestingUserId);

        if (await m_userRepo.EmailExistsAsync(user.Email))
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        user.Role = role;
        user.DiscountRole = discountRole;
        user.ID = await m_userRepo.CreateAsync(user);

        await m_userLoginRepo.CreateAsync(new UserLogin
        {
            UserID = user.ID,
            PhcString = m_hashingService.HashPassword(password)
        });
    }

    private async Task EnsureAdminAsync(Guid requestingUserId)
    {
        User requestingUser;
        try
        {
            requestingUser = await m_userRepo.GetByID(requestingUserId);
        }
        catch (NotFoundException)
        {
            throw new UnauthorizedAccessException("Only administrators can manage staff accounts.");
        }

        if (requestingUser.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only administrators can manage staff accounts.");
        }
    }

    private static void EnsureStaffRole(UserRole role)
    {
        if (role is not (UserRole.Admin or UserRole.Employee))
        {
            throw new ArgumentOutOfRangeException(nameof(role), "Only staff accounts can be managed here.");
        }
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

    public async Task<(List<User> Users, int TotalCount)> GetUsersByRolePageAsync(
        UserRole role,
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await m_userRepo.GetByRolePageAsync(role, offset, pageSize, cancellationToken);
    }

    // public async Task CreateAsync(User user, string userLogin)
    // {
    //     Guid userID = await m_userRepo.CreateCustomerAsync(user);
    //     user.ID = userid;
    //     await m_userLoginRepo.CreateAsync(userLogin);
    // }

}
