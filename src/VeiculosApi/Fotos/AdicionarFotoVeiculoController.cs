using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeiculosApi.Application.Veiculos.Fotos;
using VeiculosApi.Domain.Veiculos;
using VeiculosApi.Seguranca;

namespace VeiculosApi.Fotos;

[ApiController]
[Route("veiculos/{veiculoId:guid}/fotos")]
[Authorize(Policy = PoliticasAutorizacao.OperacaoCatalogo)]
public sealed class AdicionarFotoVeiculoController(AdicionarFotoVeiculoCasoDeUso adicionarFoto) : ControllerBase
{
    // Folga acima do limite da foto para o envelope multipart; o tamanho real é validado no domínio.
    private const long LimiteCorpoBytes = FotoVeiculo.TamanhoMaximoBytes + 64 * 1024;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteCorpoBytes)]
    [ProducesResponseType<FotoVeiculoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Adicionar(Guid veiculoId, IFormFile? arquivo, CancellationToken cancellationToken)
    {
        if (arquivo is null)
        {
            ModelState.AddModelError("arquivo", "Envie a imagem no campo 'arquivo'.");
            return ValidationProblem(ModelState);
        }

        using var fluxo = new MemoryStream();
        await using (var origem = arquivo.OpenReadStream())
            await origem.CopyToAsync(fluxo, cancellationToken);

        var foto = await adicionarFoto.ExecutarAsync(veiculoId, fluxo.ToArray(), cancellationToken);
        if (foto is null)
            return NotFound();

        return CreatedAtRoute(
            ObterConteudoFotoVeiculoController.NomeRotaConteudo,
            new { veiculoId, fotoId = foto.Id },
            FotoVeiculoResponse.De(foto));
    }
}
