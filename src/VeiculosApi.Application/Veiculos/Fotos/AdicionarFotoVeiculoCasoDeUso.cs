using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Application.Veiculos.Fotos;

public sealed class AdicionarFotoVeiculoCasoDeUso(
    IVeiculoRepositorio veiculos,
    IFotoVeiculoRepositorio fotos,
    IArmazenamentoFotos armazenamento,
    TimeProvider relogio)
{
    public const int MaximoFotosPorVeiculo = 10;

    /// <summary>Retorna null quando o veículo não existe.</summary>
    public async Task<FotoVeiculoDto?> ExecutarAsync(Guid veiculoId, byte[] conteudo, CancellationToken cancellationToken)
    {
        if (await veiculos.ObterPorIdAsync(veiculoId, cancellationToken) is null)
            return null;

        if (await fotos.ContarPorVeiculoAsync(veiculoId, cancellationToken) >= MaximoFotosPorVeiculo)
            throw new LimiteFotosExcedidoException(MaximoFotosPorVeiculo);

        var agora = relogio.GetUtcNow();
        var foto = FotoVeiculo.Criar(Guid.CreateVersion7(agora), veiculoId, conteudo, agora);

        await armazenamento.SalvarAsync(foto.ChaveArmazenamento, conteudo, foto.ContentType, cancellationToken);

        try
        {
            await fotos.AdicionarAsync(foto, cancellationToken);
        }
        catch
        {
            // Sem metadados não há como referenciar o binário: desfaz a gravação no armazenamento.
            await armazenamento.RemoverAsync(foto.ChaveArmazenamento, CancellationToken.None);
            throw;
        }

        return FotoVeiculoDto.De(foto);
    }
}
