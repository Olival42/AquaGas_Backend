using AquaGas.IntegrationTests.Fixtures;

namespace AquaGas.IntegrationTests.Collections;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
    public const string Name = "Integration tests";
}
