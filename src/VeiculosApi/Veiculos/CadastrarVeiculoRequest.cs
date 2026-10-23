using VeiculosApi.Application.Veiculos;

namespace VeiculosApi.Veiculos;

public sealed record CadastrarVeiculoRequest(
    string Marca,
    string Modelo,
    int? Ano,
    string Cor,
    decimal? Preco,
    string Chassi,
    string? Placa)
{
    // Ausente vira zero, que a regra de domínio rejeita com mensagem por campo.
    public EntradaCadastrarVeiculo ParaEntrada() =>
        new(Marca, Modelo, Ano ?? 0, Cor, Preco ?? 0m, Chassi, Placa);
}
