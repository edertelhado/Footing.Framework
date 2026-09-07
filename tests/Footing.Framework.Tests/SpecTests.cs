using Footing.Framework.Specification;

namespace Footing.Framework.Tests;

public sealed record UserSpec(int Id, string Name, bool Ativo, int Age);

public class SpecTests
{
    private static ISpecification<UserSpec> AtivoSpec() => SpecificationExtensions.Where<UserSpec>(u => u.Ativo);
    private static ISpecification<UserSpec> AdultSpec() => SpecificationExtensions.Where<UserSpec>(u => u.Age >= 18);
    private static ISpecification<UserSpec> NameNotEmptySpec() => SpecificationExtensions.Where<UserSpec>(u => !string.IsNullOrEmpty(u.Name));

    [Fact]
    public void Spec_And_Requires_Both()
    {
        var spec = AtivoSpec().And(AdultSpec());

        Assert.True(spec.IsSatisfiedBy(new UserSpec(1, "eder", true, 20)));
        Assert.False(spec.IsSatisfiedBy(new UserSpec(1, "eder", true, 10))); // Age fail
        Assert.False(spec.IsSatisfiedBy(new UserSpec(1, "eder", false, 20))); // Ativo fail
        Assert.False(spec.IsSatisfiedBy(new UserSpec(1, "eder", false, 10)));

        // Composability: And retorna ISpecification que pode re-And
        var triple = spec.And(NameNotEmptySpec());
        Assert.True(triple.IsSatisfiedBy(new UserSpec(1, "eder", true, 20)));
        Assert.False(triple.IsSatisfiedBy(new UserSpec(1, "", true, 20)));
    }

    [Fact]
    public void Spec_Or_Requires_Either()
    {
        var spec = AtivoSpec().Or(AdultSpec());

        Assert.True(spec.IsSatisfiedBy(new UserSpec(1, "eder", true, 10))); // Ativo true
        Assert.True(spec.IsSatisfiedBy(new UserSpec(1, "eder", false, 20))); // Adult true
        Assert.True(spec.IsSatisfiedBy(new UserSpec(1, "eder", true, 20))); // both
        Assert.False(spec.IsSatisfiedBy(new UserSpec(1, "eder", false, 10))); // none

        // Composability: Or encadeado
        var triple = spec.Or(NameNotEmptySpec());
        Assert.True(triple.IsSatisfiedBy(new UserSpec(1, "eder", false, 10))); // name true
        Assert.True(triple.IsSatisfiedBy(new UserSpec(1, "", false, 10)) == false); // all false
    }

    [Fact]
    public void Spec_Where_Composable_Filter()
    {
        var baseSpec = AtivoSpec();
        var filtered = baseSpec.Where(u => u.Id > 0);

        Assert.True(filtered.IsSatisfiedBy(new UserSpec(1, "eder", true, 20)));
        Assert.False(filtered.IsSatisfiedBy(new UserSpec(0, "eder", true, 20))); // Id fail
        Assert.False(filtered.IsSatisfiedBy(new UserSpec(1, "eder", false, 20))); // Ativo fail

        // Where estático
        var ageSpec = SpecificationExtensions.Where<UserSpec>(u => u.Age >= 18);
        Assert.True(ageSpec.IsSatisfiedBy(new UserSpec(1, "a", true, 18)));
        Assert.False(ageSpec.IsSatisfiedBy(new UserSpec(1, "a", true, 17)));

        // LINQ integration
        var users = new[]
        {
            new UserSpec(1,"a",true,20),
            new UserSpec(2,"b",false,20),
            new UserSpec(3,"c",true,10),
        };
        var result = users.Where(u => filtered.IsSatisfiedBy(u)).ToList();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Id == 1);
        Assert.Contains(result, u => u.Id == 3);
    }
}
