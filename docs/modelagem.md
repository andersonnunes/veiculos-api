# Modelagem de dados — veiculos-api

## Responsabilidade

O `veiculos-api` é o dono do catálogo de veículos: cadastra, edita e guarda as fotos. Ele **não** lista
veículos à venda, não efetua vendas e não conhece pagamentos. Essas responsabilidades ficam no
[`vendas-api`](https://github.com/andersonnunes/vendas-api), que mantém a própria cópia dos veículos.

## Entidades

### `Veiculo` — tabela `veiculos`

| Campo | Tipo | Tamanho | Obrigatório | Regra |
|---|---|---|---|---|
| `Id` | guid | — | sim | Chave primária. |
| `Marca` | texto | 50 | sim | |
| `Modelo` | texto | 50 | sim | |
| `Ano` | inteiro | — | sim | Dentro da faixa aceita pelo domínio. |
| `Cor` | texto | 30 | sim | |
| `Preco` | decimal | 12,2 | sim | Maior que zero. |
| `Chassi` | texto | 17 | sim | Único. Sem as letras I, O e Q. Normalizado em maiúsculas. |
| `Placa` | texto | 7 | não | Única quando informada (índice parcial). Normalizada em maiúsculas. Vazia vira nula. |
| `Status` | texto | 20 | sim | `Disponivel` ou `Vendido` (ver enum abaixo). |
| `CriadoEm` | data e hora com fuso | — | sim | Definido na criação. |
| `AtualizadoEm` | data e hora com fuso | — | sim | Atualizado em cada edição e em cada mudança de status. |
| `StatusAtualizadoEm` | data e hora com fuso | — | não | Instante do último status recebido de `vendas-api`. Usado para descartar notificações antigas. |

### `FotoVeiculo` — tabela `fotos_veiculo`

| Campo | Tipo | Tamanho | Obrigatório | Regra |
|---|---|---|---|---|
| `Id` | guid | — | sim | Chave primária. |
| `VeiculoId` | guid | — | sim | Índice (`ix_fotos_veiculo_veiculo_id`). |
| `ContentType` | texto | 50 | sim | `image/jpeg`, `image/png` ou `image/webp`, conferido pelo conteúdo do arquivo. |
| `Tamanho` | inteiro | — | sim | Tamanho em bytes. |
| `ChaveArmazenamento` | texto | 200 | sim | Chave do objeto no MinIO. O conteúdo fica no MinIO, não no banco. |
| `CriadoEm` | data e hora com fuso | — | sim | |

## Enums

**`StatusVeiculo`** (persistido como texto)

| Valor | Significado |
|---|---|
| `Disponivel` | Cadastrado e editável. |
| `Vendido` | Venda concluída em `vendas-api`. Não aceita mais edição (resposta `409`). |

Transição: `Disponivel` → `Vendido`. Não há transição de volta. Uma notificação de "disponível" recebida
depois de `Vendido` é ignorada.

## Chaves, índices e concorrência

- **Chave primária**: `Id` em `veiculos` e `fotos_veiculo`, gerada pela aplicação.
- **Unicidade**: `ux_veiculos_chassi` (chassi, único) e `ux_veiculos_placa` (placa, única quando não nula).
  A duplicidade é garantida pelo banco. Duas criações simultâneas com o mesmo chassi resultam em uma
  `201` e uma `409`.
- **Concorrência otimista**: a coluna `xmin` do PostgreSQL é usada como token de versão. Duas edições
  simultâneas do mesmo veículo não se sobrescrevem em silêncio: a segunda que chega com versão antiga
  recebe `409`.

## Fronteira com a `vendas-api`

Este serviço **envia** dados para a `vendas-api` e **recebe** o status final de vendas.

| Direção | Rota | Conteúdo | Quando |
|---|---|---|---|
| `veiculos-api` → `vendas-api` | `PUT /internal/veiculos/{id}` | Snapshot completo: marca, modelo, ano, cor, preço, chassi, placa, status e `AtualizadoEm`. | Após cada cadastro e cada edição. |
| `vendas-api` → `veiculos-api` | `PUT /internal/veiculos/{id}/status` | `status: vendido` e `ocorridoEm`. | Quando a venda é concluída. |

- A `vendas-api` aplica o snapshot conforme `AtualizadoEm`: um snapshot antigo não sobrescreve um recente.
- Este serviço aplica o status de venda conforme `StatusAtualizadoEm`: uma notificação mais antiga que a
  última aplicada é descartada.
- Cada chamada leva token de serviço (`client_credentials`, cliente `veiculos-api`). A rota `/internal`
  aceita só o cliente `vendas-api`.

## Fluxos

### Cadastro e sincronização

```mermaid
sequenceDiagram
    participant C as Cliente (vendedor)
    participant V as veiculos-api
    participant B as PostgreSQL (veiculos)
    participant S as vendas-api

    C->>V: POST /veiculos (dados + chassi)
    V->>B: INSERT veiculos (Status = Disponivel)
    B-->>V: ok (ou 409 por chassi/placa repetidos)
    V-->>C: 201 Created
    V->>S: PUT /internal/veiculos/{id} (snapshot)
    Note over V,S: Falha aqui não muda a resposta ao cliente.<br/>O erro vai para o log; não há nova tentativa.
```

### Venda concluída

```mermaid
sequenceDiagram
    participant P as Processador de pagamento
    participant S as vendas-api
    participant V as veiculos-api

    P->>S: POST /webhooks/pagamentos (aprovado, assinado)
    S->>S: Venda Reservada → Concluida
    S->>V: PUT /internal/veiculos/{id}/status (vendido)
    V->>V: Status = Vendido, StatusAtualizadoEm = ocorridoEm
    V-->>S: 200 OK
    S-->>P: 200 (alterou = true)
    Note over S,V: Se a notificação falhar, a venda continua concluída.<br/>O erro vai para o log; não há nova tentativa.
```

## Falhas e consistência

- **Consistência eventual entre os serviços.** Não há transação distribuída. Uma mudança feita aqui pode
  demorar a aparecer na `vendas-api`, ou não aparecer, se a chamada falhar.
- **Sem outbox.** As chamadas são feitas diretamente, sem fila. Uma falha definitiva deixa a outra
  base desatualizada até a próxima mudança no mesmo veículo. Registrado como limitação conhecida.
- **Dados de dev em configuração.** As credenciais e os segredos deste documento são de desenvolvimento.
- **Garantias locais.** Dentro deste serviço, chassi e placa são únicos no banco, e a edição concorrente é
  detectada pelo `xmin`.
