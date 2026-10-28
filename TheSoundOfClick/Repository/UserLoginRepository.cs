using Npgsql;

namespace TheSoundOfClick;

public class UserLoginRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public UserLoginRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<byte[]?> GetPasswordHashAsync(string email)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT ul.password_hash
            FROM user_logins ul
            JOIN users u ON ul.user_id = u.id
            WHERE LOWER(u.email) = LOWER(@email)
            """,
            connection
        );

        command.Parameters.AddWithValue("email", email);

        object? result = await command.ExecuteScalarAsync();

        return result as byte[];
    }
}