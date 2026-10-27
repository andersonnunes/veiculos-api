using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Domain.Veiculos;
using VeiculosApi.Infrastructure.Sincronizacao;

namespace VeiculosApi.Tests.Sincronizacao;

public class VendasSincronizacaoHttpTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SincronizarAsync_EnviaPutComSnapshotCompleto()
    {
        var handler = new RespostasFalsas(HttpStatusCode.OK);
        var cliente = new HttpClient(handler) { BaseAddress = new Uri("http://vendas.teste") };
        var sincronizacao = new VendasSincronizacaoHttp(cliente, NullLogger<VendasSincronizacaoHttp>.Instance);
        var veiculo = CriarVeiculo();

        await sincronizacao.SincronizarAsync(veiculo, CancellationToken.None);

        var requisicao = Assert.Single(handler.Requisicoes);
        Assert.Equal(HttpMethod.Put, requisicao.Method);
        Assert.Equal($"/internal/veiculos/{veiculo.Id}", requisicao.RequestUri!.AbsolutePath);
        using var corpo = JsonDocument.Parse(handler.CorpoUltimo!);
        Assert.Equal(veiculo.Id, corpo.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("disponivel", corpo.RootElement.GetProperty("status").GetString());
        Assert.Equal(veiculo.Chassi, corpo.RootElement.GetProperty("chassi").GetString());
        Assert.Equal(veiculo.AtualizadoEm, corpo.RootElement.GetProperty("atualizadoEm").GetDateTimeOffset());
    }

    [Fact]
    public async Task SincronizarAsync_VendasRespondeErro_NaoPropagaExcecao()
    {
        var handler = new RespostasFalsas(HttpStatusCode.InternalServerError);
        var cliente = new HttpClient(handler) { BaseAddress = new Uri("http://vendas.teste") };
        var sincronizacao = new VendasSincronizacaoHttp(cliente, NullLogger<VendasSincronizacaoHttp>.Instance);

        var excecao = await Record.ExceptionAsync(() => sincronizacao.SincronizarAsync(CriarVeiculo(), CancellationToken.None));

        Assert.Null(excecao);
    }

    [Fact]
    public async Task SincronizarAsync_ErroTransitorio_RepeteAteSucesso()
    {
        var handler = new RespostasFalsas(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddHttpClient<IVendasSincronizacao, VendasSincronizacaoHttp>(cliente =>
                cliente.BaseAddress = new Uri("http://vendas.teste"))
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddStandardResilienceHandler(VendasSincronizacaoResiliencia.Configurar);
        using var provedor = servicos.BuildServiceProvider();
        var sincronizacao = provedor.GetRequiredService<IVendasSincronizacao>();

        await sincronizacao.SincronizarAsync(CriarVeiculo(), CancellationToken.None);

        Assert.Equal(2, handler.Requisicoes.Count);
    }

    private static VeiculoDto CriarVeiculo() => new(
        Guid.NewGuid(), "Fiat", "Argo", 2025, "Prata", 85000.00m, "9BWZZZ377VT004251", null,
        StatusVeiculo.Disponivel, Agora, Agora);

    private sealed class RespostasFalsas(params HttpStatusCode[] sequencia) : HttpMessageHandler
    {
        private int _indice;

        public List<HttpRequestMessage> Requisicoes { get; } = [];

        public string? CorpoUltimo { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requisicoes.Add(request);
            CorpoUltimo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            var status = sequencia[Math.Min(_indice++, sequencia.Length - 1)];
            return new HttpResponseMessage(status);
        }
    }
}
