using Microsoft.Extensions.Time.Testing;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Tests.Aplicacao;

public class AtualizarVeiculoCasoDeUsoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecutarAsync_VeiculoExistente_AtualizaESalva()
    {
        var repositorio = new RepositorioEmMemoria();
        var cadastrado = await new CadastrarVeiculoCasoDeUso(repositorio, new FakeTimeProvider(Agora))
            .ExecutarAsync(new EntradaCadastrarVeiculo("Fiat", "Argo", 2025, "Prata", 85000.00m, "9BWZZZ377VT004251", null), CancellationToken.None);

        var resultado = await new AtualizarVeiculoCasoDeUso(repositorio, new FakeTimeProvider(Agora))
            .ExecutarAsync(cadastrado.Id, new EntradaAtualizarVeiculo("Toyota", "Corolla", 2024, "Preto", 120000.00m), CancellationToken.None);

        Assert.NotNull(resultado);
        Assert.Equal("Toyota", resultado.Marca);
        Assert.Equal(120000.00m, resultado.Preco);
        Assert.Equal(cadastrado.Chassi, resultado.Chassi);
        Assert.True(repositorio.SalvouAlteracoes);
    }

    [Fact]
    public async Task ExecutarAsync_VeiculoInexistente_RetornaNulo()
    {
        var casoDeUso = new AtualizarVeiculoCasoDeUso(new RepositorioEmMemoria(), new FakeTimeProvider(Agora));

        var resultado = await casoDeUso.ExecutarAsync(
            Guid.NewGuid(), new EntradaAtualizarVeiculo("Toyota", "Corolla", 2024, "Preto", 120000.00m), CancellationToken.None);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ExecutarAsync_VeiculoVendido_PropagaVeiculoVendido()
    {
        var repositorio = new RepositorioEmMemoria();
        var cadastrado = await new CadastrarVeiculoCasoDeUso(repositorio, new FakeTimeProvider(Agora))
            .ExecutarAsync(new EntradaCadastrarVeiculo("Fiat", "Argo", 2025, "Prata", 85000.00m, "9BWZZZ377VT004251", null), CancellationToken.None);
        repositorio.Adicionados.Single().MarcarComoVendido();

        await Assert.ThrowsAsync<VeiculoVendidoException>(() => new AtualizarVeiculoCasoDeUso(repositorio, new FakeTimeProvider(Agora))
            .ExecutarAsync(cadastrado.Id, new EntradaAtualizarVeiculo("Toyota", "Corolla", 2024, "Preto", 120000.00m), CancellationToken.None));

        Assert.False(repositorio.SalvouAlteracoes);
    }

    private sealed class RepositorioEmMemoria : IVeiculoRepositorio
    {
        public List<Veiculo> Adicionados { get; } = [];
        public bool SalvouAlteracoes { get; private set; }

        public Task<string?> CampoDuplicadoAsync(string chassi, string? placa, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);

        public Task AdicionarAsync(Veiculo veiculo, CancellationToken cancellationToken)
        {
            Adicionados.Add(veiculo);
            return Task.CompletedTask;
        }

        public Task<Veiculo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Adicionados.FirstOrDefault(v => v.Id == id));

        public Task<Veiculo?> ObterParaAlteracaoAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Adicionados.FirstOrDefault(v => v.Id == id));

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
        {
            SalvouAlteracoes = true;
            return Task.CompletedTask;
        }
    }
}
