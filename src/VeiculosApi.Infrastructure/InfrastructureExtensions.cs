using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Minio;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Application.Veiculos.Fotos;
using VeiculosApi.Infrastructure.Armazenamento;
using VeiculosApi.Infrastructure.Keycloak;
using VeiculosApi.Infrastructure.Persistence;
using VeiculosApi.Infrastructure.Sincronizacao;

namespace VeiculosApi.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuracao)
    {
        services.AddDbContext<VeiculosDbContext>(opcoes =>
            opcoes.UseNpgsql(configuracao.GetConnectionString("VeiculosDb")));

        services.AddScoped<IVeiculoRepositorio, EfVeiculoRepositorio>();
        services.AddScoped<IFotoVeiculoRepositorio, EfFotoVeiculoRepositorio>();

        services.AddOptions<ArmazenamentoFotosOptions>()
            .Bind(configuracao.GetSection(ArmazenamentoFotosOptions.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IMinioClient>(provedor =>
        {
            var opcoes = provedor.GetRequiredService<IOptions<ArmazenamentoFotosOptions>>().Value;
            return new MinioClient()
                .WithEndpoint(opcoes.Endpoint)
                .WithCredentials(opcoes.AccessKey, opcoes.SecretKey)
                .WithSSL(opcoes.Secure)
                .Build();
        });
        services.AddScoped<IArmazenamentoFotos, MinioArmazenamentoFotos>();

        services.AddHttpClient("keycloak-token");
        services.AddSingleton<TokenDeServico>();
        services.AddTransient<ServicoAutenticadoHandler>();

        services.AddOptions<VendasApiOptions>()
            .Bind(configuracao.GetSection(VendasApiOptions.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IVendasSincronizacao, VendasSincronizacaoHttp>((servicos, cliente) =>
                cliente.BaseAddress = new Uri(servicos.GetRequiredService<IOptions<VendasApiOptions>>().Value.BaseUrl))
            .AddHttpMessageHandler<ServicoAutenticadoHandler>()
            .AddStandardResilienceHandler(VendasSincronizacaoResiliencia.Configurar);

        return services;
    }
}
