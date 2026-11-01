using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VeiculosApi.Tests.Infraestrutura;
using VeiculosApi.Tests.Seguranca;

namespace VeiculosApi.Tests.Integracao;

[Collection(AmbienteCollection.NomeColecao)]
public sealed class StatusVendaInternoControllerTests(KeycloakFixture keycloak, PostgresFixture postgres)
{
    private static readonly DateTimeOffset Venda1 = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Venda2 = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Aplicar_SemToken_Retorna401()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.PutAsJsonAsync($"/internal/veiculos/{Guid.NewGuid()}/status", Corpo("vendido", Venda1));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Aplicar_TokenDeVendedor_Retorna403()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await keycloak.ObterTokenUsuarioAsync("vendedor-dev", "dev-vendedor"));

        var resposta = await cliente.PutAsJsonAsync($"/internal/veiculos/{Guid.NewGuid()}/status", Corpo("vendido", Venda1));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Aplicar_TokenDeVendasApi_MarcaVendidoEDepoisBloqueiaEdicao()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var vendedor = ClienteComToken(fabrica, await keycloak.ObterTokenUsuarioAsync("vendedor-dev", "dev-vendedor"));
        var servico = ClienteComToken(fabrica, await keycloak.ObterTokenServicoAsync("vendas-api", "dev-vendas-api-secret"));
        var id = await CadastrarVeiculoAsync(vendedor);

        var resposta = await servico.PutAsJsonAsync($"/internal/veiculos/{id}/status", Corpo("vendido", Venda1));
        var edicao = await vendedor.PutAsJsonAsync($"/veiculos/{id}", new { marca = "Toyota", modelo = "Outro", ano = 2024, cor = "Prata", preco = 100 });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("vendido", corpo.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, edicao.StatusCode);
    }

    [Fact]
    public async Task Aplicar_NotificacaoMaisAntiga_NaoReverteOStatus()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var vendedor = ClienteComToken(fabrica, await keycloak.ObterTokenUsuarioAsync("vendedor-dev", "dev-vendedor"));
        var servico = ClienteComToken(fabrica, await keycloak.ObterTokenServicoAsync("vendas-api", "dev-vendas-api-secret"));
        var id = await CadastrarVeiculoAsync(vendedor);

        await servico.PutAsJsonAsync($"/internal/veiculos/{id}/status", Corpo("vendido", Venda2));
        var antiga = await servico.PutAsJsonAsync($"/internal/veiculos/{id}/status", Corpo("disponivel", Venda1));

        Assert.Equal(HttpStatusCode.OK, antiga.StatusCode);
        var corpo = await antiga.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("vendido", corpo.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Aplicar_StatusReservado_Retorna400()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var servico = ClienteComToken(fabrica, await keycloak.ObterTokenServicoAsync("vendas-api", "dev-vendas-api-secret"));

        var resposta = await servico.PutAsJsonAsync($"/internal/veiculos/{Guid.NewGuid()}/status", Corpo("reservado", Venda1));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Aplicar_SemOcorridoEm_Retorna400()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var servico = ClienteComToken(fabrica, await keycloak.ObterTokenServicoAsync("vendas-api", "dev-vendas-api-secret"));
        var id = await CadastrarVeiculoAsync(ClienteComToken(fabrica, await keycloak.ObterTokenUsuarioAsync("vendedor-dev", "dev-vendedor")));

        var resposta = await servico.PutAsJsonAsync($"/internal/veiculos/{id}/status", new { status = "vendido" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Aplicar_VeiculoInexistente_Retorna404()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var servico = ClienteComToken(fabrica, await keycloak.ObterTokenServicoAsync("vendas-api", "dev-vendas-api-secret"));

        var resposta = await servico.PutAsJsonAsync($"/internal/veiculos/{Guid.NewGuid()}/status", Corpo("vendido", Venda1));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    private static object Corpo(string status, DateTimeOffset ocorridoEm) => new { status, ocorridoEm };

    private static HttpClient ClienteComToken(ApiComKeycloak fabrica, string token)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    private static async Task<Guid> CadastrarVeiculoAsync(HttpClient cliente)
    {
        var chassi = Guid.NewGuid().ToString("N")[..17].ToUpperInvariant();
        var criacao = await cliente.PostAsJsonAsync("/veiculos", new
        {
            marca = "Toyota",
            modelo = "Corolla XEi",
            ano = 2024,
            cor = "Prata",
            preco = 139900,
            chassi,
            placa = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var corpo = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("id").GetGuid();
    }
}
