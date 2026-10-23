using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using VeiculosApi.Application.Keycloak;
using VeiculosApi.Infrastructure.Keycloak;

namespace VeiculosApi.Tests.Seguranca;

public sealed class TokenDeServicoTests
{
    private static readonly IOptions<KeycloakOptions> Opcoes = Options.Create(new KeycloakOptions
    {
        EmissorPublico = "http://keycloak.teste/realms/revendax",
        MetadataInterna = "http://keycloak.teste/realms/revendax/.well-known/openid-configuration",
        EndpointToken = "http://keycloak.teste/realms/revendax/protocol/openid-connect/token",
        Audiencia = "vendas-api",
        ClientId = "veiculos-api",
        ClientSecret = "segredo-de-teste"
    });

    [Fact]
    public async Task ObterAsync_ReutilizaTokenEnquantoValidoERenovaAposExpiracao()
    {
        var emissor = new EmissorTokenFalso();
        var relogio = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var provedor = CriarProvedor(emissor);
        var tokens = new TokenDeServico(provedor.GetRequiredService<IHttpClientFactory>(), Opcoes, relogio);

        var primeiro = await tokens.ObterAsync(CancellationToken.None);
        var segundo = await tokens.ObterAsync(CancellationToken.None);
        relogio.Advance(TimeSpan.FromHours(1));
        var terceiro = await tokens.ObterAsync(CancellationToken.None);

        Assert.Equal(primeiro, segundo);
        Assert.NotEqual(primeiro, terceiro);
        Assert.Equal(2, emissor.Requisicoes);
    }

    private static ServiceProvider CriarProvedor(EmissorTokenFalso emissor)
    {
        var servicos = new ServiceCollection();
        servicos.AddHttpClient("keycloak-token").ConfigurePrimaryHttpMessageHandler(() => emissor);
        return servicos.BuildServiceProvider();
    }

    private sealed class EmissorTokenFalso : HttpMessageHandler
    {
        public int Requisicoes { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requisicoes++;
            var corpo = $"{{\"access_token\":\"token-{Requisicoes}\",\"expires_in\":300}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(corpo, Encoding.UTF8, "application/json")
            });
        }
    }
}
