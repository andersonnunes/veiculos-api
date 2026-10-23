using VeiculosApi.Veiculos;

namespace VeiculosApi.Tests.Unitarios;

public class CadastrarVeiculoRequestTests
{
    [Fact]
    public void ParaEntrada_AnoEPrecoAusentes_ViramZeroParaQueADominioRejeite()
    {
        var request = new CadastrarVeiculoRequest("Fiat", "Argo", null, "Prata", null, "9BWZZZ377VT004251", null);

        var entrada = request.ParaEntrada();

        Assert.Equal(0, entrada.Ano);
        Assert.Equal(0m, entrada.Preco);
    }

    [Fact]
    public void ParaEntrada_ValoresInformados_SaoMantidos()
    {
        var request = new CadastrarVeiculoRequest("Fiat", "Argo", 2025, "Prata", 85000.00m, "9BWZZZ377VT004251", "ABC1D23");

        var entrada = request.ParaEntrada();

        Assert.Equal(2025, entrada.Ano);
        Assert.Equal(85000.00m, entrada.Preco);
        Assert.Equal("ABC1D23", entrada.Placa);
    }
}
