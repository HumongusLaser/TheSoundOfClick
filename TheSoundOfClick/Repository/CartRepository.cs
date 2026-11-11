using Npgsql;
using NpgsqlTypes;

namespace TheSoundOfClick;

public class CartRepository(NpgsqlDataSource m_dataSource)
{
    public async Task<Cart?> GetCartByIdAsync(Guid cartId)
    {
        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT id, user_id, created_at, updated_at
            FROM carts
            WHERE id = @cart_id
            """,
            connection);

        command.Parameters.AddWithValue("cart_id", cartId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new Cart
        {
            ID = reader.GetGuid(0),
            UserID = reader.IsDBNull(1) ? null : reader.GetGuid(1),
            CreatedAt = reader.GetDateTime(2),
            UpdatedAt = reader.GetDateTime(3)
        };
    }

    public async Task<Cart?> GetCartByUserIdAsync(Guid userId)
    {
        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT id, user_id, created_at, updated_at
            FROM carts
            WHERE user_id = @user_id
            """,
            connection);

        command.Parameters.AddWithValue("user_id", userId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new Cart
        {
            ID = reader.GetGuid(0),
            UserID = reader.IsDBNull(1) ? null : reader.GetGuid(1),
            CreatedAt = reader.GetDateTime(2),
            UpdatedAt = reader.GetDateTime(3)
        };
    }

    public async Task<List<CartLine>> GetCartLinesAsync(Guid cartId)
    {
        List<CartLine> cartLines = [];
        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT ci.id, ci.cart_id, ci.product_id, ci.quantity, ci.created_at, ci.updated_at,
                   p.name, p.price, p.stock, p.color, p.description, p.net_weight, p.category, p.details
            FROM cart_items ci
            INNER JOIN products p ON p.id = ci.product_id
            WHERE ci.cart_id = @cart_id
            ORDER BY ci.created_at
            """,
            connection);

        command.Parameters.AddWithValue("cart_id", cartId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Product product = ProductFactory.Create(reader.GetString(12), reader.GetString(13));
            product.ID = reader.GetGuid(2);
            product.Name = reader.GetString(6);
            product.Price = reader.GetDecimal(7);
            product.Stock = reader.GetInt32(8);
            product.Color = reader.GetString(9);
            product.Description = reader.GetString(10);
            product.NetWeight = reader.GetInt32(11);

            cartLines.Add(new CartLine
            {
                Item = new CartItem
                {
                    ID = reader.GetGuid(0),
                    CartID = reader.GetGuid(1),
                    ProductID = reader.GetGuid(2),
                    Quantity = reader.GetInt32(3),
                    CreatedAt = reader.GetDateTime(4),
                    UpdatedAt = reader.GetDateTime(5)
                },
                Product = product
            });
        }

        return cartLines;
    }

    public async Task<Cart> CreateCartAsync(Guid? userId = null)
    {
        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO carts (user_id)
            VALUES (@user_id)
            RETURNING id, user_id, created_at, updated_at
            """,
            connection);

        command.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlDbType.Uuid)
        {
            Value = userId.HasValue ? userId.Value : DBNull.Value
        });

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new NpgsqlException("Could not create cart.");
        }

        return new Cart
        {
            ID = reader.GetGuid(0),
            UserID = reader.IsDBNull(1) ? null : reader.GetGuid(1),
            CreatedAt = reader.GetDateTime(2),
            UpdatedAt = reader.GetDateTime(3)
        };
    }

    public async Task UpdateCartAsync(Guid cartId, Guid? userId)
    {
        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            UPDATE carts
            SET user_id = @user_id, updated_at = now()
            WHERE id = @cart_id
            """,
            connection);

        command.Parameters.AddWithValue("cart_id", cartId);
        command.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlDbType.Uuid)
        {
            Value = userId.HasValue ? userId.Value : DBNull.Value
        });

        if (await command.ExecuteNonQueryAsync() == 0)
        {
            throw new NotFoundException($"Cart with id {cartId} was not found.");
        }
    }

    public async Task DeleteCartAsync(Guid cartId)
    {
        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "DELETE FROM carts WHERE id = @cart_id",
            connection);

        command.Parameters.AddWithValue("cart_id", cartId);

        if (await command.ExecuteNonQueryAsync() == 0)
        {
            throw new NotFoundException($"Cart with id {cartId} was not found.");
        }
    }

    public async Task<CartItem> CreateCartItemAsync(Guid cartId, Guid productId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO cart_items (cart_id, product_id, quantity)
            VALUES (@cart_id, @product_id, @quantity)
            RETURNING id, cart_id, product_id, quantity, created_at, updated_at
            """,
            connection);

        command.Parameters.AddWithValue("cart_id", cartId);
        command.Parameters.AddWithValue("product_id", productId);
        command.Parameters.AddWithValue("quantity", quantity);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new NpgsqlException("Could not create cart item.");
        }

        return new CartItem
        {
            ID = reader.GetGuid(0),
            CartID = reader.GetGuid(1),
            ProductID = reader.GetGuid(2),
            Quantity = reader.GetInt32(3),
            CreatedAt = reader.GetDateTime(4),
            UpdatedAt = reader.GetDateTime(5)
        };
    }

    public async Task<CartItem> AddCartItemAsync(Guid cartId, Guid productId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            WITH upserted_item AS (
                INSERT INTO cart_items (cart_id, product_id, quantity)
                VALUES (@cart_id, @product_id, @quantity)
                ON CONFLICT (cart_id, product_id)
                DO UPDATE SET quantity = cart_items.quantity + EXCLUDED.quantity, updated_at = now()
                RETURNING id, cart_id, product_id, quantity, created_at, updated_at
            ),
            updated_cart AS (
                UPDATE carts SET updated_at = now() WHERE id = @cart_id
                RETURNING id
            )
            SELECT upserted_item.id, upserted_item.cart_id, upserted_item.product_id,
                   upserted_item.quantity, upserted_item.created_at, upserted_item.updated_at
            FROM upserted_item
            INNER JOIN updated_cart ON updated_cart.id = upserted_item.cart_id
            """,
            connection);

        command.Parameters.AddWithValue("cart_id", cartId);
        command.Parameters.AddWithValue("product_id", productId);
        command.Parameters.AddWithValue("quantity", quantity);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new NpgsqlException("Could not add cart item.");
        }

        return new CartItem
        {
            ID = reader.GetGuid(0),
            CartID = reader.GetGuid(1),
            ProductID = reader.GetGuid(2),
            Quantity = reader.GetInt32(3),
            CreatedAt = reader.GetDateTime(4),
            UpdatedAt = reader.GetDateTime(5)
        };
    }

    public async Task UpdateCartItemAsync(Guid cartItemId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            WITH updated_item AS (
                UPDATE cart_items
                SET quantity = @quantity, updated_at = now()
                WHERE id = @cart_item_id
                RETURNING cart_id
            ),
            updated_cart AS (
                UPDATE carts SET updated_at = now()
                WHERE id = (SELECT cart_id FROM updated_item)
            )
            SELECT cart_id FROM updated_item
            """,
            connection);

        command.Parameters.AddWithValue("cart_item_id", cartItemId);
        command.Parameters.AddWithValue("quantity", quantity);

        if (await command.ExecuteScalarAsync() is null)
        {
            throw new NotFoundException($"Cart item with id {cartItemId} was not found.");
        }
    }

    public async Task DeleteCartItemAsync(Guid cartItemId)
    {
        await using var connection = await m_dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            WITH deleted_item AS (
                DELETE FROM cart_items
                WHERE id = @cart_item_id
                RETURNING cart_id
            ),
            updated_cart AS (
                UPDATE carts SET updated_at = now()
                WHERE id = (SELECT cart_id FROM deleted_item)
            )
            SELECT cart_id FROM deleted_item
            """,
            connection);

        command.Parameters.AddWithValue("cart_item_id", cartItemId);

        if (await command.ExecuteScalarAsync() is null)
        {
            throw new NotFoundException($"Cart item with id {cartItemId} was not found.");
        }
    }

}
