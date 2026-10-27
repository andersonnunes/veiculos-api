using VeiculosApi.Application.Veiculos;

namespace VeiculosApi.Tests.Aplicacao;

public sealed class SincronizacaoFalsa : IVendasSincronizacao
{
    public List<VeiculoDto> Enviados { get; } = [];

    public Task SincronizarAsync(VeiculoDto veiculo, CancellationToken cancellationToken)
    {
        Enviados.Add(veiculo);
        return Task.CompletedTask;
    }
}
