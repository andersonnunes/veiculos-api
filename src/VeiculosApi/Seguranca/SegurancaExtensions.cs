using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace VeiculosApi.Seguranca;

public static class SegurancaExtensions
{
    public static IServiceCollection AddSegurancaKeycloak(this IServiceCollection services, IConfiguration configuracao)
    {
        services.AddOptions<KeycloakOptions>()
            .Bind(configuracao.GetSection(KeycloakOptions.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigurarJwtBearer>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Validado na subida pelo ValidateOnStart acima; aqui só precisa do valor para montar a política.
        var clienteChamador = configuracao[$"{KeycloakOptions.Secao}:ClienteChamadorPermitido"] ?? string.Empty;

        services.AddAuthorizationBuilder()
            .AddPolicy(PoliticasAutorizacao.OperacaoCatalogo, politica =>
                politica.RequireRole("administrador", "vendedor"))
            .AddPolicy(PoliticasAutorizacao.ServicoVendasApi, politica =>
                politica.RequireRole("servico").RequireClaim("azp", clienteChamador));

        services.AddHttpClient("keycloak-token");
        services.AddSingleton<TokenDeServico>();
        services.AddTransient<ServicoAutenticadoHandler>();

        return services;
    }
}
