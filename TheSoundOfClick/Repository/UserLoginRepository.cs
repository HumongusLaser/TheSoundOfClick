using System.Data.SqlTypes;
using Npgsql;

namespace TheSoundOfClick;

public class UserLoginRepository
{
    private readonly NpgsqlDataSource m_dataSource;

    public UserLoginRepository(NpgsqlDataSource dataSource)
    {
        m_dataSource = dataSource;
    }

    public async Task<string?> GetPasswordHashAsync(string email)
    {
        await using var connection = await m_dataSource.OpenConnectionAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT user_logins.password_hash
            FROM user_logins ul
            JOIN users u ON ul.id = u.id
            WHERE LOWER(u.email) = LOWER(@email)
            """,
            connection
        );

        command.Parameters.AddWithValue("email", email);

        object? result = await command.ExecuteScalarAsync();

        return result as string;
    }

    public async Task CreateAsync(UserLogin userLogin)
    {
        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO user_logins(user_id, password_hash) VALUES(
            @user_id, @password_hash)
            RETURNING id
            """,
            connection
        );

        command.Parameters.AddWithValue("user_id", userLogin.UserID);
        command.Parameters.AddWithValue("password_hash", userLogin.PhcString);

        userLogin.ID = (Guid?)await command.ExecuteScalarAsync() ?? throw new SqlNullValueException($"Could not create the { nameof(UserLogin) }");
    }

    public async Task<UserLogin> GetByUserIDAsync(Guid userID)
    {
        string commandStr = 
        """
        SELECT id, user_id, password_hash
        FROM user_logins
        WHERE user_id = @user_id
        """;

        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(commandStr, connection);

        cmd.Parameters.AddWithValue("user_id", userID);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new NotFoundException("Discount role not found");
        }

        UserLogin userLogin = new()
        {
            ID = reader.GetGuid(0),
            UserID = reader.GetGuid(1),
            PhcString = reader.GetString(2)
        };

        return userLogin;
    }

    public async Task UpdatePasswordHashAsync(Guid userId, string passwordHash)
    {
        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            UPDATE user_logins
            SET password_hash = @password_hash
            WHERE user_id = @user_id
            """,
            connection);

        command.Parameters.AddWithValue("password_hash", passwordHash);
        command.Parameters.AddWithValue("user_id", userId);

        if (await command.ExecuteNonQueryAsync() != 1)
        {
            throw new NotFoundException("Login record not found.");
        }
    }
}