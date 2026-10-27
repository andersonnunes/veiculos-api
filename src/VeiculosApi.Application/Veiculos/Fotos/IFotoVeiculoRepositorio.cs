using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Application.Veiculos.Fotos;

public interface IFotoVeiculoRepositorio
{
    /// <summary>
    /// Adiciona a foto somente se o veículo ainda tiver menos fotos que o limite. A verificação e a
    /// gravação são atômicas: retorna false, sem gravar, quando o limite já foi atingido.
    /// </summary>
    Task<bool> AdicionarSeAbaixoDoLimiteAsync(FotoVeiculo foto, int limite, CancellationToken cancellationToken);

    Task<IReadOnlyList<FotoVeiculo>> ListarPorVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken);

    Task<FotoVeiculo?> ObterAsync(Guid veiculoId, Guid fotoId, CancellationToken cancellationToken);
}
