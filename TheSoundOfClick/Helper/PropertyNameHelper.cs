using System.Reflection;

namespace TheSoundOfClick;

public static class PropertyNameHelper
{
  public static string? GetPropertyAttributeName(PropertyInfo obj)
  {
    return obj.GetCustomAttribute<UserPageNamePropAttribute>()?.Name;
  }
}
