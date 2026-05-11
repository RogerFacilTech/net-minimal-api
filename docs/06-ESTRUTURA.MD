# Estrutura da Solucao

## Resumo da Arquitetura de Microserviços

Os projetos **Catálogo** e **Pedidos** podem ser classificados como **microserviços** porque:

- **Isolamento de Contextos**: Cada domínio (Catálogo e Pedidos) possui seu próprio conjunto de projetos, regras de negócio, endpoints e infraestrutura, permitindo evolução e manutenção independentes.
- **Deploy Independente**: Os projetos (`Catalogo.API` e `Pedidos.API`) permitem que cada serviço seja executado, escalado e publicado separadamente, sem dependências de ciclo de vida entre si.
- **Persistência Isolada**: Cada domínio possui seu próprio DbContext (`CatalogoDbContext`, `PedidosDbContext`) com migrations independentes, permitindo evolução do schema sem impacto cruzado.
- **Contratos e Integrações**: A comunicação entre contextos ocorre apenas por contratos bem definidos (em `Shared.Kernel` e `Shared.Web`), evitando acoplamento direto entre domínios.
- **Padrões Arquiteturais Distintos**: Cada microserviço pode adotar padrões próprios (ex: Clean Architecture no Catálogo, Vertical Slice em Pedidos), reforçando autonomia tecnológica.

Esses fatores permitem classificar Catálogo e Pedidos como microserviços, mesmo coexistindo na mesma solução para fins didáticos.

## Visao Geral

A solucao foi reorganizada para separar claramente:

- host (entrada da aplicacao)
- shared (contratos e persistencia compartilhada)
- contextos de negocio (Catalogo e Pedidos)
- integracoes (Pix)
- testes por modulo

## Estrutura de Pastas

```text
src/
  Auth/
    Auth.Endpoints/              -> Endpoint de login (POST /api/v1/auth/login)
    Auth.Host/                   -> Entry point do microserviço Auth (porta 5020)
  Catalogo/
    Catalogo.Domain/             -> Domínio do Catálogo
    Catalogo.Application/        -> Casos de uso do Catálogo
    Catalogo.Infrastructure/     -> Implementações de repositório, DbContext, migrations, DbSeeder
    Catalogo.API/                -> Entry point específico do Catálogo (porta 5000)
    Catalogo.Tests/              -> Testes do Catálogo
  Pedidos/
    Pedidos.Domain/              -> Domínio rico de Pedidos
    Pedidos.Infrastructure/      -> Implementações de persistência, DbContext, migrations
    Pedidos.API/                 -> Entry point específico de Pedidos (porta 5001)
    Pedidos.Tests/               -> Testes de Pedidos
  Shared/
    Data/                        -> Extensões de inicialização de banco compartilhadas
    Http/                        -> Utilitários HTTP, handlers, idempotência
    Kernel/                      -> Result<T> e utilitários de domínio
    Web/                         -> IEndpoint, extensões de endpoint
samples/
  Catalogo.HttpClientDemo/       -> Demo de resiliência HTTP do Catálogo
  Pix/
    Pix.ClientDemo/              -> Console app cliente Pix
    Pix.MockServer/              -> Servidor mock Pix (mTLS + OAuth2)
    Pix.MockServer.Tests/        -> Testes do mock Pix
```

## Função de Cada Projeto

| Projeto                 | Responsabilidade                                                                              |
| ----------------------- | --------------------------------------------------------------------------------------------- |
| Auth.Endpoints          | Endpoint de login (`POST /api/v1/auth/login`), emissão de JWT assinado.                       |
| Auth.Host               | Entry point do microserviço Auth. Demais serviços apenas validam o token emitido aqui.        |
| Catalogo.Domain         | Entidades, value objects e regras de negócio do Catálogo. Sem dependência de framework.       |
| Catalogo.Application    | Interfaces de repositório, serviços de aplicação, DTOs internos. Orquestra o domínio.         |
| Catalogo.Infrastructure | Repositórios EF Core, DbContext, migrations, DbSeeder, extensões de DI.                       |
| Catalogo.API            | Entry point do Catálogo. Endpoints, rate limiting, middleware, Program.cs.                    |
| Pedidos.Domain          | Agregado rico Pedido + PedidoItem, StatusPedido e invariantes de negócio.                     |
| Pedidos.Infrastructure  | Repositórios concretos, DbContext, migrations.                                                |
| Pedidos.API             | Entry point de Pedidos. Slices por caso de uso (CreatePedido, GetPedido, etc.).               |
| Shared.Data             | Extensões de inicialização de banco e utilitários de persistência compartilhada.              |
| Shared.Http             | Handlers HTTP, idempotência, correlation ID, logging de request.                              |
| Shared.Kernel           | Result<T>, utilitários de domínio e contratos transversais.                                   |
| Shared.Web              | IEndpoint, EndpointExtensions, utilitários para endpoints Minimal API.                        |
| Catalogo.HttpClientDemo | Console app demonstrando resiliência HTTP (retry + circuit breaker) contra a API do Catálogo. |
| Pix.ClientDemo          | Console app cliente Pix.                                                                      |
| Pix.MockServer          | Servidor mock Pix (mTLS + OAuth2).                                                            |

## Grafo de Dependências (Simplificado)

```text
Shared.Kernel, Shared.Web, Shared.Http, Shared.Data
  ^
Auth.Endpoints
  ^
Auth.Host

Catalogo.Domain
  ^
Catalogo.Application
  ^
Catalogo.Infrastructure
  ^
Catalogo.API

Pedidos.Domain
  ^
Pedidos.Infrastructure
  ^
Pedidos.API
```
