namespace VeiculosApi.Domain.Veiculos;

/// <summary>Campos do veículo que não podem se repetir. O nome usado no JSON (<see cref="Nome"/>) é o contrato da API.</summary>
public enum CampoUnico
{
    Chassi,
    Placa,
}

public static class CampoUnicoExtensions
{
    public static string Nome(this CampoUnico campo) => campo switch
    {
        CampoUnico.Chassi => "chassi",
        CampoUnico.Placa => "placa",
        _ => throw new ArgumentOutOfRangeException(nameof(campo), campo, "Campo único desconhecido."),
    };
}
