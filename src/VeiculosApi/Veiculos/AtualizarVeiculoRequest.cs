using VeiculosApi.Application.Veiculos;

namespace VeiculosApi.Veiculos;

public sealed record AtualizarVeiculoRequest(
    string Marca,
    string Modelo,
    int? Ano,
    string Cor,
    decimal? Preco)
{
    // Ausente vira zero, que a regra de domínio rejeita com mensagem por campo.
    public EntradaAtualizarVeiculo ParaEntrada() =>
        new(Marca, Modelo, Ano ?? 0, Cor, Preco ?? 0m);
}
