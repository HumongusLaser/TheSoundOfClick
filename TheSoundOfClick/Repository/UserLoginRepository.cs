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

    public async Task Create(UserLogin userLogin)
    {
        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO user_logins(user_id, email, password_hash) VALUES(
            @user_id, @email, @password_hash)
            RETURNING id
            """,
            connection
        );

        command.Parameters.AddWithValue("user_id", userLogin.UserID);
        command.Parameters.AddWithValue("email", userLogin.Email);
        command.Parameters.AddWithValue("password_hash", userLogin.PhcString);

        userLogin.ID = (Guid?)await command.ExecuteScalarAsync() ?? throw new SqlNullValueException($"Could not create the { nameof(UserLogin) }");
    }
}