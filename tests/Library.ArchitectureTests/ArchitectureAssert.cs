namespace Library.ArchitectureTests;

public static class ArchitectureAssert
{
    public static void Passes(NetArchTest.Rules.TestResult result)
    {
        var failingTypes = result.FailingTypeNames ?? [];

        Assert.True(result.IsSuccessful, $"Types breaking the rule: {string.Join(", ", failingTypes)}");
    }
}
