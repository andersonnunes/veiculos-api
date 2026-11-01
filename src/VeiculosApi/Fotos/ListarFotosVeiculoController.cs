using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeiculosApi.Application.Veiculos.Fotos;
using VeiculosApi.Seguranca;

namespace VeiculosApi.Fotos;

[ApiController]
[Route("veiculos/{veiculoId:guid}/fotos")]
[Authorize(Policy = PoliticasAutorizacao.OperacaoCatalogo)]
public sealed class ListarFotosVeiculoController(ListarFotosVeiculoCasoDeUso listarFotos) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<FotoVeiculoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Listar(Guid veiculoId, CancellationToken cancellationToken)
    {
        var fotos = await listarFotos.ExecutarAsync(veiculoId, cancellationToken);
        return fotos is null
            ? NotFound()
            : Ok(fotos.Select(FotoVeiculoResponse.De).ToList());
    }
}
