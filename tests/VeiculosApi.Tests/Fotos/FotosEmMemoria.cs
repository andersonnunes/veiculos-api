using System.Collections.Concurrent;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Application.Veiculos.Fotos;
using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Tests.Fotos;

public sealed class ArmazenamentoFotosEmMemoria : IArmazenamentoFotos
{
    public ConcurrentDictionary<string, (byte[] Conteudo, string ContentType)> Objetos { get; } = new();
    public ConcurrentBag<string> Removidos { get; } = [];

    public Task SalvarAsync(string chave, byte[] conteudo, string contentType, CancellationToken cancellationToken)
    {
        Objetos[chave] = (conteudo, contentType);
        return Task.CompletedTask;
    }

    public Task<byte[]> ObterAsync(string chave, CancellationToken cancellationToken) =>
        Task.FromResult(Objetos[chave].Conteudo);

    public Task RemoverAsync(string chave, CancellationToken cancellationToken)
    {
        Objetos.TryRemove(chave, out _);
        Removidos.Add(chave);
        return Task.CompletedTask;
    }
}

public sealed class FotosRepositorioEmMemoria : IFotoVeiculoRepositorio
{
    public List<FotoVeiculo> Itens { get; } = [];
    public bool FalharAoAdicionar { get; init; }

    public Task<bool> AdicionarSeAbaixoDoLimiteAsync(FotoVeiculo foto, int limite, CancellationToken cancellationToken)
    {
        if (FalharAoAdicionar)
            throw new InvalidOperationException("Falha simulada ao persistir a foto.");

        lock (Itens)
        {
            if (Itens.Count(f => f.VeiculoId == foto.VeiculoId) >= limite)
                return Task.FromResult(false);

            Itens.Add(foto);
            return Task.FromResult(true);
        }
    }

    public Task<IReadOnlyList<FotoVeiculo>> ListarPorVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FotoVeiculo>>(Itens.Where(f => f.VeiculoId == veiculoId).ToList());

    public Task<FotoVeiculo?> ObterAsync(Guid veiculoId, Guid fotoId, CancellationToken cancellationToken) =>
        Task.FromResult(Itens.FirstOrDefault(f => f.VeiculoId == veiculoId && f.Id == fotoId));
}

public sealed class VeiculosRepositorioFalso : IVeiculoRepositorio
{
    public List<Veiculo> Itens { get; } = [];

    public Task<string?> CampoDuplicadoAsync(string chassi, string? placa, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(null);

    public Task AdicionarAsync(Veiculo veiculo, CancellationToken cancellationToken)
    {
        Itens.Add(veiculo);
        return Task.CompletedTask;
    }

    public Task<Veiculo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Itens.FirstOrDefault(v => v.Id == id));

    public Task<Veiculo?> ObterParaAlteracaoAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Itens.FirstOrDefault(v => v.Id == id));

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Bytes mínimos com assinatura real de cada formato aceito.</summary>
public static class ArquivosDeImagem
{
    public static byte[] Jpeg(int tamanho = 64)
    {
        var bytes = new byte[tamanho];
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        bytes[3] = 0xE0;
        return bytes;
    }

    public static byte[] Png(int tamanho = 64)
    {
        var bytes = new byte[tamanho];
        byte[] assinatura = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        assinatura.CopyTo(bytes, 0);
        return bytes;
    }

    public static byte[] Webp(int tamanho = 64)
    {
        var bytes = new byte[tamanho];
        "RIFF"u8.CopyTo(bytes);
        "WEBP"u8.CopyTo(bytes.AsSpan(8));
        return bytes;
    }
}
