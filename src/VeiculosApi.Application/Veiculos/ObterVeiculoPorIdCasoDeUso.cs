namespace VeiculosApi.Application.Veiculos;

public sealed class ObterVeiculoPorIdCasoDeUso(IVeiculoRepositorio repositorio)
{
    public async Task<VeiculoDto?> ExecutarAsync(Guid id, CancellationToken cancellationToken)
    {
        var veiculo = await repositorio.ObterPorIdAsync(id, cancellationToken);
        return veiculo is null ? null : VeiculoDto.De(veiculo);
    }
}
