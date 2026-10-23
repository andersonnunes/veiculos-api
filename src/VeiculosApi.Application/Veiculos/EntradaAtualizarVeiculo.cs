namespace VeiculosApi.Application.Veiculos;

public sealed record EntradaAtualizarVeiculo(
    string Marca,
    string Modelo,
    int Ano,
    string Cor,
    decimal Preco);
