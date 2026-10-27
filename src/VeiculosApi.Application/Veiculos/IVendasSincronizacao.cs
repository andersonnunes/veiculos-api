namespace VeiculosApi.Application.Veiculos;

// Best-effort: a implementação registra a falha e não propaga, porque o veículo já foi persistido.
public interface IVendasSincronizacao
{
    Task SincronizarAsync(VeiculoDto veiculo, CancellationToken cancellationToken);
}
