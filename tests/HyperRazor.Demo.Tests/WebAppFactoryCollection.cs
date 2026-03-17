using Microsoft.AspNetCore.Mvc.Testing;

namespace HyperRazor.Demo.Tests;

[CollectionDefinition("WebAppFactoryCollection")]
public sealed class WebAppFactoryCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
}
