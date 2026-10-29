using System.Reflection;

namespace TheSoundOfClick;

public static class ProductHelper
{
  public static readonly Dictionary<string, Type> SubClassesUserName = [];
  public static readonly Dictionary<string, Type> SubClasses = [];

  static ProductHelper()
  {
    IEnumerable<Type> productTypes = typeof(Product).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Product)) && !t.IsAbstract);
    SubClassesUserName = productTypes.ToDictionary(t => GetProductCategoryUserView(t) ?? t.Name, t => t);
    SubClasses = productTypes.ToDictionary(t => t.Name, t => t);
  }

  private static string? GetProductCategoryUserView(Type type)
  {
    return type.GetCustomAttribute<ProductCategoryUserViewAttribute>()?.Name;
  }

  public static Type? GetTypeFromCategoryUserView(string name)
  {
    return SubClassesUserName.GetValueOrDefault(name);
  }
}
