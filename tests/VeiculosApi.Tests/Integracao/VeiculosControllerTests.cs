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
    public async Task Cadastrar_ChassiJaCadastrado_TrazCodigoECampoNaResposta()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());
        var chassi = GerarChassi();
        await cliente.PostAsJsonAsync("/veiculos", Body(chassi));

        var resposta = await cliente.PostAsJsonAsync("/veiculos", Body(chassi));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("veiculo_duplicado", corpo.GetProperty("codigo").GetString());
        Assert.Equal("chassi", corpo.GetProperty("campo").GetString());
        Assert.Equal("Veículo já cadastrado", corpo.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Cadastrar_PlacaJaCadastrada_TrazCampoPlaca()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());
        var placa = GerarPlaca();
        await cliente.PostAsJsonAsync("/veiculos", Body(GerarChassi(), placa));

        var resposta = await cliente.PostAsJsonAsync("/veiculos", Body(GerarChassi(), placa));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("veiculo_duplicado", corpo.GetProperty("codigo").GetString());
        Assert.Equal("placa", corpo.GetProperty("campo").GetString());
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
        await VenderNoBancoAsync(fabrica, id);

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

    [Fact]
    public async Task Cadastrar_MesmoChassiSimultaneo_SoUmEhCriado()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());
        var chassi = GerarChassi();

        var respostas = await Task.WhenAll(
            cliente.PostAsJsonAsync("/veiculos", Body(chassi)),
            cliente.PostAsJsonAsync("/veiculos", Body(chassi)));

        // A unicidade é garantida no banco: a segunda chegada não pode criar outra linha.
        var codigos = respostas.Select(r => r.StatusCode).OrderBy(c => c).ToList();
        Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }, codigos);
        Assert.Equal(1, await ContarPorChassiAsync(fabrica, chassi));
    }

    [Fact]
    public async Task Atualizar_EdicoesSimultaneas_NaoMisturaCamposDeDuasEdicoes()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = ClienteComToken(fabrica, await TokenVendedorAsync());
        var id = await CadastrarAsync(cliente);
        var a = new AtualizacaoBody("Toyota", "Corolla", 2024, "Preto", 110000.00m);
        var b = new AtualizacaoBody("Honda", "Civic", 2023, "Branco", 99000.00m);

        var respostas = await Task.WhenAll(
            cliente.PutAsJsonAsync($"/veiculos/{id}", a),
            cliente.PutAsJsonAsync($"/veiculos/{id}", b));

        Assert.All(respostas, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        Assert.Contains(respostas, r => r.StatusCode == HttpStatusCode.OK);

        // O estado final é exatamente uma das duas edições, inteira, e a vencedora é a que foi aceita.
        var final = await cliente.GetFromJsonAsync<JsonElement>($"/veiculos/{id}");
        var marca = final.GetProperty("marca").GetString();
        var cor = final.GetProperty("cor").GetString();
        var preco = final.GetProperty("preco").GetDecimal();
        var venceuA = marca == "Toyota" && cor == "Preto" && preco == 110000.00m;
        var venceuB = marca == "Honda" && cor == "Branco" && preco == 99000.00m;
        Assert.True(venceuA ^ venceuB, $"Campos misturados: marca={marca} cor={cor} preco={preco}");
        Assert.Equal(HttpStatusCode.OK, respostas[venceuA ? 0 : 1].StatusCode);
    }

    [Fact]
    public async Task Atualizar_CorridaComVenda_EdicaoSoPrevaleceSeFoiAceitaAntesDaVenda()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var vendedor = ClienteComToken(fabrica, await TokenVendedorAsync());
        var servico = ClienteComToken(fabrica, await keycloak.ObterTokenServicoAsync("vendas-api", "dev-vendas-api-secret"));
        var id = await CadastrarAsync(vendedor);
        var edicao = new AtualizacaoBody("Honda", "Civic", 2023, "Branco", 99000.00m);

        // As duas requisições partem antes de qualquer espera: é isso que cria a corrida.
        var tarefaEdicao = vendedor.PutAsJsonAsync($"/veiculos/{id}", edicao);
        var tarefaVenda = servico.PutAsJsonAsync($"/internal/veiculos/{id}/status", new { status = "vendido", ocorridoEm = DateTimeOffset.UtcNow });
        await Task.WhenAll(tarefaEdicao, tarefaVenda);
        var respostaEdicao = await tarefaEdicao;
        var respostaVenda = await tarefaVenda;

        // A venda sempre é aplicada. A edição ou é aceita antes dela, ou recusada depois: nunca
        // sobrescreve os dados de um veículo que já foi vendido.
        Assert.Equal(HttpStatusCode.OK, respostaVenda.StatusCode);
        Assert.Contains(respostaEdicao.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict });

        var final = await vendedor.GetFromJsonAsync<JsonElement>($"/veiculos/{id}");
        Assert.Equal("vendido", final.GetProperty("status").GetString());
        var marcaFinal = final.GetProperty("marca").GetString();
        if (respostaEdicao.StatusCode == HttpStatusCode.OK)
        {
            Assert.Equal("Honda", marcaFinal);
        }
        else
        {
            Assert.Equal("Fiat", marcaFinal);
        }
    }

    private static async Task<int> ContarPorChassiAsync(ApiComKeycloak fabrica, string chassi)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VeiculosDbContext>();
        return await contexto.Veiculos.AsNoTracking().CountAsync(v => v.Chassi == chassi);
    }

    private static async Task<Guid> CadastrarAsync(HttpClient cliente)
    {
        var criacao = await cliente.PostAsJsonAsync("/veiculos", Body(GerarChassi()));
        var corpo = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("id").GetGuid();
    }

    private static async Task VenderNoBancoAsync(ApiComKeycloak fabrica, Guid id)
    {
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<VeiculosDbContext>();
        var veiculo = await contexto.Veiculos.FirstAsync(v => v.Id == id);
        veiculo.AplicarStatusVenda(StatusVeiculo.Vendido, DateTimeOffset.UtcNow);
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
