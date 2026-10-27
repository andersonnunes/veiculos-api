using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Application.Veiculos.Fotos;

public interface IFotoVeiculoRepositorio
{
    Task<int> ContarPorVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken);

    Task AdicionarAsync(FotoVeiculo foto, CancellationToken cancellationToken);

    Task<IReadOnlyList<FotoVeiculo>> ListarPorVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken);

    Task<FotoVeiculo?> ObterAsync(Guid veiculoId, Guid fotoId, CancellationToken cancellationToken);
}
