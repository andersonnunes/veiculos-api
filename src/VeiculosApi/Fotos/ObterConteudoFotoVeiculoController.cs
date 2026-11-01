using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeiculosApi.Application.Veiculos.Fotos;
using VeiculosApi.Seguranca;

namespace VeiculosApi.Fotos;

[ApiController]
[Route("veiculos/{veiculoId:guid}/fotos")]
[Authorize(Policy = PoliticasAutorizacao.OperacaoCatalogo)]
public sealed class ObterConteudoFotoVeiculoController(ObterConteudoFotoCasoDeUso obterConteudo) : ControllerBase
{
    public const string NomeRotaConteudo = "ObterConteudoFotoVeiculo";

    [HttpGet("{fotoId:guid}/conteudo", Name = NomeRotaConteudo)]
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
