using Microsoft.Extensions.Options;
using Minio;
using VeiculosApi.Infrastructure.Armazenamento;

namespace VeiculosApi.Tests.Fotos;

/// <summary>
/// Testa o adaptador contra um MinIO real. Só executa quando TESTE_MINIO_ENDPOINT está definido,
/// com TESTE_MINIO_ACCESS_KEY, TESTE_MINIO_SECRET_KEY e TESTE_MINIO_BUCKET. Sem a variável o teste
/// retorna sem verificar nada, então confira a saída antes de dar o adaptador como validado.
/// </summary>
public class MinioArmazenamentoFotosTests
{
    [Fact]
    public async Task SalvarObterERemover_PreservamOConteudo()
    {
        var endpoint = Environment.GetEnvironmentVariable("TESTE_MINIO_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
            return;

        var opcoes = new ArmazenamentoFotosOptions
        {
            Endpoint = endpoint,
            AccessKey = Environment.GetEnvironmentVariable("TESTE_MINIO_ACCESS_KEY") ?? string.Empty,
            SecretKey = Environment.GetEnvironmentVariable("TESTE_MINIO_SECRET_KEY") ?? string.Empty,
            Bucket = Environment.GetEnvironmentVariable("TESTE_MINIO_BUCKET") ?? "veiculos-fotos-teste",
        };
        var cliente = new MinioClient().WithEndpoint(opcoes.Endpoint).WithCredentials(opcoes.AccessKey, opcoes.SecretKey).Build();
        var adaptador = new MinioArmazenamentoFotos(cliente, Options.Create(opcoes));
        var chave = $"teste/{Guid.NewGuid()}.jpg";
        var bytes = ArquivosDeImagem.Jpeg(256);

        await adaptador.SalvarAsync(chave, bytes, "image/jpeg", CancellationToken.None);
        var lidos = await adaptador.ObterAsync(chave, CancellationToken.None);
        await adaptador.RemoverAsync(chave, CancellationToken.None);

        Assert.Equal(bytes, lidos);
    }
}
