namespace VeiculosApi.Application.Veiculos;

public sealed class VeiculoDuplicadoException : Exception
{
    public VeiculoDuplicadoException(string campo)
        : base("Já existe um veículo cadastrado com este dado.")
    {
        Campo = campo;
    }

    public string Campo { get; }
}
