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
    Catalogo.Application/        -> Casos de uso do Catalogo
    Catalogo.Common/             -> Contratos e utilitarios especificos do Catalogo
    Catalogo.Domain/             -> Dominio do Catalogo
    Catalogo.Endpoints/          -> Endpoints, DTOs, extensoes HTTP
    Catalogo.Host/               -> Entry point especifico do Catalogo
    Catalogo.Infrastructure/     -> EF Core, Dapper, DbSeeder
  Pedidos/
    Pedidos.Application/         -> Casos de uso de Pedidos
    Pedidos.Common/              -> Contratos e utilitarios especificos de Pedidos
    Pedidos.Domain/              -> Dominio rico de Pedidos
    Pedidos.Endpoints/           -> Endpoints, DTOs, validadores
    Pedidos.Host/                -> Entry point especifico de Pedidos
    Pedidos.Infrastructure/      -> Implementacoes de persistencia e integracoes
  Shared/
    Common/                      -> Contratos e utilitarios transversais
    Data/                        -> Persistencia compartilhada
samples/
  Catalogo.HttpClientDemo/       -> Demo de resiliencia HTTP do Catalogo
  Pix.ClientDemo/                -> Console app cliente Pix
  Pix.MockServer/                -> Servidor mock Pix (mTLS + OAuth2)
tests/
  Catalogo.Tests/                -> Testes de Catalogo
  Pedidos.Tests/                 -> Testes de Pedidos
  Pix.MockServer.Tests/          -> Testes do mock Pix
```

## Funcao de Cada Projeto

| Projeto                 | Responsabilidade                                                                                                                      |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Catalogo.Application    | Interfaces de repositorio, servicos de aplicacao, DTOs internos. Orquestra o dominio.                                                 |
| Catalogo.Common         | Contratos e utilitarios especificos do Catalogo.                                                                                      |
| Catalogo.Domain         | Entidades, value objects e regras de negocio do Catalogo. Sem dependencia de framework.                                               |
| Catalogo.Endpoints      | Endpoints Minimal API, validadores, DTOs de request/response, extensoes de rate limiting.                                             |
| Catalogo.Host           | Entry point especifico do Catalogo.                                                                                                   |
| Catalogo.Infrastructure | Implementacoes de repositorio (EF Core + Dapper), DbSeeder, extensoes de DI.                                                          |
| Pedidos.Application     | Interfaces de repositorio, servicos de aplicacao, DTOs internos. Orquestra o dominio.                                                 |
| Pedidos.Common          | Contratos e utilitarios especificos de Pedidos.                                                                                       |
| Pedidos.Domain          | Agregado rico Pedido + PedidoItem, StatusPedido e invariantes de negocio.                                                             |
| Pedidos.Endpoints       | Endpoints Minimal API, validadores, DTOs de request/response.                                                                         |
| Pedidos.Host            | Entry point especifico de Pedidos.                                                                                                    |
| Pedidos.Infrastructure  | Implementacoes de persistencia e integracoes.                                                                                         |
| Shared.Common           | IEndpoint, Result<T>, EndpointExtensions - contratos usados por todos os bounded contexts.                                            |
| Shared.Data             | AppDbContext e migrations - unico DbContext da solucao, cobrindo Catalogo e Pedidos.                                                  |
| Catalogo.HttpClientDemo | Console app demonstrando resiliencia HTTP (retry + circuit breaker) contra a API do Catalogo. Nao e parte da arquitetura de producao. |
| Pix.ClientDemo          | Console app cliente Pix.                                                                                                              |
| Pix.MockServer          | Servidor mock Pix (mTLS + OAuth2).                                                                                                    |

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
