using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Application.Veiculos.Fotos;
using VeiculosApi.Infrastructure.Keycloak;
using VeiculosApi.Seguranca;
using VeiculosApi.Tests.Aplicacao;
using VeiculosApi.Tests.Fotos;
using VeiculosApi.Tests.Infraestrutura;

namespace VeiculosApi.Tests.Seguranca;

public sealed class ApiComKeycloak(KeycloakFixture keycloak, PostgresFixture postgres, string clientId, string clientSecret)
    : WebApplicationFactory<Program>
{
    public SincronizacaoFalsa Sincronizacao { get; } = new();

    public ApiComKeycloak(KeycloakFixture keycloak, PostgresFixture postgres)
        : this(keycloak, postgres, "veiculos-api", "dev-veiculos-api-secret")
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:VeiculosDb", postgres.ConnectionString);
        builder.UseSetting("Keycloak:EmissorPublico", keycloak.EmissorRealm);
        builder.UseSetting("Keycloak:MetadataInterna", keycloak.MetadataRealm);
        builder.UseSetting("Keycloak:EndpointToken", keycloak.EndpointToken);
        builder.UseSetting("Keycloak:ClientId", clientId);
        builder.UseSetting("Keycloak:ClientSecret", clientSecret);

        builder.ConfigureTestServices(servicos =>
        {
            servicos.AddSingleton<IVendasSincronizacao>(Sincronizacao);
            servicos.AddControllers().AddApplicationPart(typeof(TestesSegurancaController).Assembly);

            // Os testes de integração não dependem de MinIO: o armazenamento real é coberto em MinioArmazenamentoFotosTests.
            var armazenamentoFotos = new ArmazenamentoFotosEmMemoria();
            servicos.AddSingleton(armazenamentoFotos);
            servicos.AddScoped<IArmazenamentoFotos>(_ => armazenamentoFotos);

            servicos.AddHttpClient("teste-saida")
                .AddHttpMessageHandler<ServicoAutenticadoHandler>()
                .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler());
        });
    }
}
