using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Application.Veiculos.Fotos;
using VeiculosApi.Domain.Veiculos;
using VeiculosApi.Erros;

namespace VeiculosApi.Tests.Unitarios;

public class ExcecoesDeNegocioHandlerTests
{
    private readonly ExcecoesDeNegocioHandler _handler = new();

    private static DefaultHttpContext CriarContexto()
    {
        var servicos = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var contexto = new DefaultHttpContext { RequestServices = servicos };
        contexto.Response.Body = new MemoryStream();
        return contexto;
    }

    [Fact]
    public async Task ValidacaoVeiculo_RetornaBadRequestComErrosPorCampo()
    {
        var contexto = CriarContexto();
        var excecao = new ValidacaoVeiculoException(
            new Dictionary<string, string[]> { ["ano"] = ["Ano inválido."] });

        var tratada = await _handler.TryHandleAsync(contexto, excecao, CancellationToken.None);

        Assert.True(tratada);
        Assert.Equal(StatusCodes.Status400BadRequest, contexto.Response.StatusCode);
        contexto.Response.Body.Position = 0;
        using var documento = await JsonDocument.ParseAsync(contexto.Response.Body);
        Assert.True(documento.RootElement.GetProperty("errors").TryGetProperty("ano", out _));
    }

    [Fact]
    public async Task VeiculoDuplicado_RetornaConflict()
    {
        var contexto = CriarContexto();

        var tratada = await _handler.TryHandleAsync(
            contexto, new VeiculoDuplicadoException("chassi"), CancellationToken.None);

        Assert.True(tratada);
        Assert.Equal(StatusCodes.Status409Conflict, contexto.Response.StatusCode);
    }

    [Fact]
    public async Task VeiculoVendido_RetornaConflict()
    {
        var contexto = CriarContexto();

        var tratada = await _handler.TryHandleAsync(
            contexto, new VeiculoVendidoException(Guid.NewGuid()), CancellationToken.None);

        Assert.True(tratada);
        Assert.Equal(StatusCodes.Status409Conflict, contexto.Response.StatusCode);
    }

    [Fact]
    public async Task ConflitoDeConcorrencia_RetornaConflict()
    {
        var contexto = CriarContexto();

        var tratada = await _handler.TryHandleAsync(
            contexto, new VeiculoConflitoConcorrenciaException(), CancellationToken.None);

        Assert.True(tratada);
        Assert.Equal(StatusCodes.Status409Conflict, contexto.Response.StatusCode);
    }

    [Theory]
    [InlineData("chassi", "este chassi")]
    [InlineData("placa", "esta placa")]
    public async Task VeiculoDuplicado_TrazCodigoCampoEMensagemQueConcordaComOCampo(string campo, string descricao)
    {
        var contexto = CriarContexto();

        await _handler.TryHandleAsync(contexto, new VeiculoDuplicadoException(campo), CancellationToken.None);

        var corpo = await LerCorpoAsync(contexto);
        Assert.Equal("veiculo_duplicado", corpo.GetProperty("codigo").GetString());
        Assert.Equal(campo, corpo.GetProperty("campo").GetString());
        Assert.Equal($"Já existe um veículo com {descricao}.", corpo.GetProperty("detail").GetString());
        Assert.Equal("Veículo já cadastrado", corpo.GetProperty("title").GetString());
    }

    [Fact]
    public async Task VeiculoVendido_TrazCodigoVeiculoVendido()
    {
        var contexto = CriarContexto();

        await _handler.TryHandleAsync(contexto, new VeiculoVendidoException(Guid.NewGuid()), CancellationToken.None);

        var corpo = await LerCorpoAsync(contexto);
        Assert.Equal("veiculo_vendido", corpo.GetProperty("codigo").GetString());
        Assert.Equal("Veículo vendido", corpo.GetProperty("title").GetString());
        Assert.False(corpo.TryGetProperty("campo", out _));
    }

    [Fact]
    public async Task ConflitoDeConcorrencia_TrazCodigoConflitoAlteracao()
    {
        var contexto = CriarContexto();

        await _handler.TryHandleAsync(contexto, new VeiculoConflitoConcorrenciaException(), CancellationToken.None);

        var corpo = await LerCorpoAsync(contexto);
        Assert.Equal("conflito_alteracao", corpo.GetProperty("codigo").GetString());
        Assert.Equal("Conflito de alteração", corpo.GetProperty("title").GetString());
    }

    [Fact]
    public async Task LimiteDeFotos_TrazCodigoLimiteFotosESemCampo()
    {
        var contexto = CriarContexto();

        await _handler.TryHandleAsync(contexto, new LimiteFotosExcedidoException(10), CancellationToken.None);

        var corpo = await LerCorpoAsync(contexto);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, contexto.Response.StatusCode);
        Assert.Equal("limite_fotos", corpo.GetProperty("codigo").GetString());
        Assert.False(corpo.TryGetProperty("campo", out _));
    }

    private static async Task<JsonElement> LerCorpoAsync(DefaultHttpContext contexto)
    {
        contexto.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonSerializer.DeserializeAsync<JsonElement>(contexto.Response.Body);
    }

    [Fact]
    public async Task OutraExcecao_NaoEhTratada()
    {
        var contexto = CriarContexto();

        var tratada = await _handler.TryHandleAsync(
            contexto, new InvalidOperationException("falha inesperada"), CancellationToken.None);

        Assert.False(tratada);
    }
}
