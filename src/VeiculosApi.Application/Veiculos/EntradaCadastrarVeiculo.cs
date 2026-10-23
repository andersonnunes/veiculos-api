namespace VeiculosApi.Application.Veiculos;

public sealed record EntradaCadastrarVeiculo(
    string Marca,
    string Modelo,
    int Ano,
    string Cor,
    decimal Preco,
    string Chassi,
    string? Placa);
