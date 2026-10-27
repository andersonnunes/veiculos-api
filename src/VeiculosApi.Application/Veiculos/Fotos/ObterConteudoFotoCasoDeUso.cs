namespace VeiculosApi.Application.Veiculos.Fotos;

public sealed record ConteudoFoto(byte[] Bytes, string ContentType);

public sealed class ObterConteudoFotoCasoDeUso(IFotoVeiculoRepositorio fotos, IArmazenamentoFotos armazenamento)
{
    /// <summary>Retorna null quando a foto não existe para o veículo informado.</summary>
    public async Task<ConteudoFoto?> ExecutarAsync(Guid veiculoId, Guid fotoId, CancellationToken cancellationToken)
    {
        var foto = await fotos.ObterAsync(veiculoId, fotoId, cancellationToken);
        if (foto is null)
            return null;

        var bytes = await armazenamento.ObterAsync(foto.ChaveArmazenamento, cancellationToken);
        return new ConteudoFoto(bytes, foto.ContentType);
    }
}
