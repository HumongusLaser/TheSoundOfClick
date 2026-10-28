using System.Text;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;


namespace TheSoundOfClick;

public class ProductRepository
{
  private readonly NpgsqlDataSource m_dataSource;

  public ProductRepository(NpgsqlDataSource dataSource)
  {
    m_dataSource = dataSource;
  }

  public async Task CreateAsync(Product product)
  {
    await using var connection = await m_dataSource.OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand
    (
      @"INSERT INTO products (stock, name, price, color, description, net_weight, category, details)
        VALUES(@stock, @name, @price, @color, @description, @net_weight, @category, @details)", connection
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

    await cmd.ExecuteNonQueryAsync();

  }
    public async Task<Product> GetProductsAsync(Guid id)
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

}
