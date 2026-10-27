using Microsoft.EntityFrameworkCore;
using VeiculosApi.Application.Veiculos.Fotos;
using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Infrastructure.Persistence;

internal sealed class EfFotoVeiculoRepositorio(VeiculosDbContext contexto) : IFotoVeiculoRepositorio
{
    public async Task<bool> AdicionarSeAbaixoDoLimiteAsync(FotoVeiculo foto, int limite, CancellationToken cancellationToken)
    {
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);

        // Trava a linha do veículo: uploads concorrentes para o mesmo veículo passam um de cada vez,
        // e a contagem abaixo enxerga as fotos já confirmadas pelos anteriores.
        await contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM veiculos WHERE \"Id\" = {foto.VeiculoId} FOR UPDATE", cancellationToken);

        var total = await contexto.FotosVeiculo.CountAsync(f => f.VeiculoId == foto.VeiculoId, cancellationToken);
        if (total >= limite)
        {
            await transacao.RollbackAsync(cancellationToken);
            return false;
        }

        contexto.FotosVeiculo.Add(foto);
        await contexto.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<FotoVeiculo>> ListarPorVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken) =>
        await contexto.FotosVeiculo
            .AsNoTracking()
            .Where(f => f.VeiculoId == veiculoId)
            .OrderBy(f => f.CriadoEm)
            .ToListAsync(cancellationToken);

    public Task<FotoVeiculo?> ObterAsync(Guid veiculoId, Guid fotoId, CancellationToken cancellationToken) =>
        contexto.FotosVeiculo
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.VeiculoId == veiculoId && f.Id == fotoId, cancellationToken);
}
