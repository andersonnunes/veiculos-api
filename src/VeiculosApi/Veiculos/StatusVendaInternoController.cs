using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Domain.Veiculos;
using VeiculosApi.Seguranca;

namespace VeiculosApi.Veiculos;

/// <summary>Endpoint interno: só o cliente vendas-api (papel servico) pode chamar.</summary>
[ApiController]
[Route("internal/veiculos/{id:guid}/status")]
[Authorize(Policy = PoliticasAutorizacao.ServicoVendasApi)]
public sealed class StatusVendaInternoController(AplicarStatusVendaCasoDeUso aplicarStatusVenda) : ControllerBase
{
    private static readonly Dictionary<string, StatusVeiculo> StatusAceitos = new()
    {
        ["vendido"] = StatusVeiculo.Vendido,
        ["disponivel"] = StatusVeiculo.Disponivel,
    };

    [HttpPut]
    [ProducesResponseType<VeiculoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Aplicar(Guid id, [FromBody] AplicarStatusVendaRequest request, CancellationToken cancellationToken)
    {
        if (!StatusAceitos.TryGetValue(request.Status ?? string.Empty, out var status))
        {
            ModelState.AddModelError(nameof(request.Status), "Status deve ser 'vendido' ou 'disponivel'.");
            return ValidationProblem(ModelState);
        }
        if (request.OcorridoEm is not { } ocorridoEm)
        {
            ModelState.AddModelError(nameof(request.OcorridoEm), "Informe o instante da venda em 'ocorridoEm'.");
            return ValidationProblem(ModelState);
        }

        var veiculo = await aplicarStatusVenda.ExecutarAsync(id, status, ocorridoEm, cancellationToken);
        return veiculo is null ? NotFound() : Ok(VeiculoResponse.De(veiculo));
    }
}

public sealed record AplicarStatusVendaRequest(string? Status, DateTimeOffset? OcorridoEm);
