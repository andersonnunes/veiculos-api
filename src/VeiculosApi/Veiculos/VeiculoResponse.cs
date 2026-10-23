using VeiculosApi.Application.Veiculos;

namespace VeiculosApi.Veiculos;

public sealed record VeiculoResponse(
    Guid Id,
    string Marca,
    string Modelo,
    int Ano,
    string Cor,
    decimal Preco,
    string Chassi,
    string? Placa,
    string Status,
    DateTimeOffset CriadoEm)
{
    public static VeiculoResponse De(VeiculoDto veiculo) => new(
        veiculo.Id,
        veiculo.Marca,
        veiculo.Modelo,
        veiculo.Ano,
        veiculo.Cor,
        veiculo.Preco,
        veiculo.Chassi,
        veiculo.Placa,
        veiculo.Status.ToString().ToLowerInvariant(),
        veiculo.CriadoEm);
}
