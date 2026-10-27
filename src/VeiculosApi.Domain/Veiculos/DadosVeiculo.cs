namespace VeiculosApi.Domain.Veiculos;

public sealed record DadosVeiculo(
    string Marca,
    string Modelo,
    int Ano,
    string Cor,
    decimal Preco,
    string Chassi,
    string? Placa);
