using VeiculosApi.Domain.Veiculos;
using VeiculosApi.Tests.Fotos;

namespace VeiculosApi.Tests.Dominio;

public class FotoVeiculoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid VeiculoId = Guid.Parse("0196f0a4-0000-7000-8000-000000000001");
    private static readonly Guid FotoId = Guid.Parse("0196f0a4-0000-7000-8000-000000000002");

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/webp", ".webp")]
    public void Criar_FormatoSuportado_DefineTipoEChaveComExtensaoCorreta(string contentType, string extensao)
    {
        var bytes = contentType switch
        {
            "image/jpeg" => ArquivosDeImagem.Jpeg(),
            "image/png" => ArquivosDeImagem.Png(),
            _ => ArquivosDeImagem.Webp(),
        };

        var foto = FotoVeiculo.Criar(FotoId, VeiculoId, bytes, Agora);

        Assert.Equal(contentType, foto.ContentType);
        Assert.Equal(bytes.Length, foto.Tamanho);
        Assert.Equal($"veiculos/{VeiculoId}/{FotoId}{extensao}", foto.ChaveArmazenamento);
        Assert.Equal(Agora, foto.CriadoEm);
    }

    [Fact]
    public void Criar_ConteudoVazio_LancaValidacaoNoCampoArquivo()
    {
        var excecao = Assert.Throws<ValidacaoVeiculoException>(() =>
            FotoVeiculo.Criar(FotoId, VeiculoId, [], Agora));

        Assert.Contains("arquivo", excecao.Erros.Keys);
    }

    [Fact]
    public void Criar_AcimaDoLimite_LancaValidacao()
    {
        var acimaDoLimite = ArquivosDeImagem.Jpeg((int)FotoVeiculo.TamanhoMaximoBytes + 1);

        var excecao = Assert.Throws<ValidacaoVeiculoException>(() =>
            FotoVeiculo.Criar(FotoId, VeiculoId, acimaDoLimite, Agora));

        Assert.Contains("arquivo", excecao.Erros.Keys);
    }

    [Fact]
    public void Criar_NoLimiteExato_Aceita()
    {
        var noLimite = ArquivosDeImagem.Jpeg((int)FotoVeiculo.TamanhoMaximoBytes);

        var foto = FotoVeiculo.Criar(FotoId, VeiculoId, noLimite, Agora);

        Assert.Equal(FotoVeiculo.TamanhoMaximoBytes, foto.Tamanho);
    }

    [Fact]
    public void Criar_ArquivoQueNaoEImagem_LancaValidacao()
    {
        var pdf = "%PDF-1.7 conteúdo qualquer"u8.ToArray();

        var excecao = Assert.Throws<ValidacaoVeiculoException>(() =>
            FotoVeiculo.Criar(FotoId, VeiculoId, pdf, Agora));

        Assert.Contains("arquivo", excecao.Erros.Keys);
    }

    [Fact]
    public void Criar_RiffQueNaoEWebp_NaoEAceito()
    {
        var wav = new byte[32];
        "RIFF"u8.CopyTo(wav);
        "WAVE"u8.CopyTo(wav.AsSpan(8));

        Assert.Throws<ValidacaoVeiculoException>(() => FotoVeiculo.Criar(FotoId, VeiculoId, wav, Agora));
    }
}
