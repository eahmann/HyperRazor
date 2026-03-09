namespace HyperRazor.E2E;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DemoMvcE2ECollection : ICollectionFixture<DemoMvcE2EFixture>
{
    public const string Name = "demo-mvc-e2e";
}
