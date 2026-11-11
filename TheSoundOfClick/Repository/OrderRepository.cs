using Npgsql;

namespace TheSoundOfClick;

public class OrderRepository(NpgsqlDataSource dataSource)
{
    public async Task<List<OrderHistoryEntry>> GetOrdersByCustomerAsync(Guid customerId)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT o.id, o.date_created, o.status, o.total_price, o.shipping_address,
                   oi.product_name, oi.unit_price, oi.quantity
            FROM orders o
            LEFT JOIN order_items oi ON oi.order_id = o.id
            WHERE o.customer_id = @customer_id
            ORDER BY o.date_created DESC, o.id, oi.id
            """,
            connection);
        command.Parameters.AddWithValue("customer_id", customerId);

        List<OrderHistoryEntry> orders = [];
        Dictionary<Guid, OrderHistoryEntry> ordersById = [];
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Guid orderId = reader.GetGuid(0);
            if (!ordersById.TryGetValue(orderId, out OrderHistoryEntry? order))
            {
                order = new OrderHistoryEntry
                {
                    ID = orderId,
                    DateCreated = reader.GetDateTime(1),
                    Status = reader.GetString(2),
                    TotalPrice = reader.GetDecimal(3),
                    ShippingAddress = reader.GetString(4)
                };
                ordersById.Add(orderId, order);
                orders.Add(order);
            }

            if (!reader.IsDBNull(5))
            {
                order.Items.Add(new OrderHistoryItem
                {
                    ProductName = reader.GetString(5),
                    UnitPrice = reader.GetDecimal(6),
                    Quantity = reader.GetInt32(7)
                });
            }
        }

        return orders;
    }

    public async Task<Order> CheckoutCartAsync(
        Guid cartId,
        Guid userId,
        string shippingAddress)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using var cartCommand = new NpgsqlCommand(
            "SELECT user_id FROM carts WHERE id = @cart_id FOR UPDATE",
            connection,
            transaction);
        cartCommand.Parameters.AddWithValue("cart_id", cartId);
        object? owner = await cartCommand.ExecuteScalarAsync();
        if (owner is not Guid ownerId || ownerId != userId)
        {
            throw new UnauthorizedAccessException("The cart does not belong to the signed-in customer.");
        }

        List<CartLine> purchasedLines = [];
        await using (var itemsCommand = new NpgsqlCommand(
            """
            SELECT ci.id, ci.cart_id, ci.product_id, ci.quantity, ci.created_at, ci.updated_at,
                   p.name, p.price, p.stock, p.color, p.description, p.net_weight, p.category, p.details
            FROM cart_items ci
            INNER JOIN products p ON p.id = ci.product_id
            WHERE ci.cart_id = @cart_id
            ORDER BY p.id
            FOR UPDATE OF ci, p
            """,
            connection,
            transaction))
        {
            itemsCommand.Parameters.AddWithValue("cart_id", cartId);
            await using var reader = await itemsCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int quantity = reader.GetInt32(3);
                int currentStock = reader.GetInt32(8);
                if (quantity > currentStock)
                {
                    throw new InvalidOperationException(
                        $"Der er ikke nok på lager af {reader.GetString(6)}. Der er kun {currentStock} stk. tilbage.");
                }

                Product product = ProductFactory.Create(reader.GetString(12), reader.GetString(13));
                product.ID = reader.GetGuid(2);
                product.Name = reader.GetString(6);
                product.Price = reader.GetDecimal(7);
                product.Stock = currentStock - quantity;
                product.Color = reader.GetString(9);
                product.Description = reader.GetString(10);
                product.NetWeight = reader.GetInt32(11);

                purchasedLines.Add(new CartLine
                {
                    Item = new CartItem
                    {
                        ID = reader.GetGuid(0),
                        CartID = reader.GetGuid(1),
                        ProductID = reader.GetGuid(2),
                        Quantity = quantity,
                        CreatedAt = reader.GetDateTime(4),
                        UpdatedAt = reader.GetDateTime(5)
                    },
                    Product = product
                });
            }
        }

        if (purchasedLines.Count == 0)
        {
            throw new InvalidOperationException("Din kurv er tom.");
        }

        Order order = new(
            Guid.NewGuid(),
            userId,
            DateTime.UtcNow,
            "Placed",
            purchasedLines.Sum(line => line.Product.Price * line.Item.Quantity),
            shippingAddress,
            purchasedLines.ToDictionary(line => line.Product, line => line.Item.Quantity));

        await using (var orderCommand = new NpgsqlCommand(
            """
            INSERT INTO orders (id, customer_id, date_created, status, total_price, shipping_address)
            VALUES (@id, @customer_id, @date_created, @status, @total_price, @shipping_address)
            """,
            connection,
            transaction))
        {
            orderCommand.Parameters.AddWithValue("id", order.ID);
            orderCommand.Parameters.AddWithValue("customer_id", order.CustomerID);
            orderCommand.Parameters.AddWithValue("date_created", order.DateCreated);
            orderCommand.Parameters.AddWithValue("status", order.Status);
            orderCommand.Parameters.AddWithValue("total_price", order.TotalPrice);
            orderCommand.Parameters.AddWithValue("shipping_address", order.ShippingAddress);
            await orderCommand.ExecuteNonQueryAsync();
        }

        foreach (CartLine line in purchasedLines)
        {
            await using var stockCommand = new NpgsqlCommand(
                """
                UPDATE products
                SET stock = stock - @quantity
                WHERE id = @product_id
                  AND stock >= @quantity
                """,
                connection,
                transaction);
            stockCommand.Parameters.AddWithValue("product_id", line.Item.ProductID);
            stockCommand.Parameters.AddWithValue("quantity", line.Item.Quantity);

            if (await stockCommand.ExecuteNonQueryAsync() != 1)
            {
                throw new InvalidOperationException(
                    $"Lagerbeholdningen for {line.Product.Name} ændrede sig. Opdatér kurven, og prøv igen.");
            }

            await using var orderItemCommand = new NpgsqlCommand(
                """
                INSERT INTO order_items (order_id, product_id, product_name, unit_price, quantity)
                VALUES (@order_id, @product_id, @product_name, @unit_price, @quantity)
                """,
                connection,
                transaction);
            orderItemCommand.Parameters.AddWithValue("order_id", order.ID);
            orderItemCommand.Parameters.AddWithValue("product_id", line.Item.ProductID);
            orderItemCommand.Parameters.AddWithValue("product_name", line.Product.Name);
            orderItemCommand.Parameters.AddWithValue("unit_price", line.Product.Price);
            orderItemCommand.Parameters.AddWithValue("quantity", line.Item.Quantity);
            await orderItemCommand.ExecuteNonQueryAsync();
        }

        await using var clearCartCommand = new NpgsqlCommand(
            "DELETE FROM cart_items WHERE cart_id = @cart_id AND id = ANY(@item_ids)",
            connection,
            transaction);
        clearCartCommand.Parameters.AddWithValue("cart_id", cartId);
        clearCartCommand.Parameters.AddWithValue(
            "item_ids",
            purchasedLines.Select(line => line.Item.ID).ToArray());
        await clearCartCommand.ExecuteNonQueryAsync();

        await transaction.CommitAsync();
        return order;
    }
}
