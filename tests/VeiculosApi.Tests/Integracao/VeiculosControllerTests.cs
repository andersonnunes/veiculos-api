using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VeiculosApi.Domain.Veiculos;
using VeiculosApi.Infrastructure.Persistence;
using VeiculosApi.Tests.Infraestrutura;
using VeiculosApi.Tests.Seguranca;

namespace VeiculosApi.Tests.Integracao;

[Collection(AmbienteCollection.NomeColecao)]
public sealed class VeiculosControllerTests(KeycloakFixture keycloak, PostgresFixture postgres)
{
    private const string AlfabetoChassi = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";
    private const string AlfabetoPlaca = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    [Fact]
    public async Task Cadastrar_SemToken_Retorna401()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/veiculos", Body(GerarChassi()));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_TokenSemPapelDeVendedor_Retorna403()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var token = await keycloak.ObterTokenServicoAsync("vendas-api", "dev-vendas-api-secret");
        var cliente = ClienteComToken(fabrica, token);

        var resposta = await cliente.PostAsJsonAsync("/veiculos", Body(GerarChassi()));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_DadosValidos_Retorna201ComLocalizacaoEStatusDisponivel()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());

        var criacao = await cliente.PostAsJsonAsync("/veiculos", Body(GerarChassi()));

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        Assert.NotNull(criacao.Headers.Location);
        var corpo = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("disponivel", corpo.GetProperty("status").GetString());
        Assert.NotEqual(Guid.Empty, corpo.GetProperty("id").GetGuid());

        var consulta = await cliente.GetAsync(criacao.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        var consultado = await consulta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(corpo.GetProperty("id").GetGuid(), consultado.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Cadastrar_ChassiJaCadastrado_Retorna409()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());
        var chassi = GerarChassi();
        await cliente.PostAsJsonAsync("/veiculos", Body(chassi));

        var resposta = await cliente.PostAsJsonAsync("/veiculos", Body(chassi));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_PlacaJaCadastrada_Retorna409()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());
        var placa = GerarPlaca();
        await cliente.PostAsJsonAsync("/veiculos", Body(GerarChassi(), placa));

        var resposta = await cliente.PostAsJsonAsync("/veiculos", Body(GerarChassi(), placa));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task ObterPorId_VeiculoInexistente_Retorna404()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());

        var resposta = await cliente.GetAsync($"/veiculos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_DadosValidos_Retorna200ComDadosNovos()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());
        var id = await CadastrarAsync(cliente);

        var resposta = await cliente.PutAsJsonAsync($"/veiculos/{id}", new AtualizacaoBody("Toyota", "Corolla", 2024, "Preto", 120000.00m));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Toyota", corpo.GetProperty("marca").GetString());
        Assert.Equal(120000.00m, corpo.GetProperty("preco").GetDecimal());
    }

    [Fact]
    public async Task Atualizar_SemToken_Retorna401()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.PutAsJsonAsync($"/veiculos/{Guid.NewGuid()}", new AtualizacaoBody("Toyota", "Corolla", 2024, "Preto", 120000.00m));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_TokenSemPapelDeVendedor_Retorna403()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var token = await keycloak.ObterTokenServicoAsync("vendas-api", "dev-vendas-api-secret");
        var cliente = ClienteComToken(fabrica, token);

        var resposta = await cliente.PutAsJsonAsync($"/veiculos/{Guid.NewGuid()}", new AtualizacaoBody("Toyota", "Corolla", 2024, "Preto", 120000.00m));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_IdInexistente_Retorna404()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());

        var resposta = await cliente.PutAsJsonAsync($"/veiculos/{Guid.NewGuid()}", new AtualizacaoBody("Toyota", "Corolla", 2024, "Preto", 120000.00m));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_DadosInvalidos_Retorna400ComErroDoCampo()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());
        var id = await CadastrarAsync(cliente);

        var resposta = await cliente.PutAsJsonAsync($"/veiculos/{id}", new AtualizacaoBody("Toyota", "Corolla", 1800, "Preto", 120000.00m));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problema.GetProperty("errors").TryGetProperty("ano", out _));
    }

    [Fact]
    public async Task Atualizar_VeiculoVendido_Retorna409()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());
        var id = await CadastrarAsync(cliente);
        await MarcarComoVendidoNoBancoAsync(fabrica, id);

        var resposta = await cliente.PutAsJsonAsync($"/veiculos/{id}", new AtualizacaoBody("Toyota", "Corolla", 2024, "Preto", 120000.00m));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_DadosValidos_SincronizaComVendas()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());

        var id = await CadastrarAsync(cliente);

        var enviado = Assert.Single(fabrica.Sincronizacao.Enviados);
        Assert.Equal(id, enviado.Id);
    }

    [Fact]
    public async Task Atualizar_DadosValidos_SincronizaVersaoAtualizada()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());
        var id = await CadastrarAsync(cliente);

        await cliente.PutAsJsonAsync($"/veiculos/{id}", new AtualizacaoBody("Toyota", "Corolla", 2024, "Preto", 120000.00m));

        Assert.Equal(2, fabrica.Sincronizacao.Enviados.Count);
        Assert.Equal("Toyota", fabrica.Sincronizacao.Enviados[^1].Marca);
    }

    private static async Task<Guid> CadastrarAsync(HttpClient cliente)
    {
        var criacao = await cliente.PostAsJsonAsync("/veiculos", Body(GerarChassi()));
        var corpo = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("id").GetGuid();
    }

    private static async Task MarcarComoVendidoNoBancoAsync(ApiComKeycloak fabrica, Guid id)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VeiculosDbContext>();
        var veiculo = await contexto.Veiculos.FirstAsync(v => v.Id == id);
        veiculo.MarcarComoVendido();
        await contexto.SaveChangesAsync();
    }

    private sealed record AtualizacaoBody(string Marca, string Modelo, int? Ano, string Cor, decimal? Preco);

    private async Task<string> TokenVendedorAsync() =>
        await keycloak.ObterTokenUsuarioAsync("vendedor-dev", "dev-vendedor");

    private static HttpClient ClienteComToken(ApiComKeycloak fabrica, string token)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    private sealed record CorpoCadastro(string Marca, string Modelo, int Ano, string Cor, decimal Preco, string Chassi, string? Placa);

    private static CorpoCadastro Body(string chassi, string? placa = null) =>
        new("Fiat", "Argo", 2025, "Prata", 85000.00m, chassi, placa);

    private static string GerarChassi() => Gerar(AlfabetoChassi, 17);

    private static string GerarPlaca() => Gerar(AlfabetoPlaca, 7);

    private static string Gerar(string alfabeto, int tamanho) =>
        new(Enumerable.Range(0, tamanho).Select(_ => alfabeto[Random.Shared.Next(alfabeto.Length)]).ToArray());
}
