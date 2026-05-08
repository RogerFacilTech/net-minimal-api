# VisÃ£o Geral do Projeto

## Sobre o Projeto

Este Ã© um projeto educacional em **.NET 10 Minimal API** que demonstra trÃªs bounded contexts coexistindo no mesmo repositÃ³rio, cada um seguindo um padrÃ£o arquitetural distinto. O objetivo Ã© permitir comparaÃ§Ã£o direta entre abordagens â€” o mesmo problema (uma API REST com persistÃªncia, validaÃ§Ã£o e testes) resolvido com diferentes graus de estrutura e separaÃ§Ã£o de responsabilidades.

## Os TrÃªs Bounded Contexts

## Autenticacao centralizada (Auth)

A emissao de JWT da solution foi centralizada no microservico `Auth`.

- Emissor de token: `src/Auth/Auth.Host/` + `src/Auth/Auth.Endpoints/`
- Endpoint de login: `POST /api/v1/auth/login` (Auth)
- Resource servers: `Catalogo` e `Pedidos` apenas validam JWT

Diagrama simplificado do fluxo:

```mermaid
flowchart LR
   Cliente[Cliente / Swagger / Testes] -->|POST /api/v1/auth/login| Auth[Auth.Host]
   Auth -->|JWT assinado| Cliente
   Cliente -->|Bearer token| Catalogo[Catalogo.Host]
   Cliente -->|Bearer token| Pedidos[Pedidos.Host]
   Catalogo -->|valida assinatura, issuer, audience| JWT[(JWT)]
   Pedidos -->|valida assinatura, issuer, audience| JWT
```

### CatÃ¡logo

|                |                                                          |
| -------------- | -------------------------------------------------------- |
| **PadrÃ£o**    | Clean Architecture hÃ­brida (CA nas camadas, VSA na API) |
| **DiretÃ³rio** | `src/Catalogo/`                                          |
| **Rotas**      | `/api/v1/catalogo/*`                                     |

Demonstra separaÃ§Ã£o em sub-projetos (Domain, Application, Infrastructure, API), entidades com domÃ­nio rico, value objects, repositÃ³rios abstraÃ­dos por interfaces e rate limiting por polÃ­tica de rota. ContÃ©m 5 recursos: Produto, Categoria, Variante, Atributo e MÃ­dia.

Caminhos relevantes:

- `src/Catalogo/Catalogo.Domain/` â€” entidades e value objects
- `src/Catalogo/Catalogo.Application/` â€” serviÃ§os, DTOs e validadores
- `src/Catalogo/Catalogo.Infrastructure/` â€” repositÃ³rios EF Core e DbSeeder
- `src/Catalogo/Catalogo.API/Endpoints/` â€” um arquivo por grupo de recursos

---

### Pedidos

|                |                                             |
| -------------- | ------------------------------------------- |
| **PadrÃ£o**    | Vertical Slice Architecture + DomÃ­nio Rico |
| **DiretÃ³rio** | `src/Pedidos/`                              |
| **Rotas**      | `/api/v1/pedidos/*`                         |

Demonstra organizaÃ§Ã£o por caso de uso (cada operaÃ§Ã£o Ã© uma pasta isolada), aggregate com regras de negÃ³cio encapsuladas, o padrÃ£o `Result<T>` em vez de exceÃ§Ãµes, e auto-descoberta de endpoints via reflection. AutenticaÃ§Ã£o JWT obrigatÃ³ria.

Caminhos relevantes:

- `src/Pedidos/Features/` â€” pastas por caso de uso (CreatePedido, GetPedido, etc.)
- `src/Pedidos/Domain/` â€” aggregate Pedido
- `src/Shared/Common/IEndpoint.cs` â€” contrato de auto-registro

---

### Pix

|                |                                            |
| -------------- | ------------------------------------------ |
| **PadrÃ£o**    | Mock Server + HTTP Client com resiliÃªncia |
| **DiretÃ³rio** | `samples/Pix/`                             |
| **Rotas**      | â€” (integraÃ§Ã£o externa)                 |

Demonstra como integrar com APIs externas usando mTLS, OAuth2 e pipelines de resiliÃªncia (Polly via `Microsoft.Extensions.Http.Resilience`). Inclui um servidor mock que simula a API Pix do BCB e um console app que o consome.

Caminhos relevantes:

- `samples/Pix/Pix.MockServer/` â€” Minimal API simulando BCB Pix
- `samples/Pix/Pix.ClientDemo/` â€” console app com HttpClient tipado e resiliÃªncia

---

## Caminhos de Aprendizado

### Iniciante

Foco em entender a estrutura bÃ¡sica do projeto e as boas prÃ¡ticas de API REST.

```
README.md
  â†’ docs/01-ARQUITETURA.md
  â†’ docs/02-CATALOGO.md
  â†’ docs/guias/MELHORES-PRATICAS-API.md
```

### IntermediÃ¡rio

Adiciona o estudo de padrÃµes arquiteturais mais sofisticados e domÃ­nio rico.

```
(Iniciante, mais:)
  â†’ docs/03-PEDIDOS.md        (Vertical Slice Architecture na prÃ¡tica)
  â†’ Leitura: VSA vs. Layered  (teoria de organizaÃ§Ã£o por caso de uso)
  â†’ src/Shared/Common/        (Result pattern, IEndpoint)
```

### Arquiteto

Estudo completo incluindo integraÃ§Ã£o externa, estratÃ©gia de testes e decisÃµes arquiteturais registradas.

```
(IntermediÃ¡rio, mais:)
  â†’ docs/04-PIX.md            (mTLS, OAuth2, resiliÃªncia)
  â†’ docs/05-TESTES.md         (estratÃ©gia e execuÃ§Ã£o)
  â†’ docs/ADRs/                (15 ADRs no formato MADR 3.x)
   â†’ samples/Pix/Pix.ClientDemo/   (resiliÃªncia com Polly/Http.Resilience)
```

---

## Mapa da DocumentaÃ§Ã£o

| Arquivo                  | Objetivo                            | PÃºblico                   |
| ------------------------ | ----------------------------------- | -------------------------- |
| `README.md`              | VisÃ£o geral e inÃ­cio rÃ¡pido      | Todos                      |
| `docs/01-ARQUITETURA.md` | PadrÃµes e fluxo de dados           | IntermediÃ¡rio / Arquiteto |
| `docs/02-CATALOGO.md`    | Deep-dive do CatÃ¡logo              | IntermediÃ¡rio             |
| `docs/03-PEDIDOS.md`     | Vertical Slice e domÃ­nio rico      | IntermediÃ¡rio / Arquiteto |
| `docs/04-PIX.md`         | IntegraÃ§Ã£o externa e resiliÃªncia | IntermediÃ¡rio             |
| `docs/05-TESTES.md`      | EstratÃ©gia e execuÃ§Ã£o de testes  | Todos                      |
| `docs/guias/`            | Guias conceituais de REST e .NET    | Iniciante / IntermediÃ¡rio |
| `docs/ADRs/`             | DecisÃµes arquiteturais registradas | Arquiteto                  |

---

## Tecnologias

| Tecnologia              | VersÃ£o | Papel                          |
| ----------------------- | ------- | ------------------------------ |
| .NET                    | 10 LTS  | Runtime e SDK                  |
| EF Core                 | 10      | ORM e migraÃ§Ãµes              |
| SQLite                  | â€”     | Banco de dados (dev/testes)    |
| FluentValidation        | 11      | ValidaÃ§Ã£o de entrada         |
| Polly / Http.Resilience | v8      | Retry, circuit breaker         |
| JWT Bearer              | â€”     | AutenticaÃ§Ã£o em Pedidos      |
| xUnit                   | â€”     | Framework de testes            |
| FluentAssertions        | 6       | Assertivas legÃ­veis em testes |
| AutoMapper              | 13      | Mapeamento DTO â†” entidade    |
| Serilog                 | 4       | Logging estruturado            |
| Swagger / OpenAPI       | â€”     | DocumentaÃ§Ã£o interativa      |

---

## Roteiros de Aprendizado

### Roteiro 1 â€” Iniciante (2â€“3 horas)

Foco: entender a estrutura bÃ¡sica, testar a API e ver as boas prÃ¡ticas.

```
1. Executar a API (5 min)
   dotnet run --project src/Catalogo/Catalogo.API

2. Abrir Swagger e explorar endpoints
   http://localhost:5001/swagger

3. Ler o guia teÃ³rico
   docs/guias/MELHORES-PRATICAS-API.md

4. Explorar a Clean Architecture do CatÃ¡logo
   docs/02-CATALOGO.md
   â†’ src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs
   â†’ src/Catalogo/Catalogo.Application/Services/ProdutoService.cs
   â†’ src/Catalogo/Catalogo.Domain/Produto.cs

5. Testar via curl (exemplos em docs/02-CATALOGO.md â†’ seÃ§Ã£o "Exemplos cURL")

6. Rodar os testes
   dotnet test tests/FacShopAPI.Tests/
```

### Roteiro 2 â€” IntermediÃ¡rio (2â€“3 horas adicionais)

Foco: padrÃµes arquiteturais, domÃ­nio rico e Vertical Slice.

```
1. Ler o comparativo arquitetural
   docs/01-ARQUITETURA.md (seÃ§Ã£o "Comparativo")

2. Estudar Vertical Slice com Pedidos
   docs/03-PEDIDOS.md (completo â€” tem snippets de cÃ³digo)
   â†’ src/Pedidos/Domain/Pedido.cs          (aggregate rico)
   â†’ src/Pedidos/Features/CreatePedido/    (slice completa)
   â†’ src/Shared/Common/Result.cs           (Result pattern)
   â†’ src/Shared/Common/IEndpoint.cs        (auto-discovery)

3. Comparar os dois modelos lado a lado
   â†’ Produto.Criar() vs. Pedido.Create()
   â†’ ProdutoService vs. CreatePedidoHandler
   â†’ ProdutoEndpoints vs. CreatePedidoEndpoint

4. Explorar testes
   docs/05-TESTES.md
   â†’ tests/FacShopAPI.Tests/Unit/Domain/ProdutoTests.cs
   â†’ tests/FacShopAPI.Tests/Unit/Domain/PedidoTests.cs

5. Ler o guia de implementaÃ§Ã£o em .NET
   docs/guias/MELHORES-PRATICAS-MINIMAL-API.md
```

### Roteiro 3 â€” Arquiteto (3â€“4 horas adicionais)

Foco: decisÃµes arquiteturais registradas, integraÃ§Ã£o externa, resiliÃªncia e testes avanÃ§ados.

```
1. Ler os 15 ADRs (MADR 3.x)
   docs/ADRs/ â€” decisÃµes registradas com contexto, alternativas e consequÃªncias
   Destaques: ADR-0003 (Result pattern), ADR-0009 (IEndpoint), ADR-0013 (rate limiting)

2. Estudar integraÃ§Ã£o PIX
   docs/04-PIX.md (completo â€” snippets de HttpClient, OAuth2, idempotÃªncia)
   â†’ samples/Pix/Pix.MockServer/Program.cs     (mock server)
   â†’ samples/Pix/Pix.ClientDemo/Program.cs     (pipeline de handlers)

3. Executar e observar a trilha PIX
   Terminal 1: dotnet run --project samples/Pix/Pix.MockServer/Pix.MockServer.csproj
   Terminal 2: dotnet run --project samples/Pix/Pix.ClientDemo/Pix.ClientDemo.csproj
   Testes:     dotnet test samples/Pix/Pix.MockServer.Tests/

4. Estudar rate limiting avanÃ§ado
   docs/02-CATALOGO.md â†’ seÃ§Ã£o "Rate Limiting" e "Exemplos de CÃ³digo"
   docs/05-TESTES.md â†’ seÃ§Ã£o "Teste de rate limiting"
   â†’ src/Catalogo/Catalogo.API/Extensions/RateLimitingExtensions.cs

5. Explorar o ClientDemo de resiliÃªncia
   â†’ src/Catalogo/Catalogo.ClientDemo/     (retry + circuit breaker)
   â†’ Polly v8 / Microsoft.Extensions.Http.Resilience

6. Leitura complementar
   docs/guias/JSON-COMPLEXO-E-BOAS-PRATICAS.md
   docs/guias/MELHORIAS-DOTNET-10.md
```

---

## InÃ­cio RÃ¡pido (5 minutos)

```bash
# PrÃ©-requisito: .NET 10 SDK (https://dotnet.microsoft.com/download/dotnet/10.0)

# Restaurar e executar
dotnet restore
dotnet run --project src/Catalogo/Catalogo.API

# Swagger UI
open http://localhost:5001/swagger

# Rodar todos os testes (150 testes no total)
dotnet test FacShopAPI.slnx -v minimal
```

Credenciais para obter JWT nos testes e no Swagger: `admin@example.com` / `senha123`.
