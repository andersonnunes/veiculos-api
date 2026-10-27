using System.Text.RegularExpressions;

namespace VeiculosApi.Domain.Veiculos;

public sealed partial class Veiculo
{
    private const int TamanhoMaximoMarcaModelo = 50;
    private const int TamanhoMaximoCor = 30;
    private const int AnoMinimo = 1900;
    private const decimal PrecoMaximo = 9_999_999_999.99m;

    private Veiculo()
    {
    }

    public Guid Id { get; private set; }
    public string Marca { get; private set; } = string.Empty;
    public string Modelo { get; private set; } = string.Empty;
    public int Ano { get; private set; }
    public string Cor { get; private set; } = string.Empty;
    public decimal Preco { get; private set; }
    public string Chassi { get; private set; } = string.Empty;
    public string? Placa { get; private set; }
    public StatusVeiculo Status { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    public static Veiculo Cadastrar(Guid id, DadosVeiculo dados, int anoAtual, DateTimeOffset criadoEm)
    {
        var edicao = Normalizar(new DadosEdicaoVeiculo(dados.Marca, dados.Modelo, dados.Ano, dados.Cor, dados.Preco));
        var chassi = NormalizarChassi(dados.Chassi);
        var placa = NormalizarPlaca(dados.Placa);

        var erros = ValidarEditaveis(edicao, anoAtual);
        ValidarChassi(erros, chassi);
        ValidarPlaca(erros, placa);
        LancarSeInvalido(erros);

        return new Veiculo
        {
            Id = id,
            Marca = edicao.Marca,
            Modelo = edicao.Modelo,
            Ano = edicao.Ano,
            Cor = edicao.Cor,
            Preco = edicao.Preco,
            Chassi = chassi,
            Placa = placa,
            Status = StatusVeiculo.Disponivel,
            CriadoEm = criadoEm,
            AtualizadoEm = criadoEm
        };
    }

    public void Atualizar(DadosEdicaoVeiculo dados, int anoAtual, DateTimeOffset atualizadoEm)
    {
        if (Status == StatusVeiculo.Vendido)
            throw new VeiculoVendidoException(Id);

        var edicao = Normalizar(dados);
        LancarSeInvalido(ValidarEditaveis(edicao, anoAtual));

        Marca = edicao.Marca;
        Modelo = edicao.Modelo;
        Ano = edicao.Ano;
        Cor = edicao.Cor;
        Preco = edicao.Preco;
        AtualizadoEm = atualizadoEm;
    }

    public void MarcarComoVendido() => Status = StatusVeiculo.Vendido;

    [GeneratedRegex("^[A-HJ-NPR-Z0-9]{17}$", RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex RegexChassi();

    [GeneratedRegex("^[A-Z0-9]{7}$", RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex RegexPlaca();

    private static DadosEdicaoVeiculo Normalizar(DadosEdicaoVeiculo dados) => new(
        dados.Marca?.Trim() ?? string.Empty,
        dados.Modelo?.Trim() ?? string.Empty,
        dados.Ano,
        dados.Cor?.Trim() ?? string.Empty,
        dados.Preco);

    private static string NormalizarChassi(string? chassi) => chassi?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string? NormalizarPlaca(string? placa) =>
        string.IsNullOrWhiteSpace(placa) ? null : placa.Trim().ToUpperInvariant();

    private static Dictionary<string, List<string>> ValidarEditaveis(DadosEdicaoVeiculo dados, int anoAtual)
    {
        var erros = new Dictionary<string, List<string>>();

        ValidarTextoObrigatorio(erros, "marca", dados.Marca, TamanhoMaximoMarcaModelo, "Marca");
        ValidarTextoObrigatorio(erros, "modelo", dados.Modelo, TamanhoMaximoMarcaModelo, "Modelo");
        ValidarAno(erros, dados.Ano, anoAtual);
        ValidarTextoObrigatorio(erros, "cor", dados.Cor, TamanhoMaximoCor, "Cor");
        ValidarPreco(erros, dados.Preco);

        return erros;
    }

    private static void ValidarTextoObrigatorio(
        Dictionary<string, List<string>> erros, string campo, string valor, int tamanhoMaximo, string rotulo)
    {
        if (valor.Length is 0 || valor.Length > tamanhoMaximo)
            Adicionar(erros, campo, $"{rotulo} é obrigatória e deve ter até {tamanhoMaximo} caracteres.");
    }

    private static void ValidarAno(Dictionary<string, List<string>> erros, int ano, int anoAtual)
    {
        if (ano < AnoMinimo || ano > anoAtual + 1)
            Adicionar(erros, "ano", $"Ano deve estar entre {AnoMinimo} e {anoAtual + 1}.");
    }

    private static void ValidarPreco(Dictionary<string, List<string>> erros, decimal preco)
    {
        if (preco <= 0)
            Adicionar(erros, "preco", "Preço deve ser maior que zero.");
        else if (preco > PrecoMaximo)
            Adicionar(erros, "preco", $"Preço não pode ser maior que {PrecoMaximo:N2}.");
        else if (preco != decimal.Round(preco, 2))
            Adicionar(erros, "preco", "Preço deve ter no máximo duas casas decimais.");
    }

    private static void ValidarChassi(Dictionary<string, List<string>> erros, string chassi)
    {
        if (!RegexChassi().IsMatch(chassi))
            Adicionar(erros, "chassi", "Chassi deve ter 17 caracteres alfanuméricos, sem as letras I, O ou Q.");
    }

    private static void ValidarPlaca(Dictionary<string, List<string>> erros, string? placa)
    {
        if (placa is not null && !RegexPlaca().IsMatch(placa))
            Adicionar(erros, "placa", "Placa deve ter 7 caracteres alfanuméricos.");
    }

    private static void LancarSeInvalido(Dictionary<string, List<string>> erros)
    {
        if (erros.Count > 0)
            throw new ValidacaoVeiculoException(erros.ToDictionary(par => par.Key, par => par.Value.ToArray()));
    }

    private static void Adicionar(Dictionary<string, List<string>> erros, string campo, string mensagem)
    {
        if (!erros.TryGetValue(campo, out var lista))
            erros[campo] = lista = [];
        lista.Add(mensagem);
    }
}
