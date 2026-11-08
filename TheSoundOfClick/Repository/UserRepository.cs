using Npgsql;

namespace TheSoundOfClick;

public class UserRepository(NpgsqlDataSource m_dbSource)
{
    public async Task<bool> EmailExistsAsync(string email)
    {
        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1
                FROM users
                WHERE LOWER(email) = LOWER(@email)
            )
            """,
            connection);

        command.Parameters.AddWithValue("email", email);
        return (bool?)await command.ExecuteScalarAsync() ?? false;
    }

    public async Task<Guid> CreateCustomerAsync(User user)
    {
        UserRoleDBV[] userRoleDBVs = await GetUserRoleDBVsAsync();
        DiscountRoleDBV[] discountRoleDBVs = await GetDiscountTypeDBVsAsync();

        Guid role = userRoleDBVs.FirstOrDefault(u => u.Role == "Customer")?.ID ?? throw new NpgsqlException("Customer type not found internal DB naming error");
        Guid discountRole = discountRoleDBVs.FirstOrDefault(u => u.Role == "Customer")?.ID ?? throw new NpgsqlException("Customer discount type not found internal DB naming error");
        
        string commandStr = 
        $"""
        INSERT INTO users (register_date, first_name, last_name, email, street_address, apartment, postal_code, city, phone_number, role, discount_role)
        VALUES (@register_date, @first_name, @last_name, @email, @street_address, @apartment, @postal_code, @city, @phone_number, @role, @discount_role)
        RETURNING id
        """;

        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(commandStr, connection);

        cmd.Parameters.AddWithValue("register_date", user.RegisterDate);
        cmd.Parameters.AddWithValue("first_name", user.FirstName);
        cmd.Parameters.AddWithValue("last_name", user.LastName);
        cmd.Parameters.AddWithValue("email", user.Email);
        cmd.Parameters.AddWithValue("street_address", user.StreetAddress);
        cmd.Parameters.AddWithValue("apartment", (object?)user.Apartment ?? DBNull.Value);
        cmd.Parameters.AddWithValue("postal_code", user.PostalCode);
        cmd.Parameters.AddWithValue("city", user.City);
        cmd.Parameters.AddWithValue("phone_number", user.PhoneNumber);
        cmd.Parameters.AddWithValue("role", role);
        cmd.Parameters.AddWithValue("discount_role", discountRole);
        
        Guid userID = (Guid?)await cmd.ExecuteScalarAsync() ?? throw new NpgsqlException("Internal error could not save user in DB");
        return userID;
    }

    public async Task<User> GetByID(Guid id)
    {
        string commandStr = 
        """
        SELECT register_date, first_name, last_name, email, street_address, apartment, postal_code, city, phone_number, role, discount_role
        FROM users
        WHERE users.id = @id
        """;
        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(commandStr, connection);

        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        User? user = null;

        while (await reader.ReadAsync())
        {
            Guid role = reader.GetGuid(9);
            Guid discountRole = reader.GetGuid(10);
            user = new()
            {
                ID = id,
                RegisterDate = reader.GetFieldValue<DateOnly>(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                Email = reader.GetString(3),
                StreetAddress = reader.GetString(4),
                Apartment = reader.GetFieldValue<string?>(5),
                PostalCode = reader.GetString(6),
                City = reader.GetString(7),
                PhoneNumber = reader.GetString(8),
                Role = await GetUserRole(role),
                DiscountRole = await GetDiscountRole(discountRole)
            };
        }

        return user ?? throw new NotFoundException("User not found");
    }

    public async Task<User> GetByEmail(string email)
    {
        string commandStr = 
        """
        SELECT id, register_date, first_name, last_name, email, street_address, apartment, postal_code, city, phone_number, role, discount_role
        FROM users
        WHERE LOWER(users.email) = LOWER(@email)
        """;
        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(commandStr, connection);

        cmd.Parameters.AddWithValue("email", email);

        await using var reader = await cmd.ExecuteReaderAsync();

        User? user = null;

        while (await reader.ReadAsync())
        {
            Guid role = reader.GetGuid(10);
            Guid discountRole = reader.GetGuid(11);
            user = new()
            {
                ID = reader.GetGuid(0),
                RegisterDate = reader.GetFieldValue<DateOnly>(1),
                FirstName = reader.GetString(2),
                LastName = reader.GetString(3),
                Email = reader.GetString(4),
                StreetAddress = reader.GetString(5),
                Apartment = reader.GetFieldValue<string?>(6),
                PostalCode = reader.GetString(7),
                City = reader.GetString(8),
                PhoneNumber = reader.GetString(9),
                Role = await GetUserRole(role),
                DiscountRole = await GetDiscountRole(discountRole)
            };
        }

        return user ?? throw new NotFoundException("User not found");
    }

    private async Task<UserRole> GetUserRole(Guid id)
    {
        string commandStr = 
        """
        SELECT role
        FROM user_roles
        WHERE id = @id
        """;

        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(commandStr, connection);

        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new NotFoundException("Discount role not found");
        }

        string userRoleStr = reader.GetString(0);
        bool enumParse = Enum.TryParse(typeof(UserRole), userRoleStr, true, out var userRole) && Enum.IsDefined(typeof(UserRole), userRole);
        if (!enumParse)
        {
            throw new FormatException("UserRole name is not the same in DB and Enum Type");
        }

        return (UserRole)userRole!;
    }

    private async Task<DiscountRole> GetDiscountRole(Guid id)
    {
        string commandStr = 
        """
        SELECT role
        FROM discount_roles
        WHERE id = @id
        """;

        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(commandStr, connection);

        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new NotFoundException("Discount role not found");
        }

        string discountRoleStr = reader.GetString(0);
        bool enumParse = Enum.TryParse(typeof(DiscountRole), discountRoleStr, true, out var discountRole) && Enum.IsDefined(typeof(DiscountRole), discountRole);
        if (!enumParse)
        {
            throw new FormatException("DiscountRole name is not the same in DB and Enum Type");
        }

        return (DiscountRole)discountRole!;
    }

    private async Task<UserRoleDBV[]> GetUserRoleDBVsAsync()
    {
        string commandStr = 
        """
        SELECT id, role
        FROM user_roles
        """;

        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(commandStr, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        List<UserRoleDBV> userRoles = [];

        while (await reader.ReadAsync())
        {
            UserRoleDBV userRole = new()
            {
                ID = reader.GetGuid(0),
                Role = reader.GetString(1),
            };
            userRoles.Add(userRole);
        }

        return userRoles.ToArray();
    }

    private async Task<DiscountRoleDBV[]> GetDiscountTypeDBVsAsync()
    {
        string commandStr = 
        """
        SELECT id, role
        FROM discount_roles
        """;

        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(commandStr, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        List<DiscountRoleDBV> discountRoles = [];

        while (await reader.ReadAsync())
        {
            DiscountRoleDBV userRole = new()
            {
                ID = reader.GetGuid(0),
                Role = reader.GetString(1),
            };
            discountRoles.Add(userRole);
        }

        return discountRoles.ToArray();
    }
}
