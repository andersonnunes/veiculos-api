using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Infrastructure.Keycloak;
using VeiculosApi.Infrastructure.Persistence;

namespace VeiculosApi.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuracao)
    {
        services.AddDbContext<VeiculosDbContext>(opcoes =>
            opcoes.UseNpgsql(configuracao.GetConnectionString("VeiculosDb")));

        services.AddScoped<IVeiculoRepositorio, EfVeiculoRepositorio>();

        services.AddHttpClient("keycloak-token");
        services.AddSingleton<TokenDeServico>();
        services.AddTransient<ServicoAutenticadoHandler>();

        return services;
    }
}
