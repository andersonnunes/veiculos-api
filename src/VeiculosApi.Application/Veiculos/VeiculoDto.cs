using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Application.Veiculos;

public sealed record VeiculoDto(
    Guid Id,
    string Marca,
    string Modelo,
    int Ano,
    string Cor,
    decimal Preco,
    string Chassi,
    string? Placa,
    StatusVeiculo Status,
    DateTimeOffset CriadoEm)
{
    public static VeiculoDto De(Veiculo veiculo) => new(
        veiculo.Id,
        veiculo.Marca,
        veiculo.Modelo,
        veiculo.Ano,
        veiculo.Cor,
        veiculo.Preco,
        veiculo.Chassi,
        veiculo.Placa,
        veiculo.Status,
        veiculo.CriadoEm);
}
