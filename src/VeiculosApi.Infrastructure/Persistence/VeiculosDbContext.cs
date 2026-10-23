using Microsoft.EntityFrameworkCore;
using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Infrastructure.Persistence;

public sealed class VeiculosDbContext(DbContextOptions<VeiculosDbContext> options) : DbContext(options)
{
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VeiculosDbContext).Assembly);
    }
}
