using Microsoft.EntityFrameworkCore;
using Npgsql;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Infrastructure.Persistence;

internal sealed class EfVeiculoRepositorio(VeiculosDbContext contexto) : IVeiculoRepositorio
{
    private const string SqlStateViolacaoUnicidade = "23505";

    public Task<string?> CampoDuplicadoAsync(string chassi, string? placa, CancellationToken cancellationToken)
    {
        return contexto.Veiculos
            .Where(v => v.Chassi == chassi || (placa != null && v.Placa == placa))
            .Select(v => v.Chassi == chassi ? "chassi" : "placa")
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AdicionarAsync(Veiculo veiculo, CancellationToken cancellationToken)
    {
        contexto.Veiculos.Add(veiculo);

        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (excecao.InnerException is PostgresException { SqlState: SqlStateViolacaoUnicidade } postgres)
        {
            contexto.ChangeTracker.Clear();
            var campo = postgres.ConstraintName?.Contains("placa", StringComparison.OrdinalIgnoreCase) == true
                ? "placa"
                : "chassi";
            throw new VeiculoDuplicadoException(campo);
        }
    }

    public Task<Veiculo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return contexto.Veiculos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public Task<Veiculo?> ObterParaAlteracaoAsync(Guid id, CancellationToken cancellationToken)
    {
        return contexto.Veiculos.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            contexto.ChangeTracker.Clear();
            throw new VeiculoConflitoConcorrenciaException();
        }
    }
}
