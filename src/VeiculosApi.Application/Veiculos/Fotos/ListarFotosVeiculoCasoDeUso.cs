namespace VeiculosApi.Application.Veiculos.Fotos;

public sealed class ListarFotosVeiculoCasoDeUso(IVeiculoRepositorio veiculos, IFotoVeiculoRepositorio fotos)
{
    /// <summary>Retorna null quando o veículo não existe.</summary>
    public async Task<IReadOnlyList<FotoVeiculoDto>?> ExecutarAsync(Guid veiculoId, CancellationToken cancellationToken)
    {
        if (await veiculos.ObterPorIdAsync(veiculoId, cancellationToken) is null)
            return null;

        var lista = await fotos.ListarPorVeiculoAsync(veiculoId, cancellationToken);
        return lista.Select(FotoVeiculoDto.De).ToList();
    }
}
