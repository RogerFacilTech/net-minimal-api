# Arquitetura do Projeto

## VisÃ£o Estrutural

O projeto hospeda trÃªs bounded contexts no mesmo repositÃ³rio. Cada um segue um padrÃ£o arquitetural independente â€” eles nÃ£o se comunicam diretamente entre si. O que compartilham Ã© restrito a `src/Shared/`: o `AppDbContext`, a interface `IEndpoint`, o padrÃ£o `Result<T>` e o pipeline de middleware.

Essa coexistÃªncia Ã© intencional: permite comparar abordagens diferentes aplicadas ao mesmo stack tecnolÃ³gico, sem a complexidade de mÃºltiplos repositÃ³rios ou serviÃ§os distribuÃ­dos.

---

## CatÃ¡logo â€” Clean Architecture HÃ­brida

O CatÃ¡logo usa Clean Architecture nas camadas internas e Vertical Slice na camada de API (um arquivo por grupo de recursos, em vez de controllers monolÃ­ticos).

### Sub-projetos

| Sub-projeto               | Responsabilidade                                            |
| ------------------------- | ----------------------------------------------------------- |
| `Catalogo.Domain`         | Entidades, value objects, interfaces de repositÃ³rio         |
| `Catalogo.Application`    | ServiÃ§os, DTOs, validadores FluentValidation                |
| `Catalogo.Infrastructure` | RepositÃ³rios EF Core, DbSeeder, configuraÃ§Ãµes de mapeamento |
| `Catalogo.API`            | Endpoints Minimal API, polÃ­ticas de rate limiting           |

### Domain

ContÃ©m 5 entidades: **Produto**, **Categoria**, **Variante**, **Atributo** e **MÃ­dia**.

- **Produto**, **Categoria** e **Variante** tÃªm domÃ­nio rico â€” lÃ³gica de negÃ³cio encapsulada nos prÃ³prios agregados (ex.: `Produto.Desativar()`, `Categoria` com slug gerado e hierarquia pai/filho, `Variante` com value object `SKU`).
- **Atributo** e **MÃ­dia** sÃ£o CRUD simples â€” entidades anÃªmicas com persistÃªncia direta via repositÃ³rio.

### Application

ServiÃ§os de aplicaÃ§Ã£o orquestram casos de uso: recebem DTOs, invocam o domÃ­nio e delegam persistÃªncia ao repositÃ³rio. Validadores FluentValidation ficam nesta camada, junto com os DTOs de entrada e saÃ­da.

### Infrastructure

RepositÃ³rios concretos implementam as interfaces definidas no Domain. O `DbSeeder` popula o banco com dados iniciais (IDs 1â€“5 reservados para categorias, IDs 1â€“8 para produtos).

### API

Cada grupo de recursos tem seu prÃ³prio arquivo de endpoints (`ProdutoEndpoints.cs`, `CategoriaEndpoints.cs`, etc.). Rate limiting Ã© aplicado por polÃ­tica de rota â€” trÃªs polÃ­ticas registradas: `leitura`, `escrita` e `criacao-produto`.

### Fluxo de dados

```
HTTP â†’ Catalogo.API/Endpoints â†’ Catalogo.Application/Services â†’ Catalogo.Infrastructure/Repositories â†’ AppDbContext
                              â†“
                     Catalogo.Domain (entities, value objects)
```

---

## Pedidos â€” Vertical Slice + DomÃ­nio Rico

O bounded context de Pedidos organiza o cÃ³digo por caso de uso, nÃ£o por camada tÃ©cnica. Cada operaÃ§Ã£o Ã© uma pasta autocontida dentro de `src/Pedidos/Features/`.

### Estrutura de uma feature

```
Features/
  CreatePedido/
    CreatePedidoCommand.cs     â† DTO de entrada
    CreatePedidoValidator.cs   â† FluentValidation
    CreatePedidoHandler.cs     â† orquestraÃ§Ã£o do caso de uso
    CreatePedidoEndpoint.cs    â† implementa IEndpoint, auto-descoberto
```

Cada pasta contÃ©m tudo o que aquela operaÃ§Ã£o precisa â€” nenhuma dependÃªncia cruzada entre features.

### DomÃ­nio Rico

O aggregate `Pedido` encapsula as regras de negÃ³cio. MÃ©todos como `Pedido.Create()`, `Pedido.AddItem()` e `Pedido.Cancel()` nunca lanÃ§am exceÃ§Ãµes â€” retornam `Result` ou `Result<T>`. O handler sempre verifica `IsSuccess` antes de acessar `.Value`.

### Auto-descoberta de endpoints

Todos os endpoints implementam a interface `IEndpoint` (`src/Shared/Common/IEndpoint.cs`). O `Program.cs` usa reflection para descobrir e registrar automaticamente todas as implementaÃ§Ãµes â€” nenhum endpoint precisa ser cadastrado manualmente.

### Fluxo de dados

```
HTTP â†’ CreatePedidoEndpoint (IEndpoint, auto-discovered) â†’ CreatePedidoValidator â†’ CreatePedidoHandler â†’ Pedido.Create() â†’ AppDbContext
```

---

## Pix â€” IntegraÃ§Ã£o Externa

### Pix.MockServer

Minimal API autocontida que simula a API Pix do Banco Central do Brasil. Implementa OAuth2 para emissÃ£o de tokens e mTLS para autenticaÃ§Ã£o mÃºtua. Persiste dados em memÃ³ria (sem banco de dados). Seu propÃ³sito Ã© permitir desenvolvimento e testes da integraÃ§Ã£o sem dependÃªncia de ambiente externo.

LocalizaÃ§Ã£o: `samples/Pix/Pix.MockServer/`

### Pix.ClientDemo

Console app que consome o `Pix.MockServer` via `HttpClient` tipado. Demonstra:

- ConfiguraÃ§Ã£o de mTLS com certificado de cliente
- Fluxo OAuth2 (client credentials)
- Pipeline de resiliÃªncia com `AddStandardResilienceHandler` (retry + circuit breaker via `Microsoft.Extensions.Http.Resilience`)

LocalizaÃ§Ã£o: `samples/Pix/Pix.ClientDemo/`

### Catalogo.ClientDemo

Console app que demonstra retry e circuit breaker consumindo a API do CatÃ¡logo. Usa `Microsoft.Extensions.Http.Resilience` diretamente, sem mTLS, como exemplo mais acessÃ­vel do padrÃ£o de resiliÃªncia para APIs internas.

---

## Middleware e Shared

Componentes em `src/Shared/` utilizados por todos os bounded contexts:

| Componente                | DescriÃ§Ã£o                                                                                                                        |
| ------------------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| `IdempotencyMiddleware`   | Intercepta POST/PUT/PATCH com header `Idempotency-Key`. Evita reprocessamento de requisiÃ§Ãµes duplicadas.                         |
| Global exception handling | Captura exceÃ§Ãµes nÃ£o tratadas e retorna respostas padronizadas (RFC 7807 Problem Details).                                       |
| JWT Bearer auth           | Configurado globalmente. Pedidos exigem `RequireAuthorization()`. GET do CatÃ¡logo Ã© anÃ´nimo; escrita exige token.                |
| Rate limiting             | 3 polÃ­ticas: `leitura`, `escrita`, `criacao-produto`. Registradas no `Program.cs`. Desativadas quando `Environment = "Testing"`. |
| `AppDbContext`            | Ãšnico contexto EF Core, compartilhado pelos trÃªs bounded contexts.                                                               |
| `IEndpoint`               | Interface com mÃ©todo `Map(IEndpointRouteBuilder)`. ImplementaÃ§Ãµes sÃ£o descobertas por reflection.                                |
| `Result<T>`               | Tipo discriminado que representa sucesso ou falha sem lanÃ§ar exceÃ§Ãµes. Usado exclusivamente no bounded context de Pedidos.       |

---

## DecisÃµes Arquiteturais

As decisÃµes arquiteturais estÃ£o registradas em 15 ADRs no formato MADR 3.x em `docs/ADRs/`. ADR-0011 a ADR-0015 documentam as decisÃµes das Fases 1 a 3 (migraÃ§Ã£o do CatÃ¡logo para Clean Architecture, novos recursos, rate limiting e estrutura de documentaÃ§Ã£o).

---

## Estrutura de DiretÃ³rios

```
net-minimal-api/
â”œâ”€â”€ src/
â”‚   â”œâ”€â”€ Catalogo/                           # Bounded Context 1 â€” Clean Architecture hÃ­brida
â”‚   â”‚   â”œâ”€â”€ Catalogo.Domain/                # Entidades, value objects, interfaces de repositÃ³rio
â”‚   â”‚   â”œâ”€â”€ Catalogo.Application/           # ServiÃ§os, DTOs, validators, interfaces
â”‚   â”‚   â”œâ”€â”€ Catalogo.Infrastructure/        # RepositÃ³rios EF Core, DbSeeder
â”‚   â”‚   â”œâ”€â”€ Catalogo.API/                   # Endpoints Minimal API, extensÃµes, rate limiting
â”‚   â”‚   â””â”€â”€ Catalogo.ClientDemo/            # Console app com pipeline de resiliÃªncia
â”‚   â”‚
â”‚   â”œâ”€â”€ Pedidos/                            # Bounded Context 2 â€” Vertical Slice + DomÃ­nio Rico
â”‚   â”‚   â”œâ”€â”€ Domain/                         # Aggregate Pedido, PedidoItem, StatusPedido
â”‚   â”‚   â”œâ”€â”€ Features/
â”‚   â”‚   â”‚   â”œâ”€â”€ CreatePedido/               # Command, Validator, Handler, Endpoint
â”‚   â”‚   â”‚   â”œâ”€â”€ GetPedido/
â”‚   â”‚   â”‚   â”œâ”€â”€ ListPedidos/
â”‚   â”‚   â”‚   â”œâ”€â”€ AddItemPedido/
â”‚   â”‚   â”‚   â””â”€â”€ CancelPedido/
â”‚   â”‚   â””â”€â”€ Common/                         # DTOs e tipos compartilhados entre slices
â”‚   â”‚
â”‚   â””â”€â”€ Shared/                             # Compartilhado por todos os bounded contexts
â”‚       â”œâ”€â”€ Common/                         # IEndpoint, Result<T>, EndpointExtensions
â”‚       â”œâ”€â”€ Data/                           # AppDbContext + Migrations + DbSeeder
â”‚       â””â”€â”€ Middleware/                     # ExceptionHandling, IdempotencyMiddleware
â”‚
â”œâ”€â”€ samples/
â”‚   â””â”€â”€ Pix/                                # Trilha de integraÃ§Ã£o externa (educacional)
â”‚       â”œâ”€â”€ Pix.MockServer/                 # Minimal API simulando BCB Pix
â”‚       â”‚   â”œâ”€â”€ Contracts/                  # Requests e responses complexos
â”‚       â”‚   â”œâ”€â”€ Application/                # Regras de negÃ³cio e validaÃ§Ãµes
â”‚       â”‚   â”œâ”€â”€ Infrastructure/InMemory/    # RepositÃ³rios thread-safe
â”‚       â”‚   â””â”€â”€ Security/                   # Bearer + mTLS real
â”‚       â”œâ”€â”€ Pix.ClientDemo/                 # Console app â€” HttpClient tipado + resiliÃªncia
â”‚       â”‚   â”œâ”€â”€ Client/Handlers/            # CorrelationId, IdempotencyKey, Logging
â”‚       â”‚   â””â”€â”€ Scenarios/                  # Fluxo fim-a-fim didÃ¡tico
â”‚       â””â”€â”€ Pix.MockServer.Tests/           # Testes de integraÃ§Ã£o HTTP da trilha PIX
â”‚
â””â”€â”€ tests/
    â”œâ”€â”€ FacShopAPI.Tests/                  # Testes do CatÃ¡logo e Pedidos (150 testes)
    â”‚   â”œâ”€â”€ Unit/Domain/                    # Testes unitÃ¡rios de entidades e agregados
    â”‚   â”œâ”€â”€ Integration/Catalogo/           # Testes HTTP dos endpoints do CatÃ¡logo
    â”‚   â”œâ”€â”€ Integration/Pedidos/            # Testes HTTP dos endpoints de Pedidos
    â”‚   â”œâ”€â”€ Endpoints/                      # Testes de contrato HTTP
    â”‚   â”œâ”€â”€ Services/                       # Testes de serviÃ§os de aplicaÃ§Ã£o
    â”‚   â””â”€â”€ Validators/                     # Testes FluentValidation
    â””â”€â”€ (sem projeto PIX nesta raiz)        # Testes PIX agora em samples/Pix/Pix.MockServer.Tests/
```

---

## Fluxos de RequisiÃ§Ã£o

### CatÃ¡logo â€” Clean Architecture HÃ­brida

```
POST /api/v1/catalogo/produtos
    â”‚
    â”œâ”€ RateLimiter  â† polÃ­tica "criacao-produto" (TokenBucket, 5/min)
    â”‚                  retorna 429 + Retry-After se excedido
    â”‚
    â”œâ”€ IdempotencyMiddleware  â† verifica header Idempotency-Key
    â”‚                           devolve resposta cacheada se chave jÃ¡ vista
    â”‚
    â”œâ”€ RequireAuthorization()  â† valida JWT Bearer
    â”‚                             retorna 401 se ausente, 403 se sem permissÃ£o
    â”‚
    â”œâ”€ CriarProduto (handler local em ProdutoEndpoints.cs)
    â”‚   â”œâ”€ IValidator<CriarProdutoRequest>.ValidateAsync()  â† FluentValidation
    â”‚   â”‚   â””â”€ retorna 422 Unprocessable Entity se invÃ¡lido
    â”‚   â”‚
    â”‚   â””â”€ IProdutoService.CriarProdutoAsync(request)
    â”‚       â”œâ”€ Produto.Criar(...)  â† domÃ­nio rico, retorna Result<Produto>
    â”‚       â”‚   â””â”€ retorna 422 se invariante violada (ex.: preÃ§o â‰¤ 0)
    â”‚       â”œâ”€ IProdutoCommandRepository.AdicionarAsync(produto)
    â”‚       â””â”€ AppDbContext.SaveChangesAsync()
    â”‚
    â””â”€ 201 Created + ProdutoResponse
```

### Pedidos â€” Vertical Slice + DomÃ­nio Rico

```
POST /api/v1/pedidos
    â”‚
    â”œâ”€ RequireAuthorization()  â† JWT obrigatÃ³rio
    â”‚
    â”œâ”€ CreatePedidoEndpoint.Handle(command, handler)   â† IEndpoint, auto-descoberto
    â”‚   â”‚
    â”‚   â””â”€ CreatePedidoHandler.HandleAsync(command)
    â”‚       â”œâ”€ IValidator<CreatePedidoCommand>.ValidateAsync()
    â”‚       â”‚   â””â”€ retorna 400 se invÃ¡lido
    â”‚       â”‚
    â”‚       â”œâ”€ Pedido.Create(clienteNome)  â† retorna Result<Pedido>
    â”‚       â”‚   â””â”€ retorna 400 se nome invÃ¡lido
    â”‚       â”‚
    â”‚       â”œâ”€ foreach item: pedido.AddItem(produto, quantidade)
    â”‚       â”‚   â””â”€ retorna 400 se estoque insuficiente ou pedido nÃ£o estÃ¡ Aberto
    â”‚       â”‚
    â”‚       â”œâ”€ AppDbContext.Pedidos.Add(pedido)
    â”‚       â””â”€ AppDbContext.SaveChangesAsync()
    â”‚
    â””â”€ 201 Created + { id }
```

### PIX â€” Mock Server + Cliente HTTP

```
Pix.ClientDemo                          Pix.MockServer
     â”‚                                       â”‚
     â”œâ”€ POST /oauth/token â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–ºâ”‚
     â”‚   (client_id + client_secret)         â”‚ valida credenciais
     â”‚â—„â”€ 200 { access_token } â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
     â”‚                                       â”‚
     â”œâ”€ POST /pix/v1/cobrancas â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–ºâ”‚
     â”‚   Authorization: Bearer {token}       â”‚ valida Bearer
     â”‚   Idempotency-Key: {uuid}            â”‚ verifica chave
     â”‚   X-Correlation-Id: {uuid}           â”‚ logar para rastreio
     â”‚â—„â”€ 201 { txid, status: "ATIVA" } â”€â”€â”€â”€â”¤
     â”‚                                       â”‚
     â”œâ”€ POST /pix/v1/cobrancas/{txid}       â”‚
     â”‚        /simular-liquidacao â”€â”€â”€â”€â”€â”€â”€â”€â”€â–ºâ”‚ atualiza status para CONCLUIDA
     â”‚â—„â”€ 200 OK â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
     â”‚                                       â”‚
     â”œâ”€ GET /pix/v1/cobrancas/{txid} â”€â”€â”€â”€â”€â”€â–ºâ”‚
     â”‚â—„â”€ 200 { status: "CONCLUIDA" } â”€â”€â”€â”€â”€â”€â”€â”¤
```

---

## Comparativo: Clean Architecture vs Vertical Slice

| DimensÃ£o                            | CatÃ¡logo (Clean Architecture)                                             | Pedidos (Vertical Slice)                                        |
| ----------------------------------- | ------------------------------------------------------------------------- | --------------------------------------------------------------- |
| **OrganizaÃ§Ã£o do cÃ³digo**           | Por camada tÃ©cnica (Domain, Application, Infrastructure, API)             | Por caso de uso (CreatePedido, GetPedido, etc.)                 |
| **LocalizaÃ§Ã£o de um novo endpoint** | 4 sub-projetos diferentes                                                 | Uma pasta isolada                                               |
| **CoesÃ£o**                          | Baixa â€” lÃ³gica de um recurso dispersa entre camadas                       | Alta â€” tudo para um caso de uso na mesma pasta                  |
| **Acoplamento entre features**      | Alto via serviÃ§os compartilhados                                          | Baixo â€” slices independentes                                    |
| **Modelo de domÃ­nio**               | HÃ­brido (rico em Produto/Categoria/Variante, anÃªmico em Atributo/MÃ­dia)   | Rico (aggregate Pedido com invariantes encapsuladas)            |
| **Tratamento de erro**              | ExceÃ§Ã£o + middleware global                                               | Result pattern â€” sem exceptions para erros de negÃ³cio           |
| **Quando adicionar campo**          | Toca Domain, Application (DTO + Validator + Service), Infrastructure, API | Toca Domain + slice especÃ­fica                                  |
| **Teste unitÃ¡rio**                  | Testa serviÃ§o via mock de repositÃ³rio                                     | Testa aggregate direto sem dependÃªncia de infraestrutura        |
| **Escalabilidade**                  | Boa atÃ© ~50 endpoints por recurso                                         | Excelente â€” cada feature cresce isolada                         |
| **Overhead inicial**                | Alto (4 projetos, interfaces, repositÃ³rios)                               | Baixo (uma pasta por feature)                                   |
| **Indicado para**                   | Times grandes, domÃ­nio rico mas previsÃ­vel, CRUD com regras               | DomÃ­nio complexo com muitas invariantes, features independentes |

### Qual escolher no mundo real?

NÃ£o sÃ£o mutuamente exclusivos. Este projeto demonstra os dois coexistindo no mesmo `AppDbContext`:

- Use **Clean Architecture** para recursos com muitas variaÃ§Ãµes de query (paginaÃ§Ã£o, filtros), onde repositÃ³rios abstratos e DTOs separados pagam seu custo.
- Use **Vertical Slice** para operaÃ§Ãµes com lÃ³gica de negÃ³cio densa, onde cada caso de uso tem regras distintas e evolui de forma independente.
- Para CRUD puro sem lÃ³gica de negÃ³cio (ex.: `Atributo`, `MÃ­dia` no CatÃ¡logo), ambas chegam ao mesmo resultado â€” escolha pelo que o time jÃ¡ conhece.
