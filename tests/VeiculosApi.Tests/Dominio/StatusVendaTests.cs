using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Tests.Dominio;

public class StatusVendaTests
{
    private static readonly DateTimeOffset Criacao = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Venda1 = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Venda2 = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    private static Veiculo NovoVeiculo() => Veiculo.Cadastrar(
        Guid.NewGuid(),
        new DadosVeiculo("Toyota", "Corolla", 2024, "Prata", 139900m, "9BWZZZ377VT004251", "BRA2E19"),
        2026,
        Criacao);

    [Fact]
    public void AplicarStatusVenda_VendidoAplicaEGuardaOInstante()
    {
        var veiculo = NovoVeiculo();

        veiculo.AplicarStatusVenda(StatusVeiculo.Vendido, Venda1);

        Assert.Equal(StatusVeiculo.Vendido, veiculo.Status);
        Assert.Equal(Venda1, veiculo.StatusAtualizadoEm);
    }

    [Fact]
    public void AplicarStatusVenda_NaoAlteraAtualizadoEm()
    {
        var veiculo = NovoVeiculo();

        veiculo.AplicarStatusVenda(StatusVeiculo.Vendido, Venda1);

        Assert.Equal(Criacao, veiculo.AtualizadoEm);
    }

    [Fact]
    public void AplicarStatusVenda_MesmoStatusEhIdempotente()
    {
        var veiculo = NovoVeiculo();
        veiculo.AplicarStatusVenda(StatusVeiculo.Vendido, Venda1);

        veiculo.AplicarStatusVenda(StatusVeiculo.Vendido, Venda1);

        Assert.Equal(StatusVeiculo.Vendido, veiculo.Status);
        Assert.Equal(Venda1, veiculo.StatusAtualizadoEm);
    }

    [Fact]
    public void AplicarStatusVenda_NotificacaoMaisAntigaEhDescartada()
    {
        var veiculo = NovoVeiculo();
        veiculo.AplicarStatusVenda(StatusVeiculo.Vendido, Venda2);

        veiculo.AplicarStatusVenda(StatusVeiculo.Disponivel, Venda1);

        Assert.Equal(StatusVeiculo.Vendido, veiculo.Status);
        Assert.Equal(Venda2, veiculo.StatusAtualizadoEm);
    }

    [Fact]
    public void AplicarStatusVenda_ReversaoMaisRecenteAplica()
    {
        var veiculo = NovoVeiculo();
        veiculo.AplicarStatusVenda(StatusVeiculo.Vendido, Venda1);

        veiculo.AplicarStatusVenda(StatusVeiculo.Disponivel, Venda2);

        Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
    }

    [Fact]
    public void Atualizar_DepoisDeVendidoContinuaBloqueado()
    {
        var veiculo = NovoVeiculo();
        veiculo.AplicarStatusVenda(StatusVeiculo.Vendido, Venda1);

        Assert.Throws<VeiculoVendidoException>(() => veiculo.Atualizar(
            new DadosEdicaoVeiculo("Toyota", "Corolla", 2024, "Prata", 100m), 2026, Venda2));
    }
}
