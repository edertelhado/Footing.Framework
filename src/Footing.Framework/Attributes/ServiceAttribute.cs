namespace Footing.Framework.DI;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ServiceAttribute(InjectableLifetime lifetime = InjectableLifetime.Transient) : Attribute
{
    public InjectableLifetime Lifetime { get; } = lifetime;
}