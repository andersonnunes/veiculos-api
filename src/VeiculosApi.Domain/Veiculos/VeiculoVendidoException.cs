namespace VeiculosApi.Domain.Veiculos;

public sealed class VeiculoVendidoException : Exception
{
    public VeiculoVendidoException(Guid id)
        : base("Veículo vendido não pode ser alterado.")
    {
        Id = id;
    }

    public Guid Id { get; }
}
