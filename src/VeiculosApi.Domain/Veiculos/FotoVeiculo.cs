namespace VeiculosApi.Domain.Veiculos;

public sealed class FotoVeiculo
{
    public const long TamanhoMaximoBytes = 5 * 1024 * 1024;

    private static readonly byte[] AssinaturaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] AssinaturaRiff = "RIFF"u8.ToArray();
    private static readonly byte[] AssinaturaWebp = "WEBP"u8.ToArray();

    private FotoVeiculo()
    {
    }

    private FotoVeiculo(Guid id, Guid veiculoId, string contentType, long tamanho, string chaveArmazenamento, DateTimeOffset criadoEm)
    {
        Id = id;
        VeiculoId = veiculoId;
        ContentType = contentType;
        Tamanho = tamanho;
        ChaveArmazenamento = chaveArmazenamento;
        CriadoEm = criadoEm;
    }

    public Guid Id { get; private set; }
    public Guid VeiculoId { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public long Tamanho { get; private set; }
    public string ChaveArmazenamento { get; private set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; private set; }

    /// <summary>
    /// Valida o conteúdo pela assinatura do arquivo. O Content-Type declarado pelo cliente não é confiável,
    /// então o tipo gravado é sempre o detectado.
    /// </summary>
    public static FotoVeiculo Criar(Guid id, Guid veiculoId, byte[] conteudo, DateTimeOffset criadoEm)
    {
        var mensagens = new List<string>();

        if (conteudo.Length == 0)
            mensagens.Add("Envie uma imagem não vazia.");
        else if (conteudo.Length > TamanhoMaximoBytes)
            mensagens.Add("A imagem deve ter no máximo 5 MB.");

        var contentType = DetectarTipo(conteudo);
        if (contentType is null && conteudo.Length > 0)
            mensagens.Add("Formato não suportado. Envie JPEG, PNG ou WebP.");

        if (mensagens.Count > 0)
            throw new ValidacaoVeiculoException(new Dictionary<string, string[]> { ["arquivo"] = mensagens.ToArray() });

        var chave = $"veiculos/{veiculoId}/{id}{Extensao(contentType!)}";
        return new FotoVeiculo(id, veiculoId, contentType!, conteudo.LongLength, chave, criadoEm);
    }

    private static string? DetectarTipo(ReadOnlySpan<byte> conteudo)
    {
        if (conteudo.Length >= 3 && conteudo[0] == 0xFF && conteudo[1] == 0xD8 && conteudo[2] == 0xFF)
            return "image/jpeg";
        if (conteudo.Length >= AssinaturaPng.Length && conteudo[..AssinaturaPng.Length].SequenceEqual(AssinaturaPng))
            return "image/png";
        if (conteudo.Length >= 12 && conteudo[..4].SequenceEqual(AssinaturaRiff) && conteudo[8..12].SequenceEqual(AssinaturaWebp))
            return "image/webp";
        return null;
    }

    private static string Extensao(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        _ => ".webp",
    };
}
