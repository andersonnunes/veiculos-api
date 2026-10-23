using Testcontainers.PostgreSql;

namespace VeiculosApi.Tests.Infraestrutura;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("veiculos")
        .WithUsername("veiculos")
        .WithPassword("veiculos")
        .WithReuse(true)
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
