using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Seguranca;

namespace VeiculosApi.Veiculos;

[ApiController]
[Route("veiculos")]
[Authorize(Policy = PoliticasAutorizacao.OperacaoCatalogo)]
public sealed class VeiculosController(
    CadastrarVeiculoCasoDeUso cadastrarVeiculo,
    ObterVeiculoPorIdCasoDeUso obterVeiculoPorId,
    AtualizarVeiculoCasoDeUso atualizarVeiculo) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<VeiculoResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarVeiculoRequest request, CancellationToken cancellationToken)
    {
        var veiculo = await cadastrarVeiculo.ExecutarAsync(request.ParaEntrada(), cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = veiculo.Id }, VeiculoResponse.De(veiculo));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<VeiculoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var veiculo = await obterVeiculoPorId.ExecutarAsync(id, cancellationToken);
        return veiculo is null ? NotFound() : Ok(VeiculoResponse.De(veiculo));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<VeiculoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarVeiculoRequest request, CancellationToken cancellationToken)
    {
        var veiculo = await atualizarVeiculo.ExecutarAsync(id, request.ParaEntrada(), cancellationToken);
        return veiculo is null ? NotFound() : Ok(VeiculoResponse.De(veiculo));
    }
}
