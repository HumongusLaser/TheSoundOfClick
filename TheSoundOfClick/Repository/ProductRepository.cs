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

  public async Task<Guid> CreateAsync(Product product)
  {
    await using var connection = await m_dataSource.OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand
    (
      @"INSERT INTO products (stock, name, price, color, description, net_weight, category, details)
        VALUES(@stock, @name, @price, @color, @description, @net_weight, @category, @details)
        RETURNING id", connection
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

    return (Guid)(await cmd.ExecuteScalarAsync()
      ?? throw new NpgsqlException("Could not create product."));
  }
    public async Task<Product> GetProductAsync(Guid id)
    {
        Product? product = null;
        await using var connection = await m_dataSource.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, price, stock, color, description, net_weight, category, details FROM products WHERE id = @id",
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
            SELECT id, name, price, stock, color, description, net_weight, category, details
            FROM products
            WHERE id > @id AND (@category IS NULL OR category = @category)
            ORDER BY id
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
            WHERE length(btrim(name)) >= 2
              AND (@category IS NULL OR category = @category)
              AND (
                  @search IS NULL
                  OR name ILIKE @search
                  OR id::text ILIKE @search
              )
            """;

        await using var connection = await m_dataSource.OpenConnectionAsync(cancellationToken);

        await using var countCommand = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM products {filter}",
            connection);
        AddProductSearchParameters(countCommand, category, search);
        int totalCount = Convert.ToInt32(
            await countCommand.ExecuteScalarAsync(cancellationToken));

        const string orderBy = """
            ORDER BY
                CASE WHEN @sort_order = 'name' THEN name END ASC,
                CASE WHEN @sort_order = 'priceAsc' THEN price END ASC,
                CASE WHEN @sort_order = 'priceDesc' THEN price END DESC,
                id ASC
            """;
        await using var pageCommand = new NpgsqlCommand(
            $"""
            SELECT id, name, price, stock, color, description, net_weight, category, details
            FROM products
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
