using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Application.Veiculos;

public interface IVeiculoRepositorio
{
    Task<string?> CampoDuplicadoAsync(string chassi, string? placa, CancellationToken cancellationToken);

    Task AdicionarAsync(Veiculo veiculo, CancellationToken cancellationToken);

    Task<Veiculo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Veiculo?> ObterParaAlteracaoAsync(Guid id, CancellationToken cancellationToken);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
