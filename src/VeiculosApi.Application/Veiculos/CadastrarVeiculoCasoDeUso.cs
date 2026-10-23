using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Application.Veiculos;

public sealed class CadastrarVeiculoCasoDeUso(IVeiculoRepositorio repositorio, TimeProvider relogio)
{
    public async Task<VeiculoDto> ExecutarAsync(EntradaCadastrarVeiculo entrada, CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow();

        var dados = new DadosVeiculo(
            entrada.Marca,
            entrada.Modelo,
            entrada.Ano,
            entrada.Cor,
            entrada.Preco,
            entrada.Chassi,
            entrada.Placa);

        var veiculo = Veiculo.Cadastrar(Guid.CreateVersion7(agora), dados, agora.Year, agora);

        var campoDuplicado = await repositorio.CampoDuplicadoAsync(veiculo.Chassi, veiculo.Placa, cancellationToken);
        if (campoDuplicado is not null)
            throw new VeiculoDuplicadoException(campoDuplicado);

        await repositorio.AdicionarAsync(veiculo, cancellationToken);

        return VeiculoDto.De(veiculo);
    }
}
