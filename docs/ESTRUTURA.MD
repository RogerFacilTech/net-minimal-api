# Estrutura da Solucao

## Resumo da Arquitetura de Microserviços

Os projetos **Catálogo** e **Pedidos** podem ser classificados como **microserviços** porque:

- **Isolamento de Contextos**: Cada domínio (Catálogo e Pedidos) possui seu próprio conjunto de projetos, regras de negócio, endpoints e infraestrutura, permitindo evolução e manutenção independentes.
- **Deploy Independente**: Os hosts (`Catalogo.Host` e `Pedidos.Host`) permitem que cada serviço seja executado, escalado e publicado separadamente, sem dependências de ciclo de vida entre si.
- **Persistência Compartilhada, Lógica Separada**: Apesar de compartilharem o mesmo `AppDbContext` (por simplicidade educacional), a lógica de domínio, casos de uso e endpoints são totalmente isolados, respeitando os limites de contexto.
- **Contratos e Integrações**: A comunicação entre contextos ocorre apenas por contratos bem definidos (em `Shared.Common`), evitando acoplamento direto entre domínios.
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
  Catalogo/
    Catalogo.Application/        -> Casos de uso do Catálogo
    Catalogo.Common/             -> Contratos e utilitários específicos do Catálogo
    Catalogo.Data/               -> DbContext e migrations do Catálogo
    Catalogo.Domain/             -> Domínio do Catálogo
    Catalogo.Endpoints/          -> Endpoints, DTOs, extensões HTTP
    Catalogo.Host/               -> Entry point específico do Catálogo
    Catalogo.Infrastructure/     -> Implementações de repositório, queries, DbSeeder
  Pedidos/
    Pedidos.Application/         -> Casos de uso de Pedidos
    Pedidos.Common/              -> Contratos e utilitários específicos de Pedidos
    Pedidos.Data/                -> DbContext e migrations de Pedidos
    Pedidos.Domain/              -> Domínio rico de Pedidos
    Pedidos.Endpoints/           -> Endpoints, DTOs, validadores
    Pedidos.Host/                -> Entry point específico de Pedidos
    Pedidos.Infrastructure/      -> Implementações de persistência e integrações
  Shared/
    Data/                        -> Extensões de inicialização de banco compartilhadas
    Http/                        -> Utilitários HTTP, handlers, idempotência
    Kernel/                      -> Result<T> e utilitários de domínio
    Web/                         -> IEndpoint, extensões de endpoint
samples/
  Catalogo.HttpClientDemo/       -> Demo de resiliência HTTP do Catálogo
  Pix.ClientDemo/                -> Console app cliente Pix
  Pix.MockServer/                -> Servidor mock Pix (mTLS + OAuth2)
tests/
  Catalogo.Tests/                -> Testes de Catálogo
  Pedidos.Tests/                 -> Testes de Pedidos
  Pix.MockServer.Tests/          -> Testes do mock Pix
```

## Função de Cada Projeto

| Projeto                 | Responsabilidade                                                                              |
| ----------------------- | --------------------------------------------------------------------------------------------- |
| Catalogo.Application    | Interfaces de repositório, serviços de aplicação, DTOs internos. Orquestra o domínio.         |
| Catalogo.Common         | Contratos e utilitários específicos do Catálogo.                                              |
| Catalogo.Data           | DbContext e migrations do Catálogo.                                                           |
| Catalogo.Domain         | Entidades, value objects e regras de negócio do Catálogo. Sem dependência de framework.       |
| Catalogo.Endpoints      | Endpoints Minimal API, validadores, DTOs de request/response, extensões de rate limiting.     |
| Catalogo.Host           | Entry point específico do Catálogo.                                                           |
| Catalogo.Infrastructure | Implementações de repositório (EF Core + Dapper), queries, DbSeeder, extensões de DI.         |
| Pedidos.Application     | Interfaces de repositório, serviços de aplicação, DTOs internos. Orquestra o domínio.         |
| Pedidos.Common          | Contratos e utilitários específicos de Pedidos.                                               |
| Pedidos.Data            | DbContext e migrations de Pedidos.                                                            |
| Pedidos.Domain          | Agregado rico Pedido + PedidoItem, StatusPedido e invariantes de negócio.                     |
| Pedidos.Endpoints       | Endpoints Minimal API, validadores, DTOs de request/response.                                 |
| Pedidos.Host            | Entry point específico de Pedidos.                                                            |
| Pedidos.Infrastructure  | Implementações de persistência e integrações.                                                 |
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
Catalogo.Domain   Pedidos.Domain
  ^                   ^
Catalogo.Application    Pedidos.Application
  ^                   ^
Catalogo.Infrastructure Pedidos.Infrastructure
  ^                   ^
Catalogo.Endpoints      Pedidos.Endpoints
  ^                   ^
Catalogo.Host           Pedidos.Host
```
