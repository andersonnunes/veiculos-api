using System.Net.Http.Headers;

namespace VeiculosApi.Infrastructure.Keycloak;

public sealed class ServicoAutenticadoHandler(TokenDeServico tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokens.ObterAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
