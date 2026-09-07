using Footing.Framework.DI;
namespace Footing.Framework.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RepositoryAttribute(InjectableLifetime lifetime = InjectableLifetime.Scoped) : Attribute
{
    public InjectableLifetime Lifetime { get; } = lifetime;
}