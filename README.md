# veiculos-api

Serviço de cadastro e edição de veículos para uma plataforma de revenda de veículos automotores
(Tech Challenge Fase 4 — SOAT/PósTech).

**O que faz**: mantém o catálogo de veículos (marca, modelo, ano, cor, preço, chassi, placa e
fotos). Recebe o status final das vendas do [`vendas-api`](https://github.com/andersonnunes/vendas-api)
para bloquear edição de veículos já vendidos.

**O que não faz**: não lista veículos à venda nem efetua vendas. Essas responsabilidades ficam no
`vendas-api`, que tem banco de dados próprio.

## Como foi implementado

Organizado em camadas, com dependências apontando para dentro:

| Projeto | Responsabilidade |
|---|---|
| `VeiculosApi.Domain` | Entidades e invariantes do veículo. Sem dependências externas. |
| `VeiculosApi.Application` | Casos de uso e portas (`IVeiculoRepositorio`, entre outras). |
| `VeiculosApi.Infrastructure` | EF Core com PostgreSQL, migrações, armazenamento de fotos (MinIO), cliente HTTP para o `vendas-api` e cliente Keycloak. |
| `VeiculosApi` | API: controllers, autenticação JWT, `Program.cs` e `Dockerfile`. |

**Sincronização com o `vendas-api`**: a cada cadastro ou edição, o serviço envia uma cópia do veículo
para `PUT /internal/veiculos/{id}` no `vendas-api`, com token de serviço (`client_credentials`,
cliente `veiculos-api`). O `vendas-api` mantém essa cópia para as listagens. O envio não tem
nova tentativa: se falhar, o erro é registrado no log, mas a resposta ao cliente não muda (ver
Limitações conhecidas).

**Recebimento do status de venda**: o `vendas-api` chama `PUT /internal/veiculos/{id}/status` quando
uma venda é concluída. Depois disso o veículo fica `vendido` e não aceita mais edição (`409`).

**Autenticação**: JWT emitido pelo Keycloak. As rotas públicas exigem o papel `administrador` ou
`vendedor`. As rotas `/internal` exigem o papel `servico` e o cliente chamador `vendas-api`.

## Endpoints

| Método | Rota | Quem chama | Respostas |
|---|---|---|---|
| `POST` | `/veiculos` | administrador, vendedor | `201`, `400`, `401`, `403`, `409` |
| `GET` | `/veiculos/{id}` | administrador, vendedor | `200`, `401`, `403`, `404` |
| `PUT` | `/veiculos/{id}` | administrador, vendedor | `200`, `400`, `401`, `403`, `404`, `409` |
| `POST` | `/veiculos/{veiculoId}/fotos` | administrador, vendedor | `201`, `404` |
| `GET` | `/veiculos/{veiculoId}/fotos` | administrador, vendedor | `200`, `404` |
| `GET` | `/veiculos/{veiculoId}/fotos/{fotoId}/conteudo` | administrador, vendedor | `200`, `404` |
| `PUT` | `/internal/veiculos/{id}/status` | serviço `vendas-api` | uso interno entre serviços |

- `POST /veiculos` e `PUT /veiculos/{id}` recebem JSON com `marca`, `modelo`, `ano`, `cor` e `preco`.
  O cadastro também recebe `chassi` e `placa`. Chassi e placa não são editáveis.
- `409` no cadastro: chassi ou placa já cadastrados. `409` na edição: veículo vendido, ou alterado
  por outra operação entre a leitura e a gravação.
- `400` traz `ProblemDetails` com `errors` por campo.
- `POST /veiculos/{veiculoId}/fotos` recebe `multipart/form-data` com o arquivo no campo `arquivo`.
  As fotos são gravadas no MinIO (serviço `minio` do compose).

## Configuração

Valores de desenvolvimento, já presentes em `appsettings.Development.json`. Não são de produção.

| Chave | Padrão de dev | Para que serve |
|---|---|---|
| `ConnectionStrings:VeiculosDb` | `localhost:5433` | PostgreSQL deste serviço |
| `Keycloak:EmissorPublico` | `http://localhost:8180/realms/revendax` | Emissor aceito nos tokens |
| `Keycloak:EndpointToken` | `http://localhost:8180/.../token` | Obter token de serviço para chamar o `vendas-api` |
| `VendasApi:BaseUrl` | `http://localhost:5002` | Endereço do `vendas-api` |
| `Minio:Endpoint`, `Minio:Bucket` | `localhost:9000`, `veiculos-fotos` | Armazenamento das fotos |

## Como rodar localmente

### Com container (recomendado)

Sobe o serviço, o PostgreSQL, o Keycloak (com o realm `revendax` já importado) e o MinIO para as fotos:

```bash
docker compose -f infra/docker-compose.yml up --build
```

- Serviço: `http://localhost:5001`.
- PostgreSQL: `localhost:5433`. Keycloak: `http://localhost:8180`. MinIO: `localhost:9000`.

Verificar:

```bash
curl http://localhost:5001/health
# {"status":"healthy"}
```

Em `Development`, a documentação interativa (Scalar) fica em `http://localhost:5001/scalar`.

### Fora de container

Mantenha o PostgreSQL, o Keycloak e o MinIO do compose de pé (`up -d keycloak-db keycloak veiculos-db minio`).
Requer o SDK do .NET pinado em `global.json` (`10.0.401`). A string de conexão não tem padrão nos
arquivos de configuração, então precisa ser informada por variável de ambiente:

```bash
dotnet restore
ConnectionStrings__VeiculosDb='Host=localhost;Port=5433;Database=veiculos;Username=veiculos;Password=veiculos' \
  dotnet run --project src/VeiculosApi --urls http://localhost:5001
```

### Usuários e tokens de desenvolvimento

Credenciais de dev, não são de produção.

```bash
# Token de vendedor (usuário vendedor-dev / dev-vendedor)
curl -s -d grant_type=password -d client_id=revendax-dev-cli \
  -d username=vendedor-dev -d password=dev-vendedor \
  http://localhost:8180/realms/revendax/protocol/openid-connect/token
```

### Exemplo: cadastrar um veículo

```bash
TOKEN=$(curl -s -d grant_type=password -d client_id=revendax-dev-cli \
  -d username=vendedor-dev -d password=dev-vendedor \
  http://localhost:8180/realms/revendax/protocol/openid-connect/token | jq -r .access_token)

curl -i -X POST http://localhost:5001/veiculos \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"marca":"Fiat","modelo":"Argo","ano":2025,"cor":"Prata","preco":85000.00,"chassi":"9BWZZZ377VT004251","placa":"ABC1D23"}'
```

Resposta `201 Created`, com o veículo em `disponivel` e `Location` apontando para `GET /veiculos/{id}`.

Chassi e placa precisam ser inéditos. Repetir o exemplo com os mesmos valores responde `409`: troque
o chassi e a placa a cada execução.

### Exemplo: editar um veículo

```bash
curl -i -X PUT http://localhost:5001/veiculos/{id} \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"marca":"Fiat","modelo":"Argo","ano":2025,"cor":"Branco","preco":82000.00}'
```

## Rodando junto com o `vendas-api`

Os dois serviços usam **um único Keycloak**, o deste repositório (`localhost:8180`). O compose do
`vendas-api` já aponta para ele, então não é preciso configurar variáveis de ambiente.

1. Suba este serviço pelo compose deste repositório: `docker compose -f infra/docker-compose.yml up --build`.
2. Suba o `vendas-api` pelo compose dele: `docker compose -f ../vendas-api/infra/docker-compose.yml up --build`.

Com isso, um veículo cadastrado aqui aparece na listagem do `vendas-api`, e as chamadas entre os
serviços passam pela máquina (`host.docker.internal`). Esse fluxo foi verificado com os dois
composes de pé, com o roteiro de ponta a ponta passando em 21 de 21 verificações.

## Como testar

```bash
dotnet test
```

Os testes de integração sobem um Keycloak e um PostgreSQL reais via Testcontainers. Por isso é
preciso ter Docker disponível. Os contêineres são criados a cada execução.

Com cobertura, pelo mesmo gate usado no CI (mínimo 80% de linhas):

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
├── src/
│   ├── VeiculosApi.Domain/         # entidades e invariantes
│   ├── VeiculosApi.Application/    # casos de uso e portas
│   ├── VeiculosApi.Infrastructure/ # EF Core, migrações, MinIO, clientes HTTP e Keycloak
│   └── VeiculosApi/                # API: controllers, autenticação, Program.cs, Dockerfile
├── tests/VeiculosApi.Tests/        # testes de domínio, aplicação e integração (xUnit)
├── infra/docker-compose.yml        # serviço, PostgreSQL, Keycloak e MinIO
├── deploy/k8s/                     # manifests Kubernetes (kustomize)
├── deploy/kind/cluster.yaml        # cluster kind com o registry local
└── .github/workflows/ci.yml        # build, lint, testes com cobertura ≥80% e scan de imagem
```

## Limitações conhecidas

- **Sincronização com o `vendas-api` sem nova tentativa e sem fila.** Se o `vendas-api` estiver
  fora do ar, ou a chamada falhar, a alteração fica só aqui: o cadastro responde `201` mesmo assim.
  O erro aparece no log do serviço.
- **Depende do Keycloak deste serviço.** Os dois serviços validam tokens nele (`localhost:8180`),
  então ele precisa estar de pé para a comunicação entre eles funcionar.

## CI/CD

Todo Pull Request roda `dotnet restore`, `dotnet format --verify-no-changes`, `dotnet build`,
`dotnet test` com gate de cobertura de linha ≥80%, build da imagem Docker e scan de vulnerabilidades
com Trivy, que falha em qualquer HIGH ou CRITICAL. A branch `main` é protegida: só aceita merge com
o workflow de CI verde.

### Deploy no Kubernetes (kind)

Os manifests ficam em `deploy/k8s/` (namespace `veiculos`, API, banco próprio com `StatefulSet`,
`ConfigMap` e `Secret` de dev). O cluster de teste fica em `deploy/kind/cluster.yaml`.

Testado localmente: o `Deployment` sobe, o pod fica `Ready` e `GET /health` responde
`{"status":"healthy"}` dentro do cluster. Para reproduzir:

```bash
# 1. Registry local, ligado à rede do kind (sem `kind load`)
docker run -d --restart=always --name kind-registry -p 5000:5000 registry:2

# 2. Cluster kind com o containerd apontando para o registry
kind create cluster --name veiculos-cd --config deploy/kind/cluster.yaml
docker network connect kind kind-registry

# 3. Publicar a imagem no registry local
docker build -t localhost:5000/veiculos-api:dev -f src/VeiculosApi/Dockerfile .
docker push localhost:5000/veiculos-api:dev

# 4. Aplicar e esperar
kubectl apply -k deploy/k8s
kubectl -n veiculos rollout status deployment/veiculos-api --timeout=300s

# 5. Conferir
kubectl -n veiculos port-forward svc/veiculos-api 18080:8080 &
curl http://localhost:18080/health
```

**Ainda não automatizado:** o pipeline do GitHub Actions que publica a imagem no GHCR e faz o deploy
a cada merge em `main` não está escrito. Hoje o deploy é manual, como descrito acima. Os manifests
usam o Keycloak e o MinIO apenas como configuração: eles não sobem no cluster, então endpoints que
dependem de autenticação ou de fotos não funcionam ali.
