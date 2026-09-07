namespace Footing.Framework.DI;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class InjectableAttribute(InjectableLifetime lifetime) : Attribute
{
    public InjectableLifetime Lifetime { get; } = lifetime;
}