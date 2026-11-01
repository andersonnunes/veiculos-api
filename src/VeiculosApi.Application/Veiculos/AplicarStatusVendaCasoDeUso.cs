using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Application.Veiculos;

/// <summary>
/// Recebe o status final decidido por vendas-api. Não dispara a sincronização da US1.3:
/// vendas-api já conhece o status que enviou, e reenviar criaria um laço entre os serviços.
/// </summary>
public sealed class AplicarStatusVendaCasoDeUso(IVeiculoRepositorio repositorio)
{
    /// <summary>Retorna null quando o veículo não existe.</summary>
    public async Task<VeiculoDto?> ExecutarAsync(
        Guid id,
        StatusVeiculo status,
        DateTimeOffset ocorridoEm,
        CancellationToken cancellationToken)
    {
        var veiculo = await repositorio.ObterParaAlteracaoAsync(id, cancellationToken);
        if (veiculo is null)
            return null;

        veiculo.AplicarStatusVenda(status, ocorridoEm);
        await repositorio.SalvarAlteracoesAsync(cancellationToken);

        return VeiculoDto.De(veiculo);
    }
}
