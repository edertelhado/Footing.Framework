using Footing.Framework.DI;
namespace Footing.Framework.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class InjectableAttribute(InjectableLifetime lifetime) : Attribute
{
    public InjectableLifetime Lifetime { get; } = lifetime;
}