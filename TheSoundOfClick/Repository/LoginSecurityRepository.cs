using Npgsql;

namespace TheSoundOfClick;

public class LoginSecurityRepository(NpgsqlDataSource dataSource)
{
    public async Task EnsureCreatedAsync()
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS login_security (
                user_id uuid PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
                failed_attempts integer NOT NULL DEFAULT 0 CHECK (failed_attempts BETWEEN 0 AND 3),
                is_locked boolean NOT NULL DEFAULT false,
                approval_token_hash text
            );
            CREATE UNIQUE INDEX IF NOT EXISTS login_security_approval_token_hash_idx
                ON login_security (approval_token_hash)
                WHERE approval_token_hash IS NOT NULL;
            """,
            connection);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> IsLockedAsync(Guid userId)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT is_locked FROM login_security WHERE user_id = @user_id",
            connection);
        command.Parameters.AddWithValue("user_id", userId);

        object? result = await command.ExecuteScalarAsync();
        return result is true;
    }

    public async Task<bool> RecordFailedAttemptAsync(Guid userId, string approvalTokenHash)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO login_security (user_id, failed_attempts)
            VALUES (@user_id, 1)
            ON CONFLICT (user_id) DO UPDATE
            SET failed_attempts = CASE
                    WHEN login_security.is_locked THEN login_security.failed_attempts
                    ELSE login_security.failed_attempts + 1
                END,
                is_locked = login_security.is_locked OR login_security.failed_attempts >= 2,
                approval_token_hash = CASE
                    WHEN NOT login_security.is_locked AND login_security.failed_attempts >= 2
                        THEN @approval_token_hash
                    ELSE login_security.approval_token_hash
                END
            RETURNING is_locked AND approval_token_hash = @approval_token_hash
            """,
            connection);
        command.Parameters.AddWithValue("user_id", userId);
        command.Parameters.AddWithValue("approval_token_hash", approvalTokenHash);

        object? result = await command.ExecuteScalarAsync();
        return result is true;
    }

    public async Task ClearAsync(Guid userId)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "DELETE FROM login_security WHERE user_id = @user_id",
            connection);
        command.Parameters.AddWithValue("user_id", userId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task CancelLockAsync(Guid userId, string approvalTokenHash)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            UPDATE login_security
            SET failed_attempts = 2,
                is_locked = false,
                approval_token_hash = NULL
            WHERE user_id = @user_id
              AND approval_token_hash = @approval_token_hash
            """,
            connection);
        command.Parameters.AddWithValue("user_id", userId);
        command.Parameters.AddWithValue("approval_token_hash", approvalTokenHash);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> ApproveAsync(string approvalTokenHash)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            DELETE FROM login_security
            WHERE is_locked
              AND approval_token_hash = @approval_token_hash
            RETURNING user_id
            """,
            connection);
        command.Parameters.AddWithValue("approval_token_hash", approvalTokenHash);

        object? result = await command.ExecuteScalarAsync();
        return result is Guid;
    }
}
