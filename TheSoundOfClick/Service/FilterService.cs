using Npgsql;

namespace TheSoundOfClick;

public static class FilterService
{
  public static NpgsqlCommand AddFilters(NpgsqlCommand npgsqlCommand, string query, IEnumerable<Filter> filters)
  {
    var cmd = new NpgsqlCommand{ Connection = npgsqlCommand.Connection };

    var clauses = filters.Select(f => f.Apply()).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();


    if (clauses.Count > 0)
    {
      query += " WHERE " + string.Join(" AND ", clauses);
    }

    cmd.CommandText = query;
    return cmd;
  }
}
