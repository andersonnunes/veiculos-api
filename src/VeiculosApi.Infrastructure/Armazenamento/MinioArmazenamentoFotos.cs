using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using VeiculosApi.Application.Veiculos.Fotos;

namespace VeiculosApi.Infrastructure.Armazenamento;

internal sealed class MinioArmazenamentoFotos(IMinioClient cliente, IOptions<ArmazenamentoFotosOptions> opcoes)
    : IArmazenamentoFotos
{
    private readonly ArmazenamentoFotosOptions _opcoes = opcoes.Value;
    private volatile bool _bucketGarantido;

    public async Task SalvarAsync(string chave, byte[] conteudo, string contentType, CancellationToken cancellationToken)
    {
        await GarantirBucketAsync(cancellationToken);

        using var fluxo = new MemoryStream(conteudo, writable: false);
        await cliente.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_opcoes.Bucket)
                .WithObject(chave)
                .WithStreamData(fluxo)
                .WithObjectSize(conteudo.LongLength)
                .WithContentType(contentType),
            cancellationToken);
    }

    public async Task<byte[]> ObterAsync(string chave, CancellationToken cancellationToken)
    {
        using var memoria = new MemoryStream();
        await cliente.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_opcoes.Bucket)
                .WithObject(chave)
                .WithCallbackStream(fluxo => fluxo.CopyTo(memoria)),
            cancellationToken);
        return memoria.ToArray();
    }

    public Task RemoverAsync(string chave, CancellationToken cancellationToken) =>
        cliente.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(_opcoes.Bucket)
                .WithObject(chave),
            cancellationToken);

    private async Task GarantirBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketGarantido)
            return;

        var existe = await cliente.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_opcoes.Bucket), cancellationToken);
        if (!existe)
        {
            await cliente.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_opcoes.Bucket), cancellationToken);
        }

        _bucketGarantido = true;
    }
}
