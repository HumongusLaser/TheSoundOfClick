using Npgsql;

namespace TheSoundOfClick;

public abstract class Filter(NpgsqlCommand npgsqlCommand)
{
  protected NpgsqlCommand m_cmd = npgsqlCommand;

  public abstract string Apply();
}


public class PriceFilter(NpgsqlCommand npgsqlCommand, int? min, int? max) : Filter(npgsqlCommand)
{
  public override string Apply()
  {
    List<string> strings = new();
    if (min != null)
    {
      m_cmd.Parameters.AddWithValue("min", min);
      strings.Add("price >= @min");
    }
    
    if (max != null)
    {
      m_cmd.Parameters.AddWithValue("max", max);
      strings.Add("price <= @max");
    }

    return string.Join(" AND ", strings);
  }
}

public class StockFilter(NpgsqlCommand npgsqlCommand, int? min, int? max) : Filter(npgsqlCommand)
{
  public override string Apply()
  {
    List<string> strings = new();

    if (min != null)
    {
      m_cmd.Parameters.AddWithValue("stock_min", min);
      strings.Add("stock >= @stock_min");
    }

    if (max != null)
    {
      m_cmd.Parameters.AddWithValue("stock_max", max);
      strings.Add("stock <= @stock_max");
    }

    return string.Join(" AND ", strings);
  }
}

public class CreationDateFiler(NpgsqlCommand npgsqlCommand, DateOnly? min, DateOnly? max) : Filter(npgsqlCommand)
{
  public override string Apply()
  {
    List<string> strings = new();

    if (min != null)
    {
      m_cmd.Parameters.AddWithValue("creation_date_min", min.Value);
      strings.Add("creation_date >= @creation_date_min");
    }

    if (max != null)
    {
      m_cmd.Parameters.AddWithValue("creation_date_max", max.Value);
      strings.Add("creation_date <= @creation_date_max");
    }

    return string.Join(" AND ", strings);
  }
}

public class ColorFilter(NpgsqlCommand npgsqlCommand, Product.Colors color) : Filter(npgsqlCommand)
{
  public override string Apply()
  {
    m_cmd.Parameters.AddWithValue("color", color);
    return "color = @color";
  }
}