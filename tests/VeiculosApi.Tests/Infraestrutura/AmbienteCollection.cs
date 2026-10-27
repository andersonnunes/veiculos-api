using VeiculosApi.Tests.Seguranca;

namespace VeiculosApi.Tests.Infraestrutura;

[CollectionDefinition(NomeColecao)]
public sealed class AmbienteCollection : ICollectionFixture<KeycloakFixture>, ICollectionFixture<PostgresFixture>
{
    public const string NomeColecao = "ambiente";
}
