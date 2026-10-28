using Npgsql;

namespace TheSoundOfClick;

public class DiscountRepository
{
  private readonly NpgsqlDataSource m_dataSource;

  public DiscountRepository(NpgsqlDataSource dataSource)
  {
    m_dataSource = dataSource;
  }
}
