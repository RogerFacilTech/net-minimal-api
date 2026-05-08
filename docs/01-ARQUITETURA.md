# Arquitetura do Projeto

## Visão Estrutural

O projeto hospeda três bounded contexts no mesmo repositório. Cada um segue um padrão arquitetural independente — eles não se comunicam diretamente entre si. O que compartilham é restrito a `src/Shared/`: o `AppDbContext`, a interface `IEndpoint`, o padrão `Result<T>` e o pipeline de middleware.

Essa coexistência é intencional: permite comparar abordagens diferentes aplicadas ao mesmo stack tecnológico, sem a complexidade de múltiplos repositórios ou serviços distribuídos.

---

## Catálogo — Clean Architecture Híbrida

O Catálogo usa Clean Architecture nas camadas internas e Vertical Slice na camada de API (um arquivo por grupo de recursos, em vez de controllers monolíticos).

### Sub-projetos

| Sub-projeto               | Responsabilidade                                               |
| ------------------------- | -------------------------------------------------------------- |
| `Catalogo.Domain`         | Entidades, value objects, interfaces de repositório           |
| `Catalogo.Application`    | Serviços, DTOs, validadores FluentValidation                  |
| `Catalogo.Infrastructure` | Repositórios EF Core, DbSeeder, configurações de mapeamento |
| `Catalogo.API`            | Endpoints Minimal API, políticas de rate limiting             |

### Domain

Contém 5 entidades: **Produto**, **Categoria**, **Variante**, **Atributo** e **Mídia**.

- **Produto**, **Categoria** e **Variante** têm domínio rico — lógica de negócio encapsulada nos próprios agregados (ex.: `Produto.Desativar()`, `Categoria` com slug gerado e hierarquia pai/filho, `Variante` com value object `SKU`).
- **Atributo** e **Mídia** são CRUD simples — entidades anêmicas com persistência direta via repositório.

### Application

Serviços de aplicação orquestram casos de uso: recebem DTOs, invocam o domínio e delegam persistência ao repositório. Validadores FluentValidation ficam nesta camada, junto com os DTOs de entrada e saída.

### Infrastructure

Repositórios concretos implementam as interfaces definidas no Domain. O `DbSeeder` popula o banco com dados iniciais (IDs 1–5 reservados para categorias, IDs 1–8 para produtos).

### API

Cada grupo de recursos tem seu próprio arquivo de endpoints (`ProdutoEndpoints.cs`, `CategoriaEndpoints.cs`, etc.). Rate limiting é aplicado por política de rota — três políticas registradas: `leitura`, `escrita` e `criacao-produto`.

### Fluxo de dados

```
HTTP → Catalogo.API/Endpoints → Catalogo.Application/Services → Catalogo.Infrastructure/Repositories → AppDbContext
                              ↓
                     Catalogo.Domain (entities, value objects)
```

---

## Pedidos — Vertical Slice + Domínio Rico

O bounded context de Pedidos organiza o código por caso de uso, não por camada técnica. Cada operação é uma pasta autocontida dentro de `src/Pedidos/Features/`.

### Estrutura de uma feature

```
Features/
  CreatePedido/
    CreatePedidoCommand.cs     ← DTO de entrada
    CreatePedidoValidator.cs   ← FluentValidation
    CreatePedidoHandler.cs     ← orquestração do caso de uso
    CreatePedidoEndpoint.cs    ← implementa IEndpoint, auto-descoberto
```

Cada pasta contém tudo o que aquela operação precisa — nenhuma dependência cruzada entre features.

### Domínio Rico

O aggregate `Pedido` encapsula as regras de negócio. Métodos como `Pedido.Create()`, `Pedido.AddItem()` e `Pedido.Cancel()` nunca lançam exceções — retornam `Result` ou `Result<T>`. O handler sempre verifica `IsSuccess` antes de acessar `.Value`.

### Auto-descoberta de endpoints

Todos os endpoints implementam a interface `IEndpoint` (`src/Shared/Common/IEndpoint.cs`). O `Program.cs` usa reflection para descobrir e registrar automaticamente todas as implementações — nenhum endpoint precisa ser cadastrado manualmente.

### Fluxo de dados

```
HTTP → CreatePedidoEndpoint (IEndpoint, auto-discovered) → CreatePedidoValidator → CreatePedidoHandler → Pedido.Create() → AppDbContext
```

---

## Pix — Integração Externa

### Pix.MockServer

Minimal API autocontida que simula a API Pix do Banco Central do Brasil. Implementa OAuth2 para emissão de tokens e mTLS para autenticação mútua. Persiste dados em memória (sem banco de dados). Seu propósito é permitir desenvolvimento e testes da integração sem dependência de ambiente externo.

Localização: `samples/Pix/Pix.MockServer/`

### Pix.ClientDemo

Console app que consome o `Pix.MockServer` via `HttpClient` tipado. Demonstra:

- Configuração de mTLS com certificado de cliente
- Fluxo OAuth2 (client credentials)
- Pipeline de resiliência com `AddStandardResilienceHandler` (retry + circuit breaker via `Microsoft.Extensions.Http.Resilience`)

Localização: `samples/Pix/Pix.ClientDemo/`

### Catalogo.ClientDemo

Console app que demonstra retry e circuit breaker consumindo a API do Catálogo. Usa `Microsoft.Extensions.Http.Resilience` diretamente, sem mTLS, como exemplo mais acessível do padrão de resiliência para APIs internas.

---

## Middleware e Shared

Componentes em `src/Shared/` utilizados por todos os bounded contexts:

| Componente                | Descrição                                                                                                                       |
| ------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `IdempotencyMiddleware`   | Intercepta POST/PUT/PATCH com header `Idempotency-Key`. Evita reprocessamento de requisições duplicadas.                        |
| Global exception handling | Captura exceções não tratadas e retorna respostas padronizadas (RFC 7807 Problem Details).                                     |
| JWT Bearer auth           | Configurado globalmente. Pedidos exigem `RequireAuthorization()`. GET do Catálogo é anônimo; escrita exige token.              |
| Rate limiting             | 3 políticas: `leitura`, `escrita`, `criacao-produto`. Registradas no `Program.cs`. Desativadas quando `Environment = "Testing"`. |
| `AppDbContext`            | Único contexto EF Core, compartilhado pelos três bounded contexts.                                                              |
| `IEndpoint`               | Interface com método `Map(IEndpointRouteBuilder)`. Implementações são descobertas por reflection.                             |
| `Result<T>`               | Tipo discriminado que representa sucesso ou falha sem lançar exceções. Usado exclusivamente no bounded context de Pedidos.     |

---

## Decisões Arquiteturais

As decisões arquiteturais estão registradas em 15 ADRs no formato MADR 3.x em `docs/ADRs/`. ADR-0011 a ADR-0015 documentam as decisões das Fases 1 a 3 (migração do Catálogo para Clean Architecture, novos recursos, rate limiting e estrutura de documentação).

---

## Estrutura de Diretórios

```
net-minimal-api/
├── src/
│   ├── Catalogo/                           # Bounded Context 1 — Clean Architecture híbrida
│   │   ├── Catalogo.Domain/                # Entidades, value objects, interfaces de repositório
│   │   ├── Catalogo.Application/           # Serviços, DTOs, validators, interfaces
│   │   ├── Catalogo.Infrastructure/        # Repositórios EF Core, DbSeeder
│   │   ├── Catalogo.API/                   # Endpoints Minimal API, extensões, rate limiting
│   │   └── Catalogo.ClientDemo/            # Console app com pipeline de resiliência
│   │
│   ├── Pedidos/                            # Bounded Context 2 — Vertical Slice + Domínio Rico
│   │   ├── Domain/                         # Aggregate Pedido, PedidoItem, StatusPedido
│   │   ├── Features/
│   │   │   ├── CreatePedido/               # Command, Validator, Handler, Endpoint
│   │   │   ├── GetPedido/
│   │   │   ├── ListPedidos/
│   │   │   ├── AddItemPedido/
│   │   │   └── CancelPedido/
│   │   └── Common/                         # DTOs e tipos compartilhados entre slices
│   │
│   └── Shared/                             # Compartilhado por todos os bounded contexts
│       ├── Common/                         # IEndpoint, Result<T>, EndpointExtensions
│       ├── Data/                           # AppDbContext + Migrations + DbSeeder
│       └── Middleware/                     # ExceptionHandling, IdempotencyMiddleware
│
├── samples/
│   └── Pix/                                # Trilha de integração externa (educacional)
│       ├── Pix.MockServer/                 # Minimal API simulando BCB Pix
│       │   ├── Contracts/                  # Requests e responses complexos
│       │   ├── Application/                # Regras de negócio e validações
│       │   ├── Infrastructure/InMemory/    # Repositórios thread-safe
│       │   └── Security/                   # Bearer + mTLS real
│       ├── Pix.ClientDemo/                 # Console app — HttpClient tipado + resiliência
│       │   ├── Client/Handlers/            # CorrelationId, IdempotencyKey, Logging
│       │   └── Scenarios/                  # Fluxo fim-a-fim didático
│       └── Pix.MockServer.Tests/           # Testes de integração HTTP da trilha PIX
│
└── (sem pasta tests/ consolidada)
    ├── src/Catalogo/Catalogo.Tests/         # Testes do Catálogo
    ├── src/Pedidos/Pedidos.Tests/           # Testes de Pedidos
    └── samples/Pix/Pix.MockServer.Tests/    # Testes de integração HTTP da trilha PIX
```

---

## Fluxos de Requisição

### Catálogo — Clean Architecture Híbrida

```
POST /api/v1/catalogo/produtos
    │
    ├─ RateLimiter  ← política "criacao-produto" (TokenBucket, 5/min)
    │                  retorna 429 + Retry-After se excedido
    │
    ├─ IdempotencyMiddleware  ← verifica header Idempotency-Key
    │                           devolve resposta cacheada se chave já vista
    │
    ├─ RequireAuthorization()  ← valida JWT Bearer
    │                             retorna 401 se ausente, 403 se sem permissão
    │
    ├─ CriarProduto (handler local em ProdutoEndpoints.cs)
    │   ├─ IValidator<CriarProdutoRequest>.ValidateAsync()  ← FluentValidation
    │   │   └─ retorna 422 Unprocessable Entity se inválido
    │   │
    │   └─ IProdutoService.CriarProdutoAsync(request)
    │       ├─ Produto.Criar(...)  ← domínio rico, retorna Result<Produto>
    │       │   └─ retorna 422 se invariante violada (ex.: preço ≤ 0)
    │       ├─ IProdutoCommandRepository.AdicionarAsync(produto)
    │       └─ AppDbContext.SaveChangesAsync()
    │
    └─ 201 Created + ProdutoResponse
```

### Pedidos — Vertical Slice + Domínio Rico

```
POST /api/v1/pedidos
    │
    ├─ RequireAuthorization()  ← JWT obrigatório
    │
    ├─ CreatePedidoEndpoint.Handle(command, handler)   ← IEndpoint, auto-descoberto
    │   │
    │   └─ CreatePedidoHandler.HandleAsync(command)
    │       ├─ IValidator<CreatePedidoCommand>.ValidateAsync()
    │       │   └─ retorna 400 se inválido
    │       │
    │       ├─ Pedido.Create(clienteNome)  ← retorna Result<Pedido>
    │       │   └─ retorna 400 se nome inválido
    │       │
    │       ├─ foreach item: pedido.AddItem(produto, quantidade)
    │       │   └─ retorna 400 se estoque insuficiente ou pedido não está Aberto
    │       │
    │       ├─ AppDbContext.Pedidos.Add(pedido)
    │       └─ AppDbContext.SaveChangesAsync()
    │
    └─ 201 Created + { id }
```

### PIX — Mock Server + Cliente HTTP

```
Pix.ClientDemo                          Pix.MockServer
     │                                       │
     ├─ POST /oauth/token ──────────────────►│
     │   (client_id + client_secret)         │ valida credenciais
     │◄─ 200 { access_token } ──────────────┤
     │                                       │
     ├─ POST /pix/v1/cobrancas ────────────►│
     │   Authorization: Bearer {token}       │ valida Bearer
     │   Idempotency-Key: {uuid}            │ verifica chave
     │   X-Correlation-Id: {uuid}           │ logar para rastreio
     │◄─ 201 { txid, status: "ATIVA" } ────┤
     │                                       │
     ├─ POST /pix/v1/cobrancas/{txid}       │
     │        /simular-liquidacao ─────────►│ atualiza status para CONCLUIDA
     │◄─ 200 OK ────────────────────────────┤
     │                                       │
     ├─ GET /pix/v1/cobrancas/{txid} ──────►│
     │◄─ 200 { status: "CONCLUIDA" } ───────┤
```

---

## Comparativo: Clean Architecture vs Vertical Slice

| Dimensão                             | Catálogo (Clean Architecture)                                             | Pedidos (Vertical Slice)                                         |
| ------------------------------------- | -------------------------------------------------------------------------- | ---------------------------------------------------------------- |
| **Organização do código**          | Por camada técnica (Domain, Application, Infrastructure, API)             | Por caso de uso (CreatePedido, GetPedido, etc.)                  |
| **Localização de um novo endpoint** | 4 sub-projetos diferentes                                                  | Uma pasta isolada                                                |
| **Coesão**                           | Baixa — lógica de um recurso dispersa entre camadas                     | Alta — tudo para um caso de uso na mesma pasta                 |
| **Acoplamento entre features**        | Alto via serviços compartilhados                                          | Baixo — slices independentes                                   |
| **Modelo de domínio**                | Híbrido (rico em Produto/Categoria/Variante, anêmico em Atributo/Mídia) | Rico (aggregate Pedido com invariantes encapsuladas)             |
| **Tratamento de erro**                | Exceção + middleware global                                              | Result pattern — sem exceptions para erros de negócio         |
| **Quando adicionar campo**            | Toca Domain, Application (DTO + Validator + Service), Infrastructure, API  | Toca Domain + slice específica                                  |
| **Teste unitário**                   | Testa serviço via mock de repositório                                    | Testa aggregate direto sem dependência de infraestrutura        |
| **Escalabilidade**                    | Boa até ~50 endpoints por recurso                                         | Excelente — cada feature cresce isolada                        |
| **Overhead inicial**                  | Alto (4 projetos, interfaces, repositórios)                               | Baixo (uma pasta por feature)                                    |
| **Indicado para**                     | Times grandes, domínio rico mas previsível, CRUD com regras              | Domínio complexo com muitas invariantes, features independentes |

### Qual escolher no mundo real?

Não são mutuamente exclusivos. Este projeto demonstra os dois coexistindo no mesmo `AppDbContext`:

- Use **Clean Architecture** para recursos com muitas variações de query (paginação, filtros), onde repositórios abstratos e DTOs separados pagam seu custo.
- Use **Vertical Slice** para operações com lógica de negócio densa, onde cada caso de uso tem regras distintas e evolui de forma independente.
- Para CRUD puro sem lógica de negócio (ex.: `Atributo`, `Mídia` no Catálogo), ambas chegam ao mesmo resultado — escolha pelo que o time já conhece.
