using System.ComponentModel.DataAnnotations;

namespace VeiculosApi.Infrastructure.Armazenamento;

public sealed class ArmazenamentoFotosOptions
{
    public const string Secao = "Minio";

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string AccessKey { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string Bucket { get; set; } = string.Empty;

    public bool Secure { get; set; }
}
