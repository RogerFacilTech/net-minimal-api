# Análise: Consolidação de Projetos por Microserviço

**Data:** 2026-05-11  
**Status:** Proposta para consideração  
**Escopo:** Catálogo e Pedidos

---

## 1. Estrutura Atual

### Catálogo — 8 projetos (excl. Tests)

| Projeto                   | Conteúdo Real                                                                                                               |
| ------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `Catalogo.Domain`         | Entidades (Produto, Categoria, Variante, Atributo, Midia) + ValueObjects                                                    |
| `Catalogo.Application`    | DTOs por recurso, interfaces de repositórios, services com implementação, validators (FluentValidation), profile AutoMapper |
| `Catalogo.Infrastructure` | Repositórios EF (command) e Dapper (query), DbSeeder                                                                        |
| `Catalogo.Data`           | `CatalogoDbContext`, `CatalogoDbContextFactory`, Migrations                                                                 |
| `Catalogo.Common`         | **1 arquivo:** `ProdutoSnapshot.cs`                                                                                         |
| `Catalogo.Endpoints`      | Endpoints organizados por recurso, DTOs de request/response HTTP, extensões de registro                                     |
| `Catalogo.API`            | `Program.cs`, middleware de idempotência, configuração de host                                                              |
| `Catalogo.Tests`          | Testes de integração/unitários                                                                                              |

### Pedidos — 8 projetos (excl. Tests)

| Projeto                  | Conteúdo Real                                                                         |
| ------------------------ | ------------------------------------------------------------------------------------- |
| `Pedidos.Domain`         | Pedido, PedidoItem, StatusPedido, ProdutoSnapshot (agregado rico)                     |
| `Pedidos.Application`    | **2 arquivos:** `IPedidoCommandRepository.cs`, `IPedidoQueryRepository.cs`            |
| `Pedidos.Infrastructure` | Implementações dos repositórios, `CatalogoApiClient`                                  |
| `Pedidos.Data`           | `PedidosDbContext`, `PedidosDbContextFactory`, Migrations                             |
| `Pedidos.Common`         | **1 arquivo:** `PedidoResponse.cs`                                                    |
| `Pedidos.Endpoints`      | Features verticais: CreatePedido, GetPedido, ListPedidos, AddItemPedido, CancelPedido |
| `Pedidos.API`            | `Program.cs`, configuração de host                                                    |
| `Pedidos.Tests`          | Testes                                                                                |

---

## 2. Problemas Identificados

### 2.1 `*.Data` separado de `*.Infrastructure` — duplicação de camada

**Situação:** `Catalogo.Data` e `Pedidos.Data` existem exclusivamente para conter DbContext, fábrica de contexto e migrations. Os repositórios vivem em `*.Infrastructure`, que depende de `*.Data`.

**Problema:** Esta separação não corresponde a nenhum padrão de mercado amplamente adotado. O racional seria "trocar ORM sem tocar nos repositórios", mas na prática:

- O DbContext é um detalhe de infraestrutura, não um contrato público
- Migrations são exclusivamente de infraestrutura
- A separação adiciona um projeto extra sem nenhum benefício de testabilidade ou reuso

**Referência de mercado:** Projetos como eShopOnContainers (Microsoft), Ardalis CleanArchitecture template e a maioria dos projetos .NET reais colocam DbContext + Migrations **dentro** de `Infrastructure`.

### 2.2 `*.Common` quase vazio — projeto sem massa crítica

**Situação:**

- `Catalogo.Common`: 1 arquivo (`ProdutoSnapshot.cs`)
- `Pedidos.Common`: 1 arquivo (`PedidoResponse.cs`)

**Problema:** Um projeto .NET separado para abrigar 1 arquivo cria overhead de build (binário próprio, resolução de dependências), sem nenhum benefício de isolamento ou reuso. `ProdutoSnapshot` é um value object ou DTO de domínio; `PedidoResponse` é um DTO de saída.

### 2.3 `Pedidos.Application` quase vazio — camada sem justificativa

**Situação:** `Pedidos.Application` contém exatamente 2 interfaces de repositório.

**Problema:** Com Vertical Slice (arquitetura adotada em Pedidos), a camada Application clássica perde sentido. As interfaces de repositório poderiam estar em `Pedidos.Domain` (contratos do domínio) ou diretamente em cada feature slice dentro de `Pedidos.Endpoints`. Manter um projeto de 2 arquivos vai contra o espírito do Vertical Slice.

### 2.4 `Catalogo.Endpoints` separado de `Catalogo.API`

**Situação:** Os endpoints do Catálogo vivem em `Catalogo.Endpoints` (projeto biblioteca) e são consumidos por `Catalogo.API` (host).

**Justificativa razoável:** A separação permite que os endpoints sejam testados como unidade sem subir o host completo, e teoricamente permite reaproveitamento em outro host (ex: gRPC). Os testes de integração usam `WebApplicationFactory`, então o ganho real é questionável.

**Quando faz sentido manter:** Se houver planos de múltiplos hosts (ex: worker + web), ou se a testabilidade de endpoints isolados for prioridade explícita.

---

## 3. Proposta de Consolidação

### 3.1 Catálogo — de 7 para 4 projetos de produção

```
Antes (7):                       Depois (4):
Catalogo.Domain          →       Catalogo.Domain
                                   + absorve ProdutoSnapshot de Catalogo.Common
Catalogo.Application     →       Catalogo.Application
                                   (mantém DTOs, services, validators, mappings)
Catalogo.Infrastructure  →       Catalogo.Infrastructure
+ Catalogo.Data          →         + absorve DbContext, DbContextFactory, Migrations
+ Catalogo.Common        →         (ProdutoSnapshot vai para Domain)
Catalogo.Endpoints       →       Catalogo.API
+ Catalogo.API           →         + absorve endpoints, DTOs HTTP, middleware
Catalogo.Tests           →       Catalogo.Tests (inalterado)
```

**Resultado:** 4 projetos de produção (Domain, Application, Infrastructure, API) — o padrão Clean Architecture canônico.

### 3.2 Pedidos — de 7 para 3 projetos de produção

```
Antes (7):                       Depois (3):
Pedidos.Domain           →       Pedidos.Domain
                                   + absorve PedidoResponse de Pedidos.Common
                                   + absorve IPedidoCommandRepository e IPedidoQueryRepository
                                     de Pedidos.Application (contratos de domínio)
Pedidos.Application      →       (eliminado — era quase vazio)
Pedidos.Infrastructure   →       Pedidos.Infrastructure
+ Pedidos.Data           →         + absorve DbContext, DbContextFactory, Migrations
+ Pedidos.Common         →         (PedidoResponse vai para Domain ou Endpoints)
Pedidos.Endpoints        →       Pedidos.API
+ Pedidos.API            →         + features verticais ficam dentro do host
                                   (padrão Vertical Slice puro: tudo no projeto web)
Pedidos.Tests            →       Pedidos.Tests (inalterado)
```

**Resultado:** 3 projetos de produção (Domain, Infrastructure, API) — padrão Vertical Slice com domínio rico isolado.

---

## 4. Comparativo

| Dimensão                         | Catálogo Atual | Catálogo Proposto |      Pedidos Atual      |   Pedidos Proposto    |
| -------------------------------- | :------------: | :---------------: | :---------------------: | :-------------------: |
| Projetos de produção             |       7        |         4         |            7            |           3           |
| Projetos quase vazios            |   1 (Common)   |         0         | 2 (Application, Common) |           0           |
| Projetos duplicados (Data+Infra) |      Sim       |        Não        |           Sim           |          Não          |
| Aderência ao padrão adotado      |    Parcial     | Alta (Clean Arch) |         Parcial         | Alta (Vertical Slice) |

---

## 5. Referências de Mercado

- **eShopOnContainers** (Microsoft): Domain, Application, Infrastructure, API — 4 projetos por serviço. DbContext dentro de Infrastructure.
- **Ardalis CleanArchitecture template**: Core (Domain+Application), Infrastructure, Web — 3 projetos. DbContext em Infrastructure.
- **Wolverine/Marten Vertical Slice**: Features auto-contidas no projeto web; apenas domínio separado.
- **Jimmy Bogard (CQRS + Vertical Slice)**: Um único projeto web com features organizadas por pasta, com domínio opcionalmente separado.

---

## 6. Riscos e Considerações

1. **Migrações EF**: Ao mover o DbContext para Infrastructure, o projeto `*.Data` (que tem `EF Core Design` como `PrivateAssets`) precisa ser substituído por uma `IDesignTimeDbContextFactory` dentro de Infrastructure, com o pacote `Microsoft.EntityFrameworkCore.Design` referenciado diretamente em Infrastructure.

2. **Contexto educacional**: O projeto é declaradamente educacional. Manter alguns projetos extras pode ser didaticamente útil para demonstrar onde cada peça se encaixa. A consolidação proposta ainda preserva separação de conceitos — apenas elimina projetos praticamente vazios.

3. **Impacto nos testes**: `ApiFactory` e `RateLimitingApiFactory` referenciam projetos diretamente. A consolidação exige atualizar referências nos `.csproj` de testes.

4. **Ordem de implementação sugerida** (se aprovado):
    - Fase 1: Fundir `*.Data` em `*.Infrastructure` (menor risco, maior ganho)
    - Fase 2: Eliminar `*.Common` (trivial — mover 1 arquivo cada)
    - Fase 3: Fundir `Pedidos.Application` em `Pedidos.Domain`
    - Fase 4: Fundir `*.Endpoints` em `*.API` (maior refactoring)

---

## 7. Conclusão

A estrutura atual tem **fragmentação excessiva** em projetos quase vazios que não agregam isolamento real. A separação `*.Data` / `*.Infrastructure` é o principal candidato a consolidação — é incomum no mercado e cria dependência de projeto extra sem benefício claro. Os projetos `*.Common` são os segundos na fila, por conterem um único arquivo.

A proposta preserva os padrões arquiteturais adotados (Clean Architecture no Catálogo, Vertical Slice no Pedidos) enquanto alinha a granularidade de projetos com o que se vê em soluções de produção reais.
