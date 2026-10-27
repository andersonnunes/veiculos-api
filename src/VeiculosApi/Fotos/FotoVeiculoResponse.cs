using VeiculosApi.Application.Veiculos.Fotos;

namespace VeiculosApi.Fotos;

public sealed record FotoVeiculoResponse(
    Guid Id,
    Guid VeiculoId,
    string ContentType,
    long Tamanho,
    DateTimeOffset CriadoEm,
    string Url)
{
    public static FotoVeiculoResponse De(FotoVeiculoDto foto) => new(
        foto.Id,
        foto.VeiculoId,
        foto.ContentType,
        foto.Tamanho,
        foto.CriadoEm,
        $"/veiculos/{foto.VeiculoId}/fotos/{foto.Id}/conteudo");
}
