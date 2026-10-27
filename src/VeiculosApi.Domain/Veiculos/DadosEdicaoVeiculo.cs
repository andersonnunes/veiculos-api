namespace VeiculosApi.Domain.Veiculos;

public sealed record DadosEdicaoVeiculo(
    string Marca,
    string Modelo,
    int Ano,
    string Cor,
    decimal Preco);
