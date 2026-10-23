# veiculos-api

Serviço de cadastro e edição de veículos para uma plataforma de revenda de veículos
automotores (Tech Challenge Fase 4 — SOAT/PósTech). Repositório físico independente,
responsável pela gestão do catálogo (marca, modelo, ano, cor, preço) — não lida com
listagem nem venda, que são responsabilidade isolada do repositório irmão
[`vendas-api`](https://github.com/andersonnunes/vendas-api), com banco de dados próprio.

Os dois serviços se comunicam apenas via HTTP (sem acesso direto a banco um do outro): este
serviço notifica o `vendas-api` sempre que um veículo é cadastrado/editado, e recebe de volta
o status final de uma venda, para bloquear edição de veículos já vendidos.

## Stack

- .NET 10 (ASP.NET Core Web API, controllers)
- PostgreSQL (banco próprio deste serviço, isolado do `vendas-api`)
- xUnit + `WebApplicationFactory` para testes de integração
- Docker + GitHub Actions (CI: build, lint, testes com gate de cobertura ≥80%, scan de
  vulnerabilidades com Trivy)

## Como rodar localmente

```bash
docker compose -f infra/docker-compose.yml up --build
```

Sobe o serviço (`http://localhost:5001`) e seu Postgres (`localhost:5433`). Verificar:

```bash
curl http://localhost:5001/health
# {"status":"healthy"}
```

Em `Development`, a documentação interativa (Scalar) fica em `http://localhost:5001/scalar`.

### Rodando fora de container

Requer o SDK do .NET pinado em `global.json` (`10.0.401`):

```bash
dotnet restore
dotnet run --project src/VeiculosApi
```

### Autenticação (Keycloak de desenvolvimento)

O compose sobe um Keycloak com o realm `revendax` já importado (`infra/keycloak/realm-revendax.json`).
Credenciais de dev, não são de produção:

```bash
# Token de vendedor (usuário vendedor-dev / dev-vendedor)
curl -s -d grant_type=password -d client_id=revendax-dev-cli \
  -d username=vendedor-dev -d password=dev-vendedor \
  http://localhost:8180/realms/revendax/protocol/openid-connect/token

# Token de serviço (client_credentials) — o que outra API usaria para chamar esta
curl -s -d grant_type=client_credentials -d client_id=vendas-api \
  -d client_secret=dev-vendas-api-secret \
  http://localhost:8180/realms/revendax/protocol/openid-connect/token
```

## Como testar

```bash
dotnet test
```

Com cobertura (mesmo gate usado no CI, mínimo 80% de linhas):

```bash
dotnet test tests/VeiculosApi.Tests/VeiculosApi.Tests.csproj -c Release \
  /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura \
  /p:ExcludeByFile="**/obj/**/*.cs" \
  /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
```

## Estrutura

```
veiculos-api/
├── global.json              # pina o SDK do .NET
├── veiculos-api.slnx
├── src/VeiculosApi/         # API (controllers, Program.cs, Dockerfile)
├── tests/VeiculosApi.Tests/ # testes xUnit + WebApplicationFactory
├── infra/docker-compose.yml # serviço + Postgres próprio
├── docs/                    # modelagem de dados / arquitetura
└── .github/workflows/ci.yml # build + lint + testes (cobertura ≥80%) + scan de imagem
```

## CI/CD

Todo Pull Request roda `dotnet restore` + `dotnet format --verify-no-changes` + `dotnet build`
+ `dotnet test` (com gate de cobertura de linha ≥80%) + build da imagem Docker + scan de
vulnerabilidades (Trivy, falha em qualquer HIGH/CRITICAL). A branch `main` é protegida: só
aceita merge com o workflow de CI verde.

Deploy automatizado disparado por merge em `main` — detalhado quando a história de deploy
deste serviço for implementada.
