namespace TheSoundOfClick;
using System.Reflection;
using System.Text.Json;

public static class ProductFactory
{
  private static readonly Dictionary<string, Type> Types = typeof(Product).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Product)) && !t.IsAbstract)
  .ToDictionary(t => t.GetCustomAttribute<ProductCategoryAttribute>()!.Name, t => t);

  public static Product Create(string category, string detailsJson)
  {
    if (!Types.TryGetValue(category, out var type))
    {
      throw new InvalidOperationException($"Unknown category '{category}'");
    }

    return (Product)JsonSerializer.Deserialize(detailsJson, type)!;
  }

  public static string GetCategory(Product product)
  {
    return product.GetType().GetCustomAttribute<ProductCategoryAttribute>()!.Name;
  }

  public static string SerializeDetails(Product product)
  {
    return JsonSerializer.Serialize(product, product.GetType());
  }
}
