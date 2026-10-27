namespace VeiculosApi.Application.Veiculos.Fotos;

/// <summary>Porta de armazenamento de binários. A implementação atual usa MinIO.</summary>
public interface IArmazenamentoFotos
{
    Task SalvarAsync(string chave, byte[] conteudo, string contentType, CancellationToken cancellationToken);

    Task<byte[]> ObterAsync(string chave, CancellationToken cancellationToken);

    Task RemoverAsync(string chave, CancellationToken cancellationToken);
}
