using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Application.Veiculos;

public sealed class VeiculoDuplicadoException : Exception
{
    public VeiculoDuplicadoException(CampoUnico campo)
        : base("Já existe um veículo cadastrado com este dado.")
    {
        Campo = campo;
    }

    public CampoUnico Campo { get; }
}
