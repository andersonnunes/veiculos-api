using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Application.Veiculos.Fotos;
using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Erros;

internal sealed class ExcecoesDeNegocioHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        IResult? resultado = exception switch
        {
            ValidacaoVeiculoException validacao => Results.ValidationProblem(
                validacao.Erros.ToDictionary(par => par.Key, par => par.Value)),
            VeiculoDuplicadoException duplicado => Results.Problem(
                title: "Veículo já cadastrado",
                detail: $"Já existe um veículo com o mesmo {duplicado.Campo}.",
                statusCode: StatusCodes.Status409Conflict),
            VeiculoVendidoException => Results.Problem(
                title: "Veículo vendido",
                detail: "Veículo vendido não pode ser alterado.",
                statusCode: StatusCodes.Status409Conflict),
            LimiteFotosExcedidoException limite => Results.Problem(
                title: "Limite de fotos atingido",
                detail: $"Um veículo pode ter no máximo {limite.Limite} fotos.",
                statusCode: StatusCodes.Status422UnprocessableEntity),
            VeiculoConflitoConcorrenciaException => Results.Problem(
                title: "Conflito de alteração",
                detail: "O veículo foi alterado por outra operação. Recarregue e tente novamente.",
                statusCode: StatusCodes.Status409Conflict),
            _ => null
        };

        if (resultado is null)
            return false;

        await resultado.ExecuteAsync(httpContext);
        return true;
    }
}
