using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace TheSoundOfClick;

enum ProductFilters
{
  Category,
  Price,
  
}

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

  public async Task GetAsync(Int128 id, int amount, string filter)
  {
    
  }
}
