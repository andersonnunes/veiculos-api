using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Application.Veiculos.Fotos;
using VeiculosApi.Domain.Veiculos;

namespace VeiculosApi.Erros;

// `codigo` é o contrato estável que o cliente usa para decidir o tratamento. `title` e `detail`
// são texto para humanos e podem mudar sem quebrar ninguém.
internal sealed class ExcecoesDeNegocioHandler : IExceptionHandler
{
    private const string ChaveCodigo = "codigo";
    private const string ChaveCampo = "campo";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        IResult? resultado = exception switch
        {
            ValidacaoVeiculoException validacao => Results.ValidationProblem(
                validacao.Erros.ToDictionary(par => par.Key, par => par.Value)),
            VeiculoDuplicadoException duplicado => Results.Problem(
                title: "Veículo já cadastrado",
                detail: $"Já existe um veículo com {DescricaoDoCampo(duplicado.Campo)}.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?>
                {
                    [ChaveCodigo] = "veiculo_duplicado",
                    [ChaveCampo] = duplicado.Campo,
                }),
            VeiculoVendidoException => Results.Problem(
                title: "Veículo vendido",
                detail: "Veículo vendido não pode ser alterado.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { [ChaveCodigo] = "veiculo_vendido" }),
            LimiteFotosExcedidoException limite => Results.Problem(
                title: "Limite de fotos atingido",
                detail: $"Um veículo pode ter no máximo {limite.Limite} fotos.",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                extensions: new Dictionary<string, object?> { [ChaveCodigo] = "limite_fotos" }),
            VeiculoConflitoConcorrenciaException => Results.Problem(
                title: "Conflito de alteração",
                detail: "O veículo foi alterado por outra operação. Recarregue e tente novamente.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { [ChaveCodigo] = "conflito_alteracao" }),
            _ => null
        };

        if (resultado is null)
            return false;

        await resultado.ExecuteAsync(httpContext);
        return true;
    }

    // Concorda com o campo: "este chassi" e "esta placa". Um campo desconhecido cai numa forma neutra.
    private static string DescricaoDoCampo(string campo) => campo switch
    {
        "chassi" => "este chassi",
        "placa" => "esta placa",
        _ => "este dado",
    };
}
