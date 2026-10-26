using System.Collections.Concurrent;
using System.Reflection;

namespace TheSoundOfClick;

public record CachedProperty
(
  PropertyInfo Property,
  string? UserPageName,
  string? DataBaseName
);


public static class PropertyNameHelper
{
  private static ConcurrentDictionary<Type, CachedProperty[]> m_propertyCached = new();

  public static CachedProperty[] GetPropertiesWithMetaData(Type type)
  {
    return m_propertyCached.GetOrAdd(type, t =>
      t.GetProperties()
      .Select(p => new CachedProperty
      (
        Property: p, 
        UserPageName: p.GetCustomAttribute<UserPageNamePropAttribute>()?.Name,
        DataBaseName: p.GetCustomAttribute<DataBaseNamePropAttribute>()?.Name
      )).ToArray()
    );
  }

  public static string? GetPropertyAttributeName(PropertyInfo obj)
  {
    return obj.GetCustomAttribute<UserPageNamePropAttribute>()?.Name;
  }
}