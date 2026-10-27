using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using VeiculosApi.Application.Veiculos;

namespace VeiculosApi.Infrastructure.Sincronizacao;

internal sealed class VendasSincronizacaoHttp(HttpClient cliente, ILogger<VendasSincronizacaoHttp> logger)
    : IVendasSincronizacao
{
    public async Task SincronizarAsync(VeiculoDto veiculo, CancellationToken cancellationToken)
    {
        try
        {
            using var resposta = await cliente.PutAsJsonAsync(
                $"/internal/veiculos/{veiculo.Id}", SnapshotVeiculo.De(veiculo), cancellationToken);
            resposta.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException excecao)
        {
            logger.LogError(excecao, "Falha ao sincronizar veículo {VeiculoId} com vendas-api", veiculo.Id);
        }
        catch (TaskCanceledException excecao) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(excecao, "Tempo esgotado ao sincronizar veículo {VeiculoId} com vendas-api", veiculo.Id);
        }
    }
}
