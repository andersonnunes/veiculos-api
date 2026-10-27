using Microsoft.Extensions.Time.Testing;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Tests.Aplicacao;

public class CadastrarVeiculoCasoDeUsoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecutarAsync_DadosValidos_PersisteVeiculoComIdEDataDoRelogio()
    {
        var repositorio = new RepositorioEmMemoria();
        var casoDeUso = new CadastrarVeiculoCasoDeUso(repositorio, new SincronizacaoFalsa(), new FakeTimeProvider(Agora));

        var resultado = await casoDeUso.ExecutarAsync(Entrada(), CancellationToken.None);

        var persistido = Assert.Single(repositorio.Adicionados);
        Assert.Equal(persistido.Id, resultado.Id);
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal(Agora, resultado.CriadoEm);
        Assert.Equal(StatusVeiculo.Disponivel, resultado.Status);
    }

    [Fact]
    public async Task ExecutarAsync_ChassiJaCadastrado_LancaDuplicadoSemPersistir()
    {
        var repositorio = new RepositorioEmMemoria { CampoDuplicado = "chassi" };
        var casoDeUso = new CadastrarVeiculoCasoDeUso(repositorio, new SincronizacaoFalsa(), new FakeTimeProvider(Agora));

        var excecao = await Assert.ThrowsAsync<VeiculoDuplicadoException>(
            () => casoDeUso.ExecutarAsync(Entrada(), CancellationToken.None));

        Assert.Equal("chassi", excecao.Campo);
        Assert.Empty(repositorio.Adicionados);
    }

    [Fact]
    public async Task ObterPorIdAsync_VeiculoInexistente_RetornaNulo()
    {
        var casoDeUso = new ObterVeiculoPorIdCasoDeUso(new RepositorioEmMemoria());

        var resultado = await casoDeUso.ExecutarAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterPorIdAsync_VeiculoExistente_RetornaDados()
    {
        var repositorio = new RepositorioEmMemoria();
        var cadastrado = await new CadastrarVeiculoCasoDeUso(repositorio, new SincronizacaoFalsa(), new FakeTimeProvider(Agora))
            .ExecutarAsync(Entrada(), CancellationToken.None);

        var resultado = await new ObterVeiculoPorIdCasoDeUso(repositorio)
            .ExecutarAsync(cadastrado.Id, CancellationToken.None);

        Assert.Equal(cadastrado, resultado);
    }

    [Fact]
    public async Task ExecutarAsync_DadosValidos_SincronizaComVendasComOMesmoVeiculo()
    {
        var repositorio = new RepositorioEmMemoria();
        var sincronizacao = new SincronizacaoFalsa();
        var casoDeUso = new CadastrarVeiculoCasoDeUso(repositorio, sincronizacao, new FakeTimeProvider(Agora));

        var resultado = await casoDeUso.ExecutarAsync(Entrada(), CancellationToken.None);

        var enviado = Assert.Single(sincronizacao.Enviados);
        Assert.Equal(resultado, enviado);
    }

    private static EntradaCadastrarVeiculo Entrada() =>
        new("Fiat", "Argo", 2025, "Prata", 85000.00m, "9BWZZZ377VT004251", null);

    private sealed class RepositorioEmMemoria : IVeiculoRepositorio
    {
        public string? CampoDuplicado { get; init; }
        public List<Veiculo> Adicionados { get; } = [];

        public Task<string?> CampoDuplicadoAsync(string chassi, string? placa, CancellationToken cancellationToken) =>
            Task.FromResult(CampoDuplicado);

        public Task AdicionarAsync(Veiculo veiculo, CancellationToken cancellationToken)
        {
            Adicionados.Add(veiculo);
            return Task.CompletedTask;
        }

        public Task<Veiculo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Adicionados.FirstOrDefault(v => v.Id == id));

        public Task<Veiculo?> ObterParaAlteracaoAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Adicionados.FirstOrDefault(v => v.Id == id));

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
