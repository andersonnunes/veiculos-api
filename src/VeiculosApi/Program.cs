using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using VeiculosApi.Application.Veiculos;
using VeiculosApi.Erros;
using VeiculosApi.Infrastructure;
using VeiculosApi.Infrastructure.Persistence;
using VeiculosApi.Seguranca;

[assembly: InternalsVisibleTo("VeiculosApi.Tests")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ExcecoesDeNegocioHandler>();
builder.Services.AddSegurancaKeycloak(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CadastrarVeiculoCasoDeUso>();
builder.Services.AddScoped<ObterVeiculoPorIdCasoDeUso>();
builder.Services.AddScoped<AtualizarVeiculoCasoDeUso>();

var app = builder.Build();

using (var escopo = app.Services.CreateScope())
    await escopo.ServiceProvider.GetRequiredService<VeiculosDbContext>().Database.MigrateAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

await app.RunAsync();
