using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HyperRazor.Demo.Mvc.Tests;

[CollectionDefinition("DemoMvcWebAppFactoryCollection")]
public class DemoMvcWebAppFactoryCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
}
