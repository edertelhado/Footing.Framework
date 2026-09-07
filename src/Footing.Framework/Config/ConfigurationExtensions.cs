using System.Reflection;
using Microsoft.Extensions.Configuration;
namespace Footing.Framework.Config
{
    [AttributeUsage(AttributeTargets.Property)]
    public class InjectConfigAttribute(string section) : Attribute
    {
        public string Section { get; } = section;
    }

    public static class ConfigurationExtensions
    {
        public static void InjectConfiguration<T>(this T instance, IConfiguration config)
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<InjectConfigAttribute>() != null);

            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<InjectConfigAttribute>()!;
                var value = config.GetValue(prop.PropertyType, attr.Section);
                prop.SetValue(instance, value);
            }
        }
    }
}