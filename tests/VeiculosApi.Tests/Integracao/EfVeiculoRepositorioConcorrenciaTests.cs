using Microsoft.EntityFrameworkCore;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Domain.Veiculos;
using VeiculosApi.Infrastructure.Persistence;
using VeiculosApi.Tests.Infraestrutura;

namespace VeiculosApi.Tests.Integracao;

[Collection(AmbienteCollection.NomeColecao)]
public sealed class EfVeiculoRepositorioConcorrenciaTests(PostgresFixture postgres)
{
    private const string AlfabetoChassi = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";

    [Fact]
    public async Task SalvarAlteracoesAsync_LinhaAlteradaPorOutraOperacao_LancaConflito()
    {
        await using var contextoSemente = CriarContexto();
        await contextoSemente.Database.MigrateAsync();
        var veiculo = Veiculo.Cadastrar(
            Guid.CreateVersion7(),
            new DadosVeiculo("Fiat", "Argo", 2025, "Prata", 85000.00m, GerarChassi(), null),
            2026,
            DateTimeOffset.UtcNow);
        contextoSemente.Veiculos.Add(veiculo);
        await contextoSemente.SaveChangesAsync();

        await using var contextoA = CriarContexto();
        var repositorioA = new EfVeiculoRepositorio(contextoA);
        var lidoPorA = await repositorioA.ObterParaAlteracaoAsync(veiculo.Id, CancellationToken.None);

        await using (var contextoB = CriarContexto())
        {
            var lidoPorB = await contextoB.Veiculos.FirstAsync(v => v.Id == veiculo.Id);
            lidoPorB.Atualizar(new DadosEdicaoVeiculo("Fiat", "Argo", 2025, "Branco", 85000.00m), 2026, DateTimeOffset.UtcNow);
            await contextoB.SaveChangesAsync();
        }

        lidoPorA!.Atualizar(new DadosEdicaoVeiculo("Fiat", "Argo", 2025, "Azul", 85000.00m), 2026, DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<VeiculoConflitoConcorrenciaException>(
            () => repositorioA.SalvarAlteracoesAsync(CancellationToken.None));
    }

    private VeiculosDbContext CriarContexto() => new(
        new DbContextOptionsBuilder<VeiculosDbContext>().UseNpgsql(postgres.ConnectionString).Options);

    private static string GerarChassi() =>
        "9BWZZZ377VT" + new string(Enumerable.Range(0, 6).Select(_ => AlfabetoChassi[Random.Shared.Next(AlfabetoChassi.Length)]).ToArray());
}
