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

    public async Task<bool> EmailExistsForAnotherUserAsync(string email, Guid userId)
    {
        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1
                FROM users
                WHERE LOWER(email) = LOWER(@email)
                  AND id <> @user_id
            )
            """,
            connection);

        command.Parameters.AddWithValue("email", email);
        command.Parameters.AddWithValue("user_id", userId);
        return (bool?)await command.ExecuteScalarAsync() ?? false;
    }

    public async Task<Guid> CreateAsync(User user)
    {
        UserRoleDBV[] userRoleDBVs = await GetUserRoleDBVsAsync();
        DiscountRoleDBV[] discountRoleDBVs = await GetDiscountTypeDBVsAsync();

        Guid role = userRoleDBVs.FirstOrDefault(u => u.Role == user.Role.ToString())?.ID ?? throw new NpgsqlException("Customer type not found internal DB naming error");
        Guid discountRole = discountRoleDBVs.FirstOrDefault(u => u.Role == user.DiscountRole.ToString())?.ID ?? throw new NpgsqlException("Customer discount type not found internal DB naming error");
        
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
        cmd.Parameters.AddWithValue("apartment", string.IsNullOrWhiteSpace(user.Apartment) ? DBNull.Value : user.Apartment);
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
                Apartment = reader.IsDBNull(5) ? null : reader.GetString(5),
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
                Apartment = reader.IsDBNull(6) ? null : reader.GetString(6),
                PostalCode = reader.GetString(7),
                City = reader.GetString(8),
                PhoneNumber = reader.GetString(9),
                Role = await GetUserRole(role),
                DiscountRole = await GetDiscountRole(discountRole)
            };
        }

        return user ?? throw new NotFoundException("User not found");
    }

    public async Task UpdateProfileAsync(User user)
    {
        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            UPDATE users
            SET first_name = @first_name,
                last_name = @last_name,
                email = @email,
                phone_number = @phone_number
            WHERE id = @id
            """,
            connection);

        command.Parameters.AddWithValue("first_name", user.FirstName);
        command.Parameters.AddWithValue("last_name", user.LastName);
        command.Parameters.AddWithValue("email", user.Email);
        command.Parameters.AddWithValue("phone_number", user.PhoneNumber);
        command.Parameters.AddWithValue("id", user.ID);

        if (await command.ExecuteNonQueryAsync() != 1)
        {
            throw new NotFoundException("User not found.");
        }
    }

    public async Task UpdateProfileAddressAsync(Guid userId, string streetAddress, string? apartment, string postalCode, string city)
    {
        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            UPDATE users
            SET street_address = @street_address,
                apartment = @apartment,
                postal_code = @postal_code,
                city = @city
            WHERE id = @id
            """,
            connection);

        command.Parameters.AddWithValue("street_address", streetAddress);
        command.Parameters.Add(new NpgsqlParameter("apartment", NpgsqlTypes.NpgsqlDbType.Text)
        {
            Value = string.IsNullOrWhiteSpace(apartment) ? DBNull.Value : apartment
        });
        command.Parameters.AddWithValue("postal_code", postalCode);
        command.Parameters.AddWithValue("city", city);
        command.Parameters.AddWithValue("id", userId);

        if (await command.ExecuteNonQueryAsync() != 1)
        {
            throw new NotFoundException("User not found.");
        }
    }

    public async Task UpdateStaffAsync(User user, UserRole expectedRole)
    {
        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            UPDATE users
            SET first_name = @first_name,
                last_name = @last_name,
                email = @email,
                street_address = @street_address,
                apartment = @apartment,
                postal_code = @postal_code,
                city = @city,
                phone_number = @phone_number
            WHERE id = @id
              AND role = (SELECT id FROM user_roles WHERE role = @role)
            """,
            connection);

        command.Parameters.AddWithValue("first_name", user.FirstName);
        command.Parameters.AddWithValue("last_name", user.LastName);
        command.Parameters.AddWithValue("email", user.Email);
        command.Parameters.AddWithValue("street_address", user.StreetAddress);
        command.Parameters.Add(new NpgsqlParameter("apartment", NpgsqlTypes.NpgsqlDbType.Text)
        {
            Value = string.IsNullOrWhiteSpace(user.Apartment) ? DBNull.Value : user.Apartment
        });
        command.Parameters.AddWithValue("postal_code", user.PostalCode);
        command.Parameters.AddWithValue("city", user.City);
        command.Parameters.AddWithValue("phone_number", user.PhoneNumber);
        command.Parameters.AddWithValue("id", user.ID);
        command.Parameters.AddWithValue("role", expectedRole.ToString());

        if (await command.ExecuteNonQueryAsync() != 1)
        {
            throw new NotFoundException("Staff user not found.");
        }
    }

    public async Task DeleteStaffAsync(Guid userId, UserRole expectedRole)
    {
        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using var roleCommand = new NpgsqlCommand(
            "SELECT id FROM user_roles WHERE role = @role FOR UPDATE",
            connection,
            transaction);
        roleCommand.Parameters.AddWithValue("role", expectedRole.ToString());
        object? roleResult = await roleCommand.ExecuteScalarAsync();
        if (roleResult is not Guid roleId)
        {
            throw new InvalidOperationException($"Role {expectedRole} was not found.");
        }

        if (expectedRole == UserRole.Admin)
        {
            await using var countCommand = new NpgsqlCommand(
                "SELECT COUNT(*) FROM users WHERE role = @role_id",
                connection,
                transaction);
            countCommand.Parameters.AddWithValue("role_id", roleId);
            long adminCount = (long)(await countCommand.ExecuteScalarAsync()
                ?? throw new NpgsqlException("Could not count administrators."));
            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Den sidste administrator kan ikke fjernes.");
            }
        }

        await using var loginCommand = new NpgsqlCommand(
            "DELETE FROM user_logins WHERE user_id = @user_id",
            connection,
            transaction);
        loginCommand.Parameters.AddWithValue("user_id", userId);
        await loginCommand.ExecuteNonQueryAsync();

        await using var userCommand = new NpgsqlCommand(
            "DELETE FROM users WHERE id = @user_id AND role = @role_id",
            connection,
            transaction);
        userCommand.Parameters.AddWithValue("user_id", userId);
        userCommand.Parameters.AddWithValue("role_id", roleId);
        if (await userCommand.ExecuteNonQueryAsync() != 1)
        {
            throw new NotFoundException("Staff user not found.");
        }

        await transaction.CommitAsync();
    }

    public async Task DeleteCustomerOlderThan30DaysAsync(Guid userId)
    {
        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using var customerCommand = new NpgsqlCommand(
            """
            SELECT ur.role, u.register_date < CURRENT_DATE - 30
            FROM users u
            INNER JOIN user_roles ur ON ur.id = u.role
            WHERE u.id = @user_id
            FOR UPDATE OF u
            """,
            connection,
            transaction);
        customerCommand.Parameters.AddWithValue("user_id", userId);

        await using var reader = await customerCommand.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new NotFoundException("Customer not found.");
        }

        string role = reader.GetString(0);
        bool isOlderThan30Days = reader.GetBoolean(1);
        await reader.DisposeAsync();

        if (!string.Equals(role, UserRole.Customer.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only customer accounts can be deleted from the customer overview.");
        }

        if (!isOlderThan30Days)
        {
            throw new InvalidOperationException("Kunden skal have været registreret i mere end 30 dage, før kontoen kan slettes.");
        }

        await using var loginCommand = new NpgsqlCommand(
            "DELETE FROM user_logins WHERE user_id = @user_id",
            connection,
            transaction);
        loginCommand.Parameters.AddWithValue("user_id", userId);
        await loginCommand.ExecuteNonQueryAsync();

        await using var deleteCommand = new NpgsqlCommand(
            """
            DELETE FROM users
            WHERE id = @user_id
              AND role = (SELECT id FROM user_roles WHERE role = @customer_role)
              AND register_date < CURRENT_DATE - 30
            """,
            connection,
            transaction);
        deleteCommand.Parameters.AddWithValue("user_id", userId);
        deleteCommand.Parameters.AddWithValue("customer_role", UserRole.Customer.ToString());

        if (await deleteCommand.ExecuteNonQueryAsync() != 1)
        {
            throw new InvalidOperationException("Kunden opfylder ikke længere betingelserne for sletning.");
        }

        await transaction.CommitAsync();
    }

    public async Task DeleteCustomerAccountAsync(Guid userId)
    {
        await using var connection = await m_dbSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using var customerCommand = new NpgsqlCommand(
            """
            SELECT ur.role
            FROM users u
            INNER JOIN user_roles ur ON ur.id = u.role
            WHERE u.id = @user_id
            FOR UPDATE OF u
            """,
            connection,
            transaction);
        customerCommand.Parameters.AddWithValue("user_id", userId);

        object? roleResult = await customerCommand.ExecuteScalarAsync();
        if (roleResult is not string role)
        {
            throw new NotFoundException("Customer account not found.");
        }

        if (!string.Equals(role, UserRole.Customer.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Only customers can delete their own account.");
        }

        await using var loginCommand = new NpgsqlCommand(
            "DELETE FROM user_logins WHERE user_id = @user_id",
            connection,
            transaction);
        loginCommand.Parameters.AddWithValue("user_id", userId);
        await loginCommand.ExecuteNonQueryAsync();

        await using var deleteCommand = new NpgsqlCommand(
            """
            DELETE FROM users
            WHERE id = @user_id
              AND role = (SELECT id FROM user_roles WHERE role = @customer_role)
            """,
            connection,
            transaction);
        deleteCommand.Parameters.AddWithValue("user_id", userId);
        deleteCommand.Parameters.AddWithValue("customer_role", UserRole.Customer.ToString());
        if (await deleteCommand.ExecuteNonQueryAsync() != 1)
        {
            throw new NotFoundException("Customer account not found.");
        }

        await transaction.CommitAsync();
    }

    public async Task<(List<User> Users, int TotalCount)> GetByRolePageAsync(
        UserRole userRole,
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        await using var connection = await m_dbSource.OpenConnectionAsync(cancellationToken);
        await using var countCommand = new NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM users u
            INNER JOIN user_roles ur ON ur.id = u.role
            WHERE ur.role = @role
            """,
            connection);
        countCommand.Parameters.AddWithValue("role", userRole.ToString());
        int totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var command = new NpgsqlCommand(
            """
            SELECT u.id, u.register_date, u.first_name, u.last_name, u.email,
                   u.street_address, u.apartment, u.postal_code, u.city, u.phone_number,
                   ur.role, dr.role
            FROM users u
            INNER JOIN user_roles ur ON ur.id = u.role
            INNER JOIN discount_roles dr ON dr.id = u.discount_role
            WHERE ur.role = @role
            ORDER BY u.last_name, u.first_name, u.id
            LIMIT @page_size OFFSET @offset
            """,
            connection);
        command.Parameters.AddWithValue("role", userRole.ToString());
        command.Parameters.AddWithValue("page_size", pageSize);
        command.Parameters.AddWithValue("offset", offset);

        List<User> users = [];
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!Enum.TryParse(reader.GetString(10), true, out UserRole role) ||
                !Enum.IsDefined(role) ||
                !Enum.TryParse(reader.GetString(11), true, out DiscountRole discountRole) ||
                !Enum.IsDefined(discountRole))
            {
                throw new FormatException($"User {reader.GetGuid(0)} has an unknown role.");
            }

            users.Add(new User
            {
                ID = reader.GetGuid(0),
                RegisterDate = reader.GetFieldValue<DateOnly>(1),
                FirstName = reader.GetString(2),
                LastName = reader.GetString(3),
                Email = reader.GetString(4),
                StreetAddress = reader.GetString(5),
                Apartment = reader.IsDBNull(6) ? null : reader.GetString(6),
                PostalCode = reader.GetString(7),
                City = reader.GetString(8),
                PhoneNumber = reader.GetString(9),
                Role = role,
                DiscountRole = discountRole
            });
        }

        return (users, totalCount);
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
