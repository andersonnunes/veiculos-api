using VeiculosApi.Application.Veiculos;
using VeiculosApi.Domain.Veiculos;
using VeiculosApi.Tests.Fotos;

namespace VeiculosApi.Tests.Aplicacao;

public class AplicarStatusVendaCasoDeUsoTests
{
    private static readonly DateTimeOffset Criacao = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Venda = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecutarAsync_VeiculoInexistente_RetornaNulo()
    {
        var casoDeUso = new AplicarStatusVendaCasoDeUso(new VeiculosRepositorioFalso());

        var resultado = await casoDeUso.ExecutarAsync(Guid.NewGuid(), StatusVeiculo.Vendido, Venda, CancellationToken.None);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ExecutarAsync_VeiculoExistente_AplicaStatusSemDispararSincronizacao()
    {
        var repositorio = new VeiculosRepositorioFalso();
        var veiculo = Veiculo.Cadastrar(
            Guid.NewGuid(),
            new DadosVeiculo("Honda", "Civic", 2024, "Preto", 129900m, "9BWZZZ377VT004251", null),
            2026,
            Criacao);
        repositorio.Itens.Add(veiculo);
        var casoDeUso = new AplicarStatusVendaCasoDeUso(repositorio);

        var resultado = await casoDeUso.ExecutarAsync(veiculo.Id, StatusVeiculo.Vendido, Venda, CancellationToken.None);

        Assert.NotNull(resultado);
        Assert.Equal(StatusVeiculo.Vendido, resultado.Status);
        Assert.Equal(StatusVeiculo.Vendido, veiculo.Status);
    }
}
