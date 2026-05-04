# Estrutura da Solucao

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
  Host/                          -> Entry point da API
  Shared/
    Common/                      -> Contratos e utilitarios transversais
    Data/                        -> Persistencia compartilhada
  Catalogo/
    Catalogo.Domain/             -> Dominio do Catalogo
    Catalogo.Application/        -> Casos de uso do Catalogo
    Catalogo.Infrastructure/     -> EF Core, Dapper, DbSeeder
    Catalogo.Endpoints/          -> Endpoints, DTOs, extensoes HTTP
  Pedidos/
    Domain/                      -> Dominio rico de Pedidos
    (raiz)/                      -> Slices verticais de Pedidos
  Pix/
    Pix.MockServer/              -> Servidor mock Pix (mTLS + OAuth2)
    Pix.ClientDemo/              -> Console app cliente Pix
samples/
  Catalogo.HttpClientDemo/       -> Demo de resiliencia HTTP do Catalogo
tests/
  ProdutosAPI.Tests/             -> Testes de Catalogo + integracao geral
  Pedidos.Tests/                 -> Testes dos slices de Pedidos
  Pix.MockServer.Tests/          -> Testes do mock Pix
```

## Funcao de Cada Projeto

| Projeto                 | Responsabilidade                                                                                                                      |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Host                    | Program.cs, appsettings.json, middlewares, registro de DI, roteamento. Nao contem logica de negocio.                                  |
| Shared.Common           | IEndpoint, Result<T>, EndpointExtensions - contratos usados por todos os bounded contexts.                                            |
| Shared.Data             | AppDbContext e migrations - unico DbContext da solucao, cobrindo Catalogo e Pedidos.                                                  |
| Catalogo.Domain         | Entidades, value objects e regras de negocio do Catalogo. Sem dependencia de framework.                                               |
| Catalogo.Application    | Interfaces de repositorio, servicos de aplicacao, DTOs internos. Orquestra o dominio.                                                 |
| Catalogo.Infrastructure | Implementacoes de repositorio (EF Core + Dapper), DbSeeder, extensoes de DI.                                                          |
| Catalogo.Endpoints      | Endpoints Minimal API, validadores, DTOs de request/response, extensoes de rate limiting.                                             |
| Pedidos.Domain          | Agregado rico Pedido + PedidoItem, StatusPedido e invariantes de negocio.                                                             |
| Pedidos                 | Slices verticais (Create, Get, List, AddItem, Cancel) com handlers, validators, endpoints e repositorios CQRS.                        |
| Catalogo.HttpClientDemo | Console app demonstrando resiliencia HTTP (retry + circuit breaker) contra a API do Catalogo. Nao e parte da arquitetura de producao. |

## Grafo de Dependencias

```text
Shared.Common
    ^
Catalogo.Domain   Pedidos.Domain (-> Shared.Common + Catalogo.Domain)
    ^                   ^
Catalogo.Application    |
    ^               Shared.Data (-> Pedidos.Domain + Catalogo.Application)
Catalogo.Infrastructure     ^
    ^               Pedidos (-> Pedidos.Domain + Shared.Data + Shared.Common)
Catalogo.Endpoints          ^
         \                  |
              Host <--------+
```
