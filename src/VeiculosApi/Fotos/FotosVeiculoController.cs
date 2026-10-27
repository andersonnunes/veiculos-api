using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using VeiculosApi.Application.Veiculos.Fotos;
using VeiculosApi.Domain.Veiculos;
using VeiculosApi.Seguranca;

namespace VeiculosApi.Fotos;

[ApiController]
[Route("veiculos/{veiculoId:guid}/fotos")]
[Authorize(Policy = PoliticasAutorizacao.OperacaoCatalogo)]
public sealed class FotosVeiculoController(
    AdicionarFotoVeiculoCasoDeUso adicionarFoto,
    ListarFotosVeiculoCasoDeUso listarFotos,
    ObterConteudoFotoCasoDeUso obterConteudo) : ControllerBase
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

        var resposta = FotoVeiculoResponse.De(foto);
        return CreatedAtAction(nameof(ObterConteudo), new { veiculoId, fotoId = foto.Id }, resposta);
    }

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

    [HttpGet("{fotoId:guid}/conteudo", Name = nameof(ObterConteudo))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterConteudo(Guid veiculoId, Guid fotoId, CancellationToken cancellationToken)
    {
        var conteudo = await obterConteudo.ExecutarAsync(veiculoId, fotoId, cancellationToken);
        return conteudo is null
            ? NotFound()
            : File(conteudo.Bytes, conteudo.ContentType);
    }
}
