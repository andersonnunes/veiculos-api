using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeiculosApi.Seguranca;

namespace VeiculosApi.Tests.Seguranca;

[ApiController]
[Route("testes")]
public sealed class TestesSegurancaController : ControllerBase
{
    [HttpGet("operacao")]
    [Authorize(Policy = PoliticasAutorizacao.OperacaoCatalogo)]
    public IActionResult Operacao() => Ok();

    [HttpGet("interno")]
    [Authorize(Policy = PoliticasAutorizacao.ServicoVendasApi)]
    public IActionResult Interno() => Ok();
}
