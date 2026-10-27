using Microsoft.Extensions.Http.Resilience;

namespace VeiculosApi.Infrastructure.Sincronizacao;

internal static class VendasSincronizacaoResiliencia
{
    internal static void Configurar(HttpStandardResilienceOptions opcoes)
    {
        opcoes.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
        opcoes.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(12);
        opcoes.Retry.MaxRetryAttempts = 2;
    }
}
