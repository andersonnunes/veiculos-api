namespace VeiculosApi.Application.Veiculos.Fotos;

public sealed class LimiteFotosExcedidoException(int limite)
    : Exception($"O veículo já possui o máximo de {limite} fotos.")
{
    public int Limite { get; } = limite;
}
