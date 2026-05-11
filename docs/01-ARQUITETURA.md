# Arquitetura do Projeto

## Visão Estrutural

O projeto hospeda quatro bounded contexts no mesmo repositório. Cada um segue um padrão arquitetural independente — eles não se comunicam diretamente entre si. O que compartilham é restrito a `src/Shared/`: a interface `IEndpoint`, o padrão `Result<T>` e utilitários HTTP.

Essa coexistência é intencional: permite comparar abordagens diferentes aplicadas ao mesmo stack tecnológico, sem a complexidade de múltiplos repositórios ou serviços distribuídos.

---

## Catálogo — Clean Architecture Híbrida

O Catálogo usa Clean Architecture nas camadas internas e Vertical Slice na camada de API (um arquivo por grupo de recursos, em vez de controllers monolíticos).

### Sub-projetos

| Sub-projeto               | Responsabilidade                                          |
| ------------------------- | --------------------------------------------------------- |
| `Catalogo.Domain`         | Entidades, value objects, interfaces de repositório       |
| `Catalogo.Application`    | Serviços, DTOs, validadores FluentValidation              |
| `Catalogo.Infrastructure` | Repositórios EF Core, DbContext, DbSeeder, migrations     |
| `Catalogo.API`            | Entry point da API, endpoints, DI, middleware, Program.cs |

### Domain

Contém 5 entidades: **Produto**, **Categoria**, **Variante**, **Atributo** e **Mídia**.

- **Produto**, **Categoria** e **Variante** têm domínio rico — lógica de negócio encapsulada nos próprios agregados (ex.: `Produto.Desativar()`, `Categoria` com slug gerado e hierarquia pai/filho, `Variante` com value object `SKU`).
- **Atributo** e **Mídia** são CRUD simples — entidades anêmicas com persistência direta via repositório.

### Application

Serviços de aplicação orquestram casos de uso: recebem DTOs, invocam o domínio e delegam persistência ao repositório. Validadores FluentValidation ficam nesta camada, junto com os DTOs de entrada e saída.

### Infrastructure

Repositórios concretos implementam as interfaces definidas no Domain. O `DbSeeder` popula o banco com dados iniciais (IDs 1–5 reservados para categorias, IDs 1–8 para produtos).

### Endpoints

Cada grupo de recursos tem seu próprio arquivo de endpoints (`ProdutoEndpoints.cs`, `CategoriaEndpoints.cs`, etc.) em `Catalogo.API/Endpoints/`. Rate limiting é aplicado por política de rota — três políticas registradas: `leitura`, `escrita` e `criacao-produto`.

### Fluxo de dados

```
HTTP → Catalogo.API/Endpoints → Catalogo.Application/Services → Catalogo.Infrastructure/Repositories → CatalogoDbContext
                                    ↓
                           Catalogo.Domain (entities, value objects)
```

---

## Pedidos — Vertical Slice + Domínio Rico

O bounded context de Pedidos organiza o código por caso de uso, não por camada técnica. Cada operação é uma pasta autocontida dentro de `src/Pedidos/Pedidos.API/`.

### Estrutura de uma feature

```
Pedidos.API/
  CreatePedido/
    CreatePedidoCommand.cs     ← DTO de entrada + Handler (orquestração)
    CreatePedidoValidator.cs   ← FluentValidation
    CreatePedidoEndpoint.cs    ← implementa IEndpoint, auto-descoberto
```

Cada pasta contém tudo o que aquela operação precisa — nenhuma dependência cruzada entre features.

### Domínio Rico

O aggregate `Pedido` encapsula as regras de negócio. Métodos como `Pedido.Create()`, `Pedido.AddItem()` e `Pedido.Cancel()` nunca lançam exceções — retornam `Result` ou `Result<T>`. O handler sempre verifica `IsSuccess` antes de acessar `.Value`.

### Auto-descoberta de endpoints

Todos os endpoints implementam a interface `IEndpoint` (`src/Shared/Web/IEndpoint.cs`). O `Program.cs` usa reflection para descobrir e registrar automaticamente todas as implementações — nenhum endpoint precisa ser cadastrado manualmente.

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

### Catalogo.HttpClientDemo

Console app que demonstra retry e circuit breaker consumindo a API do Catálogo. Usa `Microsoft.Extensions.Http.Resilience` diretamente, sem mTLS, como exemplo mais acessível do padrão de resiliência para APIs internas.

Localização: `samples/Catalogo.HttpClientDemo/`

---

## Middleware e Shared

Componentes em `src/Shared/` utilizados por todos os bounded contexts:

| Componente                               | Descrição                                                                                                                                             |
| ---------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IdempotencyMiddleware`                  | Intercepta POST/PUT/PATCH com header `Idempotency-Key`. Evita reprocessamento de requisições duplicadas. Ver [overview completo](08-IDEMPOTENCIA.md). |
| Global exception handling                | Captura exceções não tratadas e retorna respostas padronizadas (RFC 7807 Problem Details).                                                            |
| JWT Bearer auth                          | Configurado globalmente. Pedidos exigem `RequireAuthorization()`. GET do Catálogo é anônimo; escrita exige token.                                     |
| Rate limiting                            | 3 políticas: `leitura`, `escrita`, `criacao-produto`. Registradas no `Program.cs`. Desativadas quando `Environment = "Testing"`.                      |
| `CatalogoDbContext` / `PedidosDbContext` | Cada bounded context possui seu próprio DbContext com migrations independentes.                                                                       |
| `IEndpoint`                              | Interface com método `MapEndpoints(IEndpointRouteBuilder)`. Implementações são descobertas por reflection.                                            |
| `Result<T>`                              | Tipo discriminado que representa sucesso ou falha sem lançar exceções. Usado exclusivamente no bounded context de Pedidos.                            |

---

## Decisões Arquiteturais

As decisões arquiteturais estão registradas em 16 ADRs no formato MADR 3.x em `docs/ADRs/`. ADR-0011 a ADR-0016 documentam as decisões das Fases 1 a 3 e além (migração do Catálogo para Clean Architecture, novos recursos, rate limiting, estrutura de documentação e separação do Auth como microserviço dedicado).

---

## Estrutura de Diretórios

Para a descrição completa de cada projeto e o grafo de dependências, veja [06-ESTRUTURA.MD](06-ESTRUTURA.MD).

```
net-minimal-api/
├── src/
│   ├── Auth/                               # Bounded Context — Emissor JWT
│   │   ├── Auth.Endpoints/                 # Endpoint de login
│   │   └── Auth.Host/                      # Entry point (porta 5020)
│   │
│   ├── Catalogo/                           # Bounded Context — Clean Architecture híbrida
│   │   ├── Catalogo.Domain/                # Entidades, value objects, interfaces de repositório
│   │   ├── Catalogo.Application/           # Serviços, DTOs, validators
│   │   ├── Catalogo.Infrastructure/        # Repositórios EF Core, DbContext, DbSeeder, migrations
│   │   ├── Catalogo.API/                   # Entry point (porta 5000)
│   │   └── Catalogo.Tests/                 # Testes do Catálogo
│   │
│   ├── Pedidos/                            # Bounded Context — Vertical Slice + Domínio Rico
│   │   ├── Pedidos.Domain/                 # Aggregate Pedido, PedidoItem, StatusPedido
│   │   ├── Pedidos.Infrastructure/         # Repositórios concretos, DbContext, migrations
│   │   ├── Pedidos.API/                    # Entry point (porta 5001)
│   │   │   ├── CreatePedido/               # Command + Handler, Validator, Endpoint
│   │   │   ├── GetPedido/
│   │   │   ├── ListPedidos/
│   │   │   ├── AddItemPedido/
│   │   │   └── CancelPedido/
│   │   └── Pedidos.Tests/                  # Testes de Pedidos
│   │
│   └── Shared/                             # Utilitários compartilhados
│       ├── Data/                           # Extensões de migration
│       ├── Http/                           # Headers e utilitários HTTP
│       ├── Kernel/                         # Result<T>
│       └── Web/                            # IEndpoint, EndpointExtensions
│
└── samples/
    ├── Catalogo.HttpClientDemo/            # Console app — retry + circuit breaker
    └── Pix/                                # Trilha de integração externa
        ├── Pix.MockServer/                 # Minimal API simulando BCB Pix
        ├── Pix.ClientDemo/                 # Console app — mTLS + OAuth2 + resiliência
        └── Pix.MockServer.Tests/           # Testes de integração HTTP
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

| Dimensão                            | Catálogo (Clean Architecture)                                             | Pedidos (Vertical Slice)                                        |
| ----------------------------------- | ------------------------------------------------------------------------- | --------------------------------------------------------------- |
| **Organização do código**           | Por camada técnica (Domain, Application, Infrastructure, API)             | Por caso de uso (CreatePedido, GetPedido, etc.)                 |
| **Localização de um novo endpoint** | 4 sub-projetos diferentes                                                 | Uma pasta isolada                                               |
| **Coesão**                          | Baixa — lógica de um recurso dispersa entre camadas                       | Alta — tudo para um caso de uso na mesma pasta                  |
| **Acoplamento entre features**      | Alto via serviços compartilhados                                          | Baixo — slices independentes                                    |
| **Modelo de domínio**               | Híbrido (rico em Produto/Categoria/Variante, anêmico em Atributo/Mídia)   | Rico (aggregate Pedido com invariantes encapsuladas)            |
| **Tratamento de erro**              | Exceção + middleware global                                               | Result pattern — sem exceptions para erros de negócio           |
| **Quando adicionar campo**          | Toca Domain, Application (DTO + Validator + Service), Infrastructure, API | Toca Domain + slice específica                                  |
| **Teste unitário**                  | Testa serviço via mock de repositório                                     | Testa aggregate direto sem dependência de infraestrutura        |
| **Escalabilidade**                  | Boa até ~50 endpoints por recurso                                         | Excelente — cada feature cresce isolada                         |
| **Overhead inicial**                | Alto (4 projetos, interfaces, repositórios)                               | Baixo (uma pasta por feature)                                   |
| **Indicado para**                   | Times grandes, domínio rico mas previsível, CRUD com regras               | Domínio complexo com muitas invariantes, features independentes |

### Qual escolher no mundo real?

Não são mutuamente exclusivos. Este projeto demonstra os dois coexistindo no mesmo `AppDbContext`:

- Use **Clean Architecture** para recursos com muitas variações de query (paginação, filtros), onde repositórios abstratos e DTOs separados pagam seu custo.
- Use **Vertical Slice** para operações com lógica de negócio densa, onde cada caso de uso tem regras distintas e evolui de forma independente.
- Para CRUD puro sem lógica de negócio (ex.: `Atributo`, `Mídia` no Catálogo), ambas chegam ao mesmo resultado — escolha pelo que o time já conhece.
