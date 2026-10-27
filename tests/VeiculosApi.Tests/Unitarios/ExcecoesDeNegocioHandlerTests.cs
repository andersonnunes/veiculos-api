using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using VeiculosApi.Application.Veiculos;
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

    [Fact]
    public async Task OutraExcecao_NaoEhTratada()
    {
        var contexto = CriarContexto();

        var tratada = await _handler.TryHandleAsync(
            contexto, new InvalidOperationException("falha inesperada"), CancellationToken.None);

        Assert.False(tratada);
    }
}
