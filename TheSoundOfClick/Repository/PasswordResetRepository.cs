using Npgsql;

namespace TheSoundOfClick;

public class PasswordResetRepository(NpgsqlDataSource dataSource)
{
    public async Task CreateTokenAsync(Guid userId, string tokenHash)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using var deleteCommand = new NpgsqlCommand(
            "DELETE FROM password_reset_tokens WHERE user_id = @user_id AND used_at IS NULL",
            connection,
            transaction);
        deleteCommand.Parameters.AddWithValue("user_id", userId);
        await deleteCommand.ExecuteNonQueryAsync();

        await using var insertCommand = new NpgsqlCommand(
            """
            INSERT INTO password_reset_tokens (user_id, token_hash, expires_at)
            VALUES (@user_id, @token_hash, now() + interval '30 minutes')
            """,
            connection,
            transaction);
        insertCommand.Parameters.AddWithValue("user_id", userId);
        insertCommand.Parameters.AddWithValue("token_hash", tokenHash);
        await insertCommand.ExecuteNonQueryAsync();

        await transaction.CommitAsync();
    }

    public async Task DeleteTokenAsync(string tokenHash)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "DELETE FROM password_reset_tokens WHERE token_hash = @token_hash AND used_at IS NULL",
            connection);
        command.Parameters.AddWithValue("token_hash", tokenHash);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> ResetPasswordAsync(string tokenHash, string passwordHash)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        Guid userId;
        await using (var tokenCommand = new NpgsqlCommand(
            """
            SELECT user_id
            FROM password_reset_tokens
            WHERE token_hash = @token_hash
              AND used_at IS NULL
              AND expires_at > now()
            FOR UPDATE
            """,
            connection,
            transaction))
        {
            tokenCommand.Parameters.AddWithValue("token_hash", tokenHash);
            object? result = await tokenCommand.ExecuteScalarAsync();
            if (result is not Guid tokenUserId)
            {
                return false;
            }

            userId = tokenUserId;
        }

        await using var passwordCommand = new NpgsqlCommand(
            """
            UPDATE user_logins
            SET password_hash = @password_hash
            WHERE user_id = @user_id
            """,
            connection,
            transaction);
        passwordCommand.Parameters.AddWithValue("password_hash", passwordHash);
        passwordCommand.Parameters.AddWithValue("user_id", userId);
        if (await passwordCommand.ExecuteNonQueryAsync() != 1)
        {
            throw new NotFoundException("Login record not found.");
        }

        await using var consumeCommand = new NpgsqlCommand(
            """
            UPDATE password_reset_tokens
            SET used_at = now()
            WHERE user_id = @user_id
              AND used_at IS NULL
            """,
            connection,
            transaction);
        consumeCommand.Parameters.AddWithValue("user_id", userId);
        await consumeCommand.ExecuteNonQueryAsync();

        await transaction.CommitAsync();
        return true;
    }
}
