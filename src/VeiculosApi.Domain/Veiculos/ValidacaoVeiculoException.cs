namespace VeiculosApi.Domain.Veiculos;

public sealed class ValidacaoVeiculoException : Exception
{
    public ValidacaoVeiculoException(IReadOnlyDictionary<string, string[]> erros)
        : base("Dados do veículo inválidos.")
    {
        Erros = erros;
    }

    public IReadOnlyDictionary<string, string[]> Erros { get; }
}
