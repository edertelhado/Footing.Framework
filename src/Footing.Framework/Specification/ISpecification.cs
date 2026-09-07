namespace Footing.Framework.Specification;

/// <summary>
/// Agnostic Specification pattern — composable And/Or/Where.
/// No DB, no ORM, no external dependencies. Pure IsSatisfiedBy.
/// Usage: new Spec&lt;User&gt;(u => u.Active).And(other).Where(u => u.Id > 0)
/// </summary>
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T candidate);
}

public sealed class PredicateSpecification<T>(Func<T, bool> predicate) : ISpecification<T>
{
    public bool IsSatisfiedBy(T candidate) => predicate(candidate);
}

public sealed class AndSpecification<T>(ISpecification<T> left, ISpecification<T> right) : ISpecification<T>
{
    public bool IsSatisfiedBy(T c) => left.IsSatisfiedBy(c) && right.IsSatisfiedBy(c);
}

public sealed class OrSpecification<T>(ISpecification<T> left, ISpecification<T> right) : ISpecification<T>
{
    public bool IsSatisfiedBy(T c) => left.IsSatisfiedBy(c) || right.IsSatisfiedBy(c);
}

public sealed class NotSpecification<T>(ISpecification<T> inner) : ISpecification<T>
{
    public bool IsSatisfiedBy(T c) => !inner.IsSatisfiedBy(c);
}

public static class SpecificationExtensions
{
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
        => new AndSpecification<T>(left, right);

    public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right)
        => new OrSpecification<T>(left, right);

    public static ISpecification<T> Not<T>(this ISpecification<T> spec)
        => new NotSpecification<T>(spec);

    public static ISpecification<T> Where<T>(this ISpecification<T> left, Func<T, bool> predicate)
        => new AndSpecification<T>(left, new PredicateSpecification<T>(predicate));

    public static ISpecification<T> Where<T>(Func<T, bool> predicate)
        => new PredicateSpecification<T>(predicate);
}
