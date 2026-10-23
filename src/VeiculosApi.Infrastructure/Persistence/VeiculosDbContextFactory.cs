using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VeiculosApi.Infrastructure.Persistence;

// Usada só pelo dotnet ef (gerar migrations); a conexão real vem da API em tempo de execução.
[ExcludeFromCodeCoverage]
internal sealed class VeiculosDbContextFactory : IDesignTimeDbContextFactory<VeiculosDbContext>
{
    public VeiculosDbContext CreateDbContext(string[] args)
    {
        var senha = Environment.GetEnvironmentVariable("VEICULOS_DB_PASSWORD") ?? string.Empty;
        var opcoes = new DbContextOptionsBuilder<VeiculosDbContext>()
            .UseNpgsql($"Host=localhost;Database=veiculos;Username=veiculos;Password={senha}")
            .Options;

        return new VeiculosDbContext(opcoes);
    }
}
