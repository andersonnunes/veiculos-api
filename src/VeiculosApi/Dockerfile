# Build no contexto raiz do repositório: docker build -f src/VeiculosApi/Dockerfile .
#
# Tags do .NET pinadas em versão exata (não "10.0" flutuante) — a tag flutuante pode apontar
# para um runtime com CVE conhecida sem aviso; pinar e escanear com Trivy a cada build detecta
# isso antes do deploy, em vez de descobrir depois.
FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src

COPY ["src/VeiculosApi/VeiculosApi.csproj", "src/VeiculosApi/"]
COPY ["src/VeiculosApi.Application/VeiculosApi.Application.csproj", "src/VeiculosApi.Application/"]
COPY ["src/VeiculosApi.Domain/VeiculosApi.Domain.csproj", "src/VeiculosApi.Domain/"]
COPY ["src/VeiculosApi.Infrastructure/VeiculosApi.Infrastructure.csproj", "src/VeiculosApi.Infrastructure/"]
RUN dotnet restore "src/VeiculosApi/VeiculosApi.csproj"

COPY src/. src/
WORKDIR /src/src/VeiculosApi
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13 AS final
WORKDIR /app
COPY --from=build /app/publish .

# A imagem base já expõe ASPNETCORE_HTTP_PORTS=8080 por padrão (.NET 8+).
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "VeiculosApi.dll"]
