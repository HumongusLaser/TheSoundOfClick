using System.Reflection;

namespace TheSoundOfClick;

public record CachedProductName
(
  string Name,
  string? Category,
  string? CategoryUserView
);


public static class ProductNameHelper
{
  public static readonly Dictionary<Type, CachedProductName> SubClassesUserName = [];

  static ProductNameHelper()
  {
    SubClassesUserName = typeof(Product).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Product)) && !t.IsAbstract).
    ToDictionary(t => t, t =>
    {
      return new CachedProductName
      (
        Name: t.Name, 
        Category: ProductFactory.GetCategory(t),
        CategoryUserView: GetProductCategoryUserView(t)
      );
    });
  }

  private static string? GetProductCategoryUserView(Type type)
  {
    return type.GetCustomAttribute<ProductCategoryUserViewAttribute>()?.Name;
  }

  public static Type? GetTypeFromCategoryUserView(string name)
  {
    return SubClassesUserName.FirstOrDefault(kvp => kvp.Value.CategoryUserView == name).Key;
  }

  public static CachedProperty[]? GetPropertiesFromCategoryUserView(string name)
  {
    Type? type = SubClassesUserName.FirstOrDefault(kvp => kvp.Value.CategoryUserView == name).Key;
    if (type == null)
    {
      return null;
    }
    return PropertyNameHelper.GetPropertiesWithMetaData(type);
  }

  public static CachedProperty[]? GetPropertiesFromTypeName(string name)
  {
    Type? type = SubClassesUserName.FirstOrDefault(kvp => kvp.Value.Name == name).Key;
    if (type == null)
    {
      return null;
    }
    return PropertyNameHelper.GetPropertiesWithMetaData(type);
  }
}
