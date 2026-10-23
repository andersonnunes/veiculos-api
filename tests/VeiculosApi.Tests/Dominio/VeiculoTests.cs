using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Tests.Dominio;

public class VeiculoTests
{
    private const string ChassiValido = "9BWZZZ377VT004251";
    private static readonly DateTimeOffset Agora = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Cadastrar_DadosValidos_CriaVeiculoDisponivel()
    {
        var veiculo = Criar();

        Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
        Assert.Equal(Agora, veiculo.CriadoEm);
    }

    [Fact]
    public void Cadastrar_NormalizaTextoChassiEPlaca()
    {
        var veiculo = Criar(marca: "  Fiat  ", chassi: " 9bwzzz377vt004251 ", placa: " abc1d23 ");

        Assert.Equal("Fiat", veiculo.Marca);
        Assert.Equal(ChassiValido, veiculo.Chassi);
        Assert.Equal("ABC1D23", veiculo.Placa);
    }

    [Fact]
    public void Cadastrar_PlacaEmBrancoViraNula()
    {
        var veiculo = Criar(placa: "   ");

        Assert.Null(veiculo.Placa);
    }

    [Fact]
    public void Cadastrar_MarcaVazia_RejeitaCampoMarca()
    {
        AssertErroNoCampo("marca", () => Criar(marca: "   "));
    }

    [Fact]
    public void Cadastrar_ModeloAcimaDoLimite_RejeitaCampoModelo()
    {
        AssertErroNoCampo("modelo", () => Criar(modelo: new string('M', 51)));
    }

    [Fact]
    public void Cadastrar_CorAcimaDoLimite_RejeitaCampoCor()
    {
        AssertErroNoCampo("cor", () => Criar(cor: new string('C', 31)));
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2028)]
    public void Cadastrar_AnoForaDaFaixa_RejeitaCampoAno(int ano)
    {
        AssertErroNoCampo("ano", () => Criar(ano: ano));
    }

    [Fact]
    public void Cadastrar_AnoDoProximoModelo_EhAceito()
    {
        var veiculo = Criar(ano: 2027);

        Assert.Equal(2027, veiculo.Ano);
    }

    [Fact]
    public void Cadastrar_PrecoZeroOuNegativo_RejeitaCampoPreco()
    {
        AssertErroNoCampo("preco", () => Criar(preco: 0m));
    }

    [Fact]
    public void Cadastrar_PrecoComTresCasasDecimais_RejeitaCampoPreco()
    {
        AssertErroNoCampo("preco", () => Criar(preco: 10.005m));
    }

    [Fact]
    public void Cadastrar_ChassiComLetraI_RejeitaCampoChassi()
    {
        AssertErroNoCampo("chassi", () => Criar(chassi: "9BWZZZ377VT00425I"));
    }

    [Fact]
    public void Cadastrar_ChassiComTamanhoErrado_RejeitaCampoChassi()
    {
        AssertErroNoCampo("chassi", () => Criar(chassi: "9BWZZZ377VT0042"));
    }

    [Fact]
    public void Cadastrar_PlacaComTamanhoErrado_RejeitaCampoPlaca()
    {
        AssertErroNoCampo("placa", () => Criar(placa: "ABC12"));
    }

    private static void AssertErroNoCampo(string campo, Func<Veiculo> cadastro)
    {
        var excecao = Assert.Throws<ValidacaoVeiculoException>(cadastro);

        Assert.True(excecao.Erros.ContainsKey(campo), $"Esperava erro no campo '{campo}'.");
    }

    private static Veiculo Criar(
        string marca = "Fiat",
        string modelo = "Argo",
        int ano = 2025,
        string cor = "Prata",
        decimal preco = 85000.00m,
        string chassi = ChassiValido,
        string? placa = null) =>
        Veiculo.Cadastrar(Guid.NewGuid(), new DadosVeiculo(marca, modelo, ano, cor, preco, chassi, placa), 2026, Agora);
}
