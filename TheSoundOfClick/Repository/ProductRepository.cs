using System.Text;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;


namespace TheSoundOfClick;

public class ProductRepository
{
  private readonly NpgsqlDataSource m_dataSource;
  private readonly ILogger<ProductRepository> m_logger;

  public ProductRepository(NpgsqlDataSource dataSource, ILogger<ProductRepository> logger)
  {
    m_dataSource = dataSource;
    m_logger = logger;
  }

  public async Task EnsureImageTableCreatedAsync()
  {
    await using var connection = await m_dataSource.OpenConnectionAsync();
    await using var command = new NpgsqlCommand(
      """
      CREATE TABLE IF NOT EXISTS product_images (
          product_id uuid PRIMARY KEY REFERENCES products(id) ON DELETE CASCADE,
          image_path text NOT NULL
      )
      """,
      connection);
    await command.ExecuteNonQueryAsync();
  }

  public async Task<Guid> CreateAsync(Product product)
  {
    return await CreateAsync(product, null);
  }

  public async Task<Guid> CreateAsync(Product product, string? imagePath)
  {
    await using var connection = await m_dataSource.OpenConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync();
    await using var cmd = new NpgsqlCommand
    (
      @"INSERT INTO products (stock, name, price, color, description, net_weight, category, details)
        VALUES(@stock, @name, @price, @color, @description, @net_weight, @category, @details)
        RETURNING id", connection, transaction
    );


    cmd.Parameters.AddWithValue("stock", product.Stock);
    cmd.Parameters.AddWithValue("name", product.Name);
    cmd.Parameters.AddWithValue("price", product.Price);
    cmd.Parameters.AddWithValue("color", product.Color);
    cmd.Parameters.AddWithValue("description", product.Description);
    cmd.Parameters.AddWithValue("net_weight", product.NetWeight);
    cmd.Parameters.AddWithValue("category", ProductFactory.GetCategory(product));
    cmd.Parameters.Add(new NpgsqlParameter("details", NpgsqlDbType.Jsonb)
    {
      Value = ProductFactory.SerializeDetails(product)
    });

    Guid productId = (Guid)(await cmd.ExecuteScalarAsync()
      ?? throw new NpgsqlException("Could not create product."));

    if (imagePath is not null)
    {
      await using var imageCommand = new NpgsqlCommand(
        "INSERT INTO product_images (product_id, image_path) VALUES (@product_id, @image_path)",
        connection,
        transaction);
      imageCommand.Parameters.AddWithValue("product_id", productId);
      imageCommand.Parameters.AddWithValue("image_path", imagePath);
      await imageCommand.ExecuteNonQueryAsync();
    }

    await transaction.CommitAsync();
    return productId;
  }

  public async Task UpdateAsync(Product product, string? imagePath)
  {
    await using var connection = await m_dataSource.OpenConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync();
    await using var command = new NpgsqlCommand(
      """
      UPDATE products
      SET stock = @stock,
          name = @name,
          price = @price,
          color = @color,
          description = @description,
          net_weight = @net_weight,
          category = @category,
          details = @details
      WHERE id = @id
      """,
      connection,
      transaction);
    command.Parameters.AddWithValue("stock", product.Stock);
    command.Parameters.AddWithValue("name", product.Name);
    command.Parameters.AddWithValue("price", product.Price);
    command.Parameters.AddWithValue("color", product.Color);
    command.Parameters.AddWithValue("description", product.Description);
    command.Parameters.AddWithValue("net_weight", product.NetWeight);
    command.Parameters.AddWithValue("category", ProductFactory.GetCategory(product));
    command.Parameters.Add(new NpgsqlParameter("details", NpgsqlDbType.Jsonb)
    {
      Value = ProductFactory.SerializeDetails(product)
    });
    command.Parameters.AddWithValue("id", product.ID);

    if (await command.ExecuteNonQueryAsync() != 1)
    {
      throw new NotFoundException($"No such product with id {product.ID}");
    }

    if (imagePath is not null)
    {
      await using var imageCommand = new NpgsqlCommand(
        """
        INSERT INTO product_images (product_id, image_path)
        VALUES (@product_id, @image_path)
        ON CONFLICT (product_id) DO UPDATE SET image_path = EXCLUDED.image_path
        """,
        connection,
        transaction);
      imageCommand.Parameters.AddWithValue("product_id", product.ID);
      imageCommand.Parameters.AddWithValue("image_path", imagePath);
      await imageCommand.ExecuteNonQueryAsync();
    }

    await transaction.CommitAsync();
  }

  public async Task<string?> DeleteAsync(Guid productId)
  {
    await using var connection = await m_dataSource.OpenConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    await using (var lockCommand = new NpgsqlCommand(
      "SELECT id FROM products WHERE id = @id FOR UPDATE",
      connection,
      transaction))
    {
      lockCommand.Parameters.AddWithValue("id", productId);
      if (await lockCommand.ExecuteScalarAsync() is null)
      {
        throw new NotFoundException($"No such product with id {productId}");
      }
    }

    await using (var orderCommand = new NpgsqlCommand(
      "SELECT EXISTS (SELECT 1 FROM order_items WHERE product_id = @product_id)",
      connection,
      transaction))
    {
      orderCommand.Parameters.AddWithValue("product_id", productId);
      if ((bool?)await orderCommand.ExecuteScalarAsync() == true)
      {
        throw new InvalidOperationException("Produkter, der indgår i tidligere ordrer, kan ikke slettes.");
      }
    }

    string? imagePath;
    await using (var imageCommand = new NpgsqlCommand(
      "SELECT image_path FROM product_images WHERE product_id = @product_id",
      connection,
      transaction))
    {
      imageCommand.Parameters.AddWithValue("product_id", productId);
      imagePath = await imageCommand.ExecuteScalarAsync() as string;
    }

    await using (var cartCommand = new NpgsqlCommand(
      "DELETE FROM cart_items WHERE product_id = @product_id",
      connection,
      transaction))
    {
      cartCommand.Parameters.AddWithValue("product_id", productId);
      await cartCommand.ExecuteNonQueryAsync();
    }

    await using (var deleteCommand = new NpgsqlCommand(
      "DELETE FROM products WHERE id = @id",
      connection,
      transaction))
    {
      deleteCommand.Parameters.AddWithValue("id", productId);
      if (await deleteCommand.ExecuteNonQueryAsync() != 1)
      {
        throw new NotFoundException($"No such product with id {productId}");
      }
    }

    await transaction.CommitAsync();
    return imagePath;
  }

    public async Task<Product> GetProductAsync(Guid id)
    {
        Product? product = null;
        await using var connection = await m_dataSource.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(
            """
            SELECT p.id, p.name, p.price, p.stock, p.color, p.description,
                   p.net_weight, p.category, p.details, pi.image_path
            FROM products p
            LEFT JOIN product_images pi ON pi.product_id = p.id
            WHERE p.id = @id
            """,
            connection
        );
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            Guid gid = reader.GetGuid(0);
            string name = reader.GetString(1);
            decimal price = reader.GetDecimal(2);
            int stock = reader.GetInt32(3);
            string color = reader.GetString(4);
            string description = reader.GetString(5);
            int netWeight = reader.GetInt32(6);
            string category = reader.GetString(7);
            string details = reader.GetString(8);


            product = ProductFactory.Create(category, details);
            product.ID = gid;
            product.Name = name;
            product.Price = price;
            product.Stock = stock;
            product.Color = color;
            product.Description = description;
            product.NetWeight = netWeight;
            product.ImagePath = reader.IsDBNull(9) ? null : reader.GetString(9);
        }
        
        if (product == null)
        {
            throw new NotFoundException($"No such product with id {id}");
        }
        return product;
    }

    public async Task<List<Product>> GetProductsAsync(Guid? id, string? category, int batchSize)
    {
        List<Product> products = [];
        await using var connection = await m_dataSource.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(
            """
            SELECT p.id, p.name, p.price, p.stock, p.color, p.description,
                   p.net_weight, p.category, p.details, pi.image_path
            FROM products p
            LEFT JOIN product_images pi ON pi.product_id = p.id
            WHERE p.id > @id AND (@category IS NULL OR p.category = @category)
            ORDER BY p.id
            LIMIT @BatchSize
            """,
            connection);

        id = (id == null) ? new Guid() : id;

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.Add(new NpgsqlParameter("category", NpgsqlDbType.Text)
        {
            Value = category is null ? DBNull.Value : category
        });
        cmd.Parameters.AddWithValue("BatchSize", batchSize);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            Product? product = null;
            Guid gid = reader.GetGuid(0);
            string name = reader.GetString(1);
            decimal price = reader.GetDecimal(2);
            int stock = reader.GetInt32(3);
            string color = reader.GetString(4);
            string description = reader.GetString(5);
            int netWeight = reader.GetInt32(6);
            string pCategory = reader.GetString(7);
            string details = reader.GetString(8);

            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
            {
                m_logger.LogWarning(
                    "Skipping product {ProductId} in category {Category}: its name must contain at least two non-whitespace characters.",
                    gid,
                    pCategory);
                continue;
            }

            product = ProductFactory.Create(pCategory, details);
            product.ID = gid;
            product.Name = name.Trim();
            product.Price = price;
            product.Stock = stock;
            product.Color = color;
            product.Description = description;
            product.NetWeight = netWeight;
            product.ImagePath = reader.IsDBNull(9) ? null : reader.GetString(9);

            products.Add(product);   
        }
        return products;
    }

    public async Task<(List<Product> Products, int TotalCount)> GetProductPageAsync(
        int offset,
        int pageSize,
        string? category,
        string? search,
        string sortOrder,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        const string filter = """
            WHERE length(btrim(p.name)) >= 2
              AND (@category IS NULL OR p.category = @category)
              AND (
                  @search IS NULL
                  OR p.name ILIKE @search
                  OR p.id::text ILIKE @search
              )
            """;

        await using var connection = await m_dataSource.OpenConnectionAsync(cancellationToken);

        await using var countCommand = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM products p {filter}",
            connection);
        AddProductSearchParameters(countCommand, category, search);
        int totalCount = Convert.ToInt32(
            await countCommand.ExecuteScalarAsync(cancellationToken));

        const string orderBy = """
            ORDER BY
                CASE WHEN @sort_order = 'name' THEN p.name END ASC,
                CASE WHEN @sort_order = 'priceAsc' THEN p.price END ASC,
                CASE WHEN @sort_order = 'priceDesc' THEN p.price END DESC,
                p.id ASC
            """;
        await using var pageCommand = new NpgsqlCommand(
            $"""
            SELECT p.id, p.name, p.price, p.stock, p.color, p.description,
                   p.net_weight, p.category, p.details, pi.image_path
            FROM products p
            LEFT JOIN product_images pi ON pi.product_id = p.id
            {filter}
            {orderBy}
            LIMIT @page_size OFFSET @offset
            """,
            connection);

        AddProductSearchParameters(pageCommand, category, search);
        pageCommand.Parameters.AddWithValue("sort_order", sortOrder);
        pageCommand.Parameters.AddWithValue("page_size", pageSize);
        pageCommand.Parameters.AddWithValue("offset", offset);

        List<Product> products = [];
        await using var reader = await pageCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            Guid id = reader.GetGuid(0);
            string productName = reader.GetString(1).Trim();
            string productCategory = reader.GetString(7);
            Product product = ProductFactory.Create(productCategory, reader.GetString(8));
            product.ID = id;
            product.Name = productName;
            product.Price = reader.GetDecimal(2);
            product.Stock = reader.GetInt32(3);
            product.Color = reader.GetString(4);
            product.Description = reader.GetString(5);
            product.NetWeight = reader.GetInt32(6);
            product.ImagePath = reader.IsDBNull(9) ? null : reader.GetString(9);
            products.Add(product);
        }

        return (products, totalCount);
    }

    private static void AddProductSearchParameters(
        NpgsqlCommand command,
        string? category,
        string? search)
    {
        command.Parameters.Add(new NpgsqlParameter("category", NpgsqlDbType.Text)
        {
            Value = string.IsNullOrWhiteSpace(category) ? DBNull.Value : category
        });
        command.Parameters.Add(new NpgsqlParameter("search", NpgsqlDbType.Text)
        {
            Value = string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%"
        });
    }

}
