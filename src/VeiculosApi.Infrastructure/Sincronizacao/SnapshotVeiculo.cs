using VeiculosApi.Application.Veiculos;

namespace VeiculosApi.Infrastructure.Sincronizacao;

internal sealed record SnapshotVeiculo(
    Guid Id,
    string Marca,
    string Modelo,
    int Ano,
    string Cor,
    decimal Preco,
    string Chassi,
    string? Placa,
    string Status,
    DateTimeOffset AtualizadoEm)
{
    public static SnapshotVeiculo De(VeiculoDto veiculo) => new(
        veiculo.Id,
        veiculo.Marca,
        veiculo.Modelo,
        veiculo.Ano,
        veiculo.Cor,
        veiculo.Preco,
        veiculo.Chassi,
        veiculo.Placa,
        veiculo.Status.ToString().ToLowerInvariant(),
        veiculo.AtualizadoEm);
}
