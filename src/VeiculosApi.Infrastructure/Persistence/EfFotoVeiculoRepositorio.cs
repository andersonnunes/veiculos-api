using Microsoft.EntityFrameworkCore;
using VeiculosApi.Application.Veiculos.Fotos;
using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Infrastructure.Persistence;

internal sealed class EfFotoVeiculoRepositorio(VeiculosDbContext contexto) : IFotoVeiculoRepositorio
{
    public Task<int> ContarPorVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken) =>
        contexto.FotosVeiculo.CountAsync(f => f.VeiculoId == veiculoId, cancellationToken);

    public async Task AdicionarAsync(FotoVeiculo foto, CancellationToken cancellationToken)
    {
        contexto.FotosVeiculo.Add(foto);
        await contexto.SaveChangesAsync(cancellationToken);
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
