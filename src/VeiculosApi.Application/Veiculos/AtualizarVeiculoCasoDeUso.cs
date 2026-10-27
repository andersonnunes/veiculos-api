using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Application.Veiculos;

public sealed class AtualizarVeiculoCasoDeUso(
    IVeiculoRepositorio repositorio,
    IVendasSincronizacao sincronizacao,
    TimeProvider relogio)
{
    public async Task<VeiculoDto?> ExecutarAsync(Guid id, EntradaAtualizarVeiculo entrada, CancellationToken cancellationToken)
    {
        var veiculo = await repositorio.ObterParaAlteracaoAsync(id, cancellationToken);
        if (veiculo is null)
            return null;

        var agora = relogio.GetUtcNow();
        veiculo.Atualizar(
            new DadosEdicaoVeiculo(entrada.Marca, entrada.Modelo, entrada.Ano, entrada.Cor, entrada.Preco),
            agora.Year,
            agora);

        await repositorio.SalvarAlteracoesAsync(cancellationToken);

        var dto = VeiculoDto.De(veiculo);
        await sincronizacao.SincronizarAsync(dto, cancellationToken);

        return dto;
    }
}
