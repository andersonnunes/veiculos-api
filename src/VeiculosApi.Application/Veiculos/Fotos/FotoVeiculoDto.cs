using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Application.Veiculos.Fotos;

public sealed record FotoVeiculoDto(Guid Id, Guid VeiculoId, string ContentType, long Tamanho, DateTimeOffset CriadoEm)
{
    public static FotoVeiculoDto De(FotoVeiculo foto) =>
        new(foto.Id, foto.VeiculoId, foto.ContentType, foto.Tamanho, foto.CriadoEm);
}
