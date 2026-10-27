using System.ComponentModel.DataAnnotations;

namespace VeiculosApi.Infrastructure.Sincronizacao;

public sealed class VendasApiOptions
{
    public const string Secao = "VendasApi";

    [Required] public string BaseUrl { get; init; } = string.Empty;
}
