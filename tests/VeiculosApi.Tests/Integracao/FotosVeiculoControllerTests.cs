using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VeiculosApi.Tests.Fotos;
using VeiculosApi.Tests.Infraestrutura;
using VeiculosApi.Tests.Seguranca;

namespace VeiculosApi.Tests.Integracao;

[Collection(AmbienteCollection.NomeColecao)]
public sealed class FotosVeiculoControllerTests(KeycloakFixture keycloak, PostgresFixture postgres)
{
    [Fact]
    public async Task Adicionar_SemToken_Retorna401()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsync($"/veiculos/{Guid.NewGuid()}/fotos", ConteudoMultipart(ArquivosDeImagem.Jpeg()));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Adicionar_JpegValido_Retorna201_ListaEEntregaOMesmoConteudo()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteVendedorAsync(fabrica);
        var veiculoId = await CadastrarVeiculoAsync(cliente);
        var bytes = ArquivosDeImagem.Jpeg(128);

        var criacao = await cliente.PostAsync($"/veiculos/{veiculoId}/fotos", ConteudoMultipart(bytes));

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        Assert.NotNull(criacao.Headers.Location);
        var corpo = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("image/jpeg", corpo.GetProperty("contentType").GetString());
        Assert.Equal(bytes.Length, corpo.GetProperty("tamanho").GetInt64());

        var lista = await (await cliente.GetAsync($"/veiculos/{veiculoId}/fotos")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, lista.GetArrayLength());

        var conteudo = await cliente.GetAsync(criacao.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, conteudo.StatusCode);
        Assert.Equal("image/jpeg", conteudo.Content.Headers.ContentType?.MediaType);
        Assert.Equal(bytes, await conteudo.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Adicionar_ArquivoQueNaoEImagem_Retorna400ComErroNoCampoArquivo()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteVendedorAsync(fabrica);
        var veiculoId = await CadastrarVeiculoAsync(cliente);

        var resposta = await cliente.PostAsync($"/veiculos/{veiculoId}/fotos", ConteudoMultipart("%PDF-1.7"u8.ToArray()));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(corpo.GetProperty("errors").TryGetProperty("arquivo", out _));
    }

    [Fact]
    public async Task Adicionar_SemCampoArquivo_Retorna400ComErroNoCampoArquivo()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteVendedorAsync(fabrica);
        var veiculoId = await CadastrarVeiculoAsync(cliente);

        var formulario = new MultipartFormDataContent { { new StringContent("sem foto"), "legenda" } };
        var resposta = await cliente.PostAsync($"/veiculos/{veiculoId}/fotos", formulario);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(corpo.GetProperty("errors").TryGetProperty("arquivo", out _));
    }

    [Fact]
    public async Task Adicionar_UploadsConcorrentes_NuncaUltrapassamOLimite()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteVendedorAsync(fabrica);
        var veiculoId = await CadastrarVeiculoAsync(cliente);
        var envios = Enumerable.Range(0, 12)
            .Select(_ => cliente.PostAsync($"/veiculos/{veiculoId}/fotos", ConteudoMultipart(ArquivosDeImagem.Webp())))
            .ToArray();

        var respostas = await Task.WhenAll(envios);

        Assert.Equal(10, respostas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(2, respostas.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity));
        var lista = await (await cliente.GetAsync($"/veiculos/{veiculoId}/fotos")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(10, lista.GetArrayLength());
    }

    [Fact]
    public async Task Adicionar_VeiculoInexistente_Retorna404()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteVendedorAsync(fabrica);

        var resposta = await cliente.PostAsync($"/veiculos/{Guid.NewGuid()}/fotos", ConteudoMultipart(ArquivosDeImagem.Png()));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Adicionar_AlemDoLimiteDeFotos_Retorna422()
    {
        using var fabrica = new ApiComKeycloak(keycloak, postgres);
        var cliente = await ClienteVendedorAsync(fabrica);
        var veiculoId = await CadastrarVeiculoAsync(cliente);
        for (var i = 0; i < 10; i++)
        {
            var ok = await cliente.PostAsync($"/veiculos/{veiculoId}/fotos", ConteudoMultipart(ArquivosDeImagem.Webp()));
            Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        }

        var excedente = await cliente.PostAsync($"/veiculos/{veiculoId}/fotos", ConteudoMultipart(ArquivosDeImagem.Webp()));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, excedente.StatusCode);
    }

    private static MultipartFormDataContent ConteudoMultipart(byte[] bytes)
    {
        var conteudo = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(bytes);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        conteudo.Add(arquivo, "arquivo", "foto.bin");
        return conteudo;
    }

    private async Task<HttpClient> ClienteVendedorAsync(ApiComKeycloak fabrica)
    {
        var token = await keycloak.ObterTokenUsuarioAsync("vendedor-dev", "dev-vendedor");
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
