using Microsoft.Extensions.Time.Testing;
using VeiculosApi.Application.Veiculos.Fotos;
using VeiculosApi.Domain.Veiculos;
using VeiculosApi.Tests.Fotos;

namespace VeiculosApi.Tests.Aplicacao;

public class AdicionarFotoVeiculoCasoDeUsoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Executar_VeiculoInexistente_RetornaNuloSemGravarNada()
    {
        var veiculos = new VeiculosRepositorioFalso();
        var fotos = new FotosRepositorioEmMemoria();
        var armazenamento = new ArmazenamentoFotosEmMemoria();
        var casoDeUso = new AdicionarFotoVeiculoCasoDeUso(veiculos, fotos, armazenamento, new FakeTimeProvider(Agora));

        var resultado = await casoDeUso.ExecutarAsync(Guid.NewGuid(), ArquivosDeImagem.Jpeg(), CancellationToken.None);

        Assert.Null(resultado);
        Assert.Empty(fotos.Itens);
        Assert.Empty(armazenamento.Objetos);
    }

    [Fact]
    public async Task Executar_FotoValida_GravaNoArmazenamentoEPersisteMetadados()
    {
        var veiculo = AdicionarVeiculo(new VeiculosRepositorioFalso(), out var veiculos);
        var fotos = new FotosRepositorioEmMemoria();
        var armazenamento = new ArmazenamentoFotosEmMemoria();
        var casoDeUso = new AdicionarFotoVeiculoCasoDeUso(veiculos, fotos, armazenamento, new FakeTimeProvider(Agora));

        var resultado = await casoDeUso.ExecutarAsync(veiculo.Id, ArquivosDeImagem.Png(), CancellationToken.None);

        Assert.NotNull(resultado);
        Assert.Equal(veiculo.Id, resultado.VeiculoId);
        var persistida = Assert.Single(fotos.Itens);
        Assert.Equal(resultado.Id, persistida.Id);
        Assert.Single(armazenamento.Objetos);
        Assert.Equal("image/png", armazenamento.Objetos[persistida.ChaveArmazenamento].ContentType);
    }

    [Fact]
    public async Task Executar_LimiteDeFotosAtingido_LancaSemGravar()
    {
        var veiculo = AdicionarVeiculo(new VeiculosRepositorioFalso(), out var veiculos);
        var fotos = new FotosRepositorioEmMemoria();
        for (var i = 0; i < AdicionarFotoVeiculoCasoDeUso.MaximoFotosPorVeiculo; i++)
            fotos.Itens.Add(FotoVeiculo.Criar(Guid.NewGuid(), veiculo.Id, ArquivosDeImagem.Jpeg(), Agora));
        var armazenamento = new ArmazenamentoFotosEmMemoria();
        var casoDeUso = new AdicionarFotoVeiculoCasoDeUso(veiculos, fotos, armazenamento, new FakeTimeProvider(Agora));

        var excecao = await Assert.ThrowsAsync<LimiteFotosExcedidoException>(() =>
            casoDeUso.ExecutarAsync(veiculo.Id, ArquivosDeImagem.Jpeg(), CancellationToken.None));

        Assert.Equal(AdicionarFotoVeiculoCasoDeUso.MaximoFotosPorVeiculo, excecao.Limite);
        Assert.Empty(armazenamento.Objetos);
    }

    [Fact]
    public async Task Executar_FalhaAoPersistir_RemoveOBinarioJaGravado()
    {
        var veiculo = AdicionarVeiculo(new VeiculosRepositorioFalso(), out var veiculos);
        var fotos = new FotosRepositorioEmMemoria { FalharAoAdicionar = true };
        var armazenamento = new ArmazenamentoFotosEmMemoria();
        var casoDeUso = new AdicionarFotoVeiculoCasoDeUso(veiculos, fotos, armazenamento, new FakeTimeProvider(Agora));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            casoDeUso.ExecutarAsync(veiculo.Id, ArquivosDeImagem.Jpeg(), CancellationToken.None));

        var removido = Assert.Single(armazenamento.Removidos);
        Assert.Empty(armazenamento.Objetos);
        Assert.Contains(veiculo.Id.ToString(), removido);
    }

    [Fact]
    public async Task Executar_ArquivoInvalido_LancaValidacaoAntesDeGravar()
    {
        var veiculo = AdicionarVeiculo(new VeiculosRepositorioFalso(), out var veiculos);
        var armazenamento = new ArmazenamentoFotosEmMemoria();
        var casoDeUso = new AdicionarFotoVeiculoCasoDeUso(veiculos, new FotosRepositorioEmMemoria(), armazenamento, new FakeTimeProvider(Agora));

        await Assert.ThrowsAsync<ValidacaoVeiculoException>(() =>
            casoDeUso.ExecutarAsync(veiculo.Id, "texto"u8.ToArray(), CancellationToken.None));

        Assert.Empty(armazenamento.Objetos);
    }

    private static Veiculo AdicionarVeiculo(VeiculosRepositorioFalso repositorio, out VeiculosRepositorioFalso saida)
    {
        var veiculo = Veiculo.Cadastrar(
            Guid.NewGuid(),
            new DadosVeiculo("Toyota", "Corolla XEi", 2024, "Prata", 139900m, "9BWZZZ377VT004251", "BRA2E19"),
            2026,
            Agora);
        repositorio.Itens.Add(veiculo);
        saida = repositorio;
        return veiculo;
    }
}
