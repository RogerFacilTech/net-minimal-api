# Plano Detalhado: Consolidação de Projetos

**Data:** 2026-05-11  
**Status:** Aguardando aprovação  
**Base:** Análise `2026-05-11-analise-consolidacao-projetos.md`

**Meta:**

- Catálogo: 7 projetos de produção → **4** (Domain, Application, Infrastructure, API)
- Pedidos: 7 projetos de produção → **3** (Domain, Infrastructure, API)

---

## PARTE 1 — CATÁLOGO

### Fase C1 — Fundir `Catalogo.Data` → `Catalogo.Infrastructure`

**Por quê primeiro:** maior impacto, menor risco de namespace; Data não tem lógica, só contexto e migrations.

#### C1.1 — Mover arquivos

```
src/Catalogo/Catalogo.Data/CatalogoDbContext.cs        → src/Catalogo/Catalogo.Infrastructure/
src/Catalogo/Catalogo.Data/CatalogoDbContextFactory.cs → src/Catalogo/Catalogo.Infrastructure/
src/Catalogo/Catalogo.Data/Migrations/                 → src/Catalogo/Catalogo.Infrastructure/Migrations/
```

#### C1.2 — Atualizar namespaces dos arquivos movidos

| Arquivo                       | De                                   | Para                                           |
| ----------------------------- | ------------------------------------ | ---------------------------------------------- |
| `CatalogoDbContext.cs`        | `namespace FacShopAPI.Catalogo.Data` | `namespace FacShopAPI.Catalogo.Infrastructure` |
| `CatalogoDbContextFactory.cs` | `namespace FacShopAPI.Catalogo.Data` | `namespace FacShopAPI.Catalogo.Infrastructure` |

#### C1.3 — Atualizar `Catalogo.Infrastructure.csproj`

Adicionar pacotes (que estavam em `Catalogo.Data.csproj`):

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.7">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.7" />
```

Remover (Data passará a não existir):

```xml
<!-- REMOVER esta linha se existir -->
<ProjectReference Include="../Catalogo.Data/Catalogo.Data.csproj" />
```

#### C1.4 — Atualizar `Catalogo.API.csproj`

```xml
<!-- REMOVER -->
<ProjectReference Include="../Catalogo.Data/Catalogo.Data.csproj" />
```

`Catalogo.Infrastructure` já é referenciado — não precisa adicionar.

#### C1.5 — Atualizar `using` em `Program.cs`

```csharp
// DE:
using FacShopAPI.Catalogo.Data;
// PARA:
using FacShopAPI.Catalogo.Infrastructure;
```

#### C1.6 — Atualizar `ApiFactory.cs` em Catalogo.Tests

```csharp
// DE:
using FacShopAPI.Catalogo.Data;
// PARA:
using FacShopAPI.Catalogo.Infrastructure;
```

#### C1.7 — Atualizar referência de migration path

Ao rodar `dotnet ef migrations add` e `dotnet ef database update`, o projeto de startup passa a ser `Catalogo.Infrastructure` (ou ainda `Catalogo.API` com `--project Catalogo.Infrastructure`). Verificar se existe script de migration e atualizar.

#### C1.8 — Deletar projeto `Catalogo.Data`

1. Remover a pasta `src/Catalogo/Catalogo.Data/`
2. Remover a linha do `FacShop.slnx`:
    ```xml
    <Project Path="src/Catalogo/Catalogo.Data/Catalogo.Data.csproj" />
    ```

#### C1.9 — Validar

```bash
dotnet build src/Catalogo/Catalogo.Infrastructure/Catalogo.Infrastructure.csproj
dotnet build src/Catalogo/Catalogo.API/Catalogo.API.csproj
dotnet test src/Catalogo/Catalogo.Tests/
```

---

### Fase C2 — Fundir `Catalogo.Common` → `Catalogo.Domain`

**Conteúdo atual:** 1 arquivo (`ProdutoSnapshot.cs`) que não é usado em nenhum código de produção — apenas disponível via referência de projeto nos testes.

#### C2.1 — Mover arquivo

```
src/Catalogo/Catalogo.Common/ProdutoSnapshot.cs → src/Catalogo/Catalogo.Domain/ProdutoSnapshot.cs
```

#### C2.2 — Atualizar namespace do arquivo

```csharp
// DE:
namespace FacShopAPI.Catalogo.Common;
// PARA:
namespace Catalogo.Domain;  // mesmo namespace das outras entidades de Domain
```

#### C2.3 — Atualizar `Catalogo.Tests.csproj`

```xml
<!-- REMOVER -->
<ProjectReference Include="../Catalogo.Common/Catalogo.Common.csproj" />
```

`Catalogo.Domain` já é referenciado nos testes.

#### C2.4 — Atualizar `using` nos arquivos de teste que importam `FacShopAPI.Catalogo.Common`

Fazer grep por `FacShopAPI.Catalogo.Common` em `src/Catalogo/Catalogo.Tests/` e trocar por `Catalogo.Domain`.

#### C2.5 — Deletar projeto `Catalogo.Common`

1. Remover pasta `src/Catalogo/Catalogo.Common/`
2. Remover do `FacShop.slnx`:
    ```xml
    <Project Path="src/Catalogo/Catalogo.Common/Catalogo.Common.csproj" />
    ```

#### C2.6 — Validar

```bash
dotnet build FacShop.slnx
dotnet test src/Catalogo/Catalogo.Tests/
```

---

### Fase C3 — Fundir `Catalogo.Endpoints` → `Catalogo.API`

**Esta é a fase de maior escopo** — envolve mover código de apresentação HTTP para dentro do projeto host.

#### C3.1 — Mover pastas para `Catalogo.API`

```
src/Catalogo/Catalogo.Endpoints/Endpoints/    → src/Catalogo/Catalogo.API/Endpoints/
src/Catalogo/Catalogo.Endpoints/DTOs/         → src/Catalogo/Catalogo.API/DTOs/
src/Catalogo/Catalogo.Endpoints/Extensions/   → src/Catalogo/Catalogo.API/Extensions/
src/Catalogo/Catalogo.Endpoints/GlobalUsings.cs → mesclar com GlobalUsings.cs existente no API (se houver) ou mover
```

#### C3.2 — Atualizar namespaces nos arquivos movidos

Todas as declarações `namespace FacShopAPI.Catalogo.Endpoints.*` devem ser trocadas por `Catalogo.API.*` (ou `FacShopAPI.Catalogo.API.*` — manter consistência com o `RootNamespace` do projeto API).

Exemplo:

```csharp
// DE:
namespace FacShopAPI.Catalogo.Endpoints.Endpoints.Produtos;
// PARA:
namespace Catalogo.API.Endpoints.Produtos;
```

#### C3.3 — Atualizar `Catalogo.API.csproj`

Adicionar pacotes que estavam em `Catalogo.Endpoints.csproj`:

```xml
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.10.0" />
<!-- JWT já existe no API.csproj -->
```

Remover referência ao projeto Endpoints:

```xml
<!-- REMOVER -->
<ProjectReference Include="../Catalogo.Endpoints/Catalogo.Endpoints.csproj" />
```

#### C3.4 — Atualizar `Program.cs`

Os `using` que apontam para `FacShopAPI.Catalogo.Endpoints.Endpoints.*` precisam ser atualizados para os novos namespaces:

```csharp
// DE:
using FacShopAPI.Catalogo.Endpoints.Endpoints.Atributos;
using FacShopAPI.Catalogo.Endpoints.Endpoints.Categorias;
// etc.
// PARA:
using Catalogo.API.Endpoints.Atributos;
using Catalogo.API.Endpoints.Categorias;
// etc.
```

Idem para:

```csharp
// DE:
using FacShopAPI.Catalogo.Endpoints.Extensions;
// PARA:
using Catalogo.API.Extensions;
```

#### C3.5 — Atualizar `Catalogo.Tests.csproj`

```xml
<!-- REMOVER -->
<ProjectReference Include="../Catalogo.Endpoints/Catalogo.Endpoints.csproj" />
```

`Catalogo.API` já é referenciado e agora contém tudo.

#### C3.6 — Atualizar `using` nos arquivos de teste

Fazer grep por `FacShopAPI.Catalogo.Endpoints` nos testes e atualizar para novos namespaces.

#### C3.7 — Deletar projeto `Catalogo.Endpoints`

1. Remover pasta `src/Catalogo/Catalogo.Endpoints/`
2. Remover do `FacShop.slnx`:
    ```xml
    <Project Path="src/Catalogo/Catalogo.Endpoints/Catalogo.Endpoints.csproj" />
    ```

#### C3.8 — Validar

```bash
dotnet build src/Catalogo/Catalogo.API/Catalogo.API.csproj
dotnet test src/Catalogo/Catalogo.Tests/
```

---

### Resultado Final — Catálogo

```
Antes                         Depois
─────────────────────         ─────────────────────
Catalogo.Domain         →     Catalogo.Domain
                                + ProdutoSnapshot (de Common)
Catalogo.Application    →     Catalogo.Application (inalterado)
Catalogo.Infrastructure →     Catalogo.Infrastructure
+ Catalogo.Data         →       + DbContext, DbContextFactory, Migrations
+ Catalogo.Common       →     (eliminado)
Catalogo.Endpoints      →     Catalogo.API
+ Catalogo.API          →       + Endpoints, DTOs, Extensions, Middleware
Catalogo.Tests          →     Catalogo.Tests (inalterado)
```

**Dependências resultantes:**

```
Catalogo.API
  ├── Catalogo.Application
  ├── Catalogo.Infrastructure
  │     ├── Catalogo.Application
  │     └── Catalogo.Domain
  └── Catalogo.Domain

Catalogo.Application
  └── Catalogo.Domain
```

---

---

## PARTE 2 — PEDIDOS

### Fase P1 — Fundir `Pedidos.Data` → `Pedidos.Infrastructure`

Mesma lógica da Fase C1.

#### P1.1 — Mover arquivos

```
src/Pedidos/Pedidos.Data/PedidosDbContext.cs        → src/Pedidos/Pedidos.Infrastructure/
src/Pedidos/Pedidos.Data/PedidosDbContextFactory.cs → src/Pedidos/Pedidos.Infrastructure/
src/Pedidos/Pedidos.Data/Migrations/                → src/Pedidos/Pedidos.Infrastructure/Migrations/
```

#### P1.2 — Atualizar namespaces dos arquivos movidos

| Arquivo                      | De                                  | Para                                          |
| ---------------------------- | ----------------------------------- | --------------------------------------------- |
| `PedidosDbContext.cs`        | `namespace FacShopAPI.Pedidos.Data` | `namespace FacShopAPI.Pedidos.Infrastructure` |
| `PedidosDbContextFactory.cs` | `namespace FacShopAPI.Pedidos.Data` | `namespace FacShopAPI.Pedidos.Infrastructure` |

#### P1.3 — Atualizar `Pedidos.Infrastructure.csproj`

Adicionar pacotes:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.7">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.7" />
```

Remover referência ao Data:

```xml
<!-- REMOVER -->
<ProjectReference Include="../Pedidos.Data/Pedidos.Data.csproj" />
```

#### P1.4 — Atualizar `Pedidos.API.csproj`

```xml
<!-- REMOVER -->
<ProjectReference Include="../Pedidos.Data/Pedidos.Data.csproj" />
```

#### P1.5 — Atualizar `using` em `Program.cs` e `ApiFactory.cs` (Pedidos.Tests)

```csharp
// DE:
using FacShopAPI.Pedidos.Data;
// PARA:
using FacShopAPI.Pedidos.Infrastructure;
```

#### P1.6 — Atualizar `PedidoCommandRepository.cs` e `PedidoQueryRepository.cs`

Estes já estão em Infrastructure e importam `FacShopAPI.Pedidos.Data` — atualizar:

```csharp
// DE:
using FacShopAPI.Pedidos.Data;
// PARA:
using FacShopAPI.Pedidos.Infrastructure;
```

#### P1.7 — Deletar projeto `Pedidos.Data`

1. Remover pasta `src/Pedidos/Pedidos.Data/`
2. Remover do `FacShop.slnx`

#### P1.8 — Validar

```bash
dotnet build src/Pedidos/Pedidos.Infrastructure/Pedidos.Infrastructure.csproj
dotnet build src/Pedidos/Pedidos.API/Pedidos.API.csproj
```

---

### Fase P2 — Fundir `Pedidos.Application` + `Pedidos.Common` → `Pedidos.Domain`

**Atenção à ordem:** mover Common antes de Application, pois `IPedidoQueryRepository` (em Application) depende de `PedidoResponse` (em Common).

#### P2.1 — Mover `PedidoResponse.cs` para Domain

```
src/Pedidos/Pedidos.Common/PedidoResponse.cs → src/Pedidos/Pedidos.Domain/PedidoResponse.cs
```

Atualizar namespace:

```csharp
// DE:
namespace FacShopAPI.Pedidos.Common;
// PARA:
namespace FacShopAPI.Pedidos.Domain;
```

`PedidoResponse.From(Pedido)` usa `Pedido` e `PedidoItem` — ambos já estão em Domain. Sem dependência circular.

#### P2.2 — Mover interfaces de repositório para Domain

```
src/Pedidos/Pedidos.Application/IPedidoCommandRepository.cs → src/Pedidos/Pedidos.Domain/
src/Pedidos/Pedidos.Application/IPedidoQueryRepository.cs   → src/Pedidos/Pedidos.Domain/
```

Atualizar namespaces:

```csharp
// DE:
namespace FacShopAPI.Pedidos.Repositories;
// PARA:
namespace FacShopAPI.Pedidos.Domain;
```

> **Nota:** O `using FacShopAPI.Pedidos.Common` dentro de `IPedidoQueryRepository.cs` vira `using FacShopAPI.Pedidos.Domain` — mas como agora são do mesmo namespace, o using pode ser removido.

#### P2.3 — Atualizar `Pedidos.Domain.csproj`

Não há necessidade de novos pacotes — Domain já referencia `Shared.Kernel`.

#### P2.4 — Atualizar todos os arquivos que importam os namespaces antigos

Fazer grep por `FacShopAPI.Pedidos.Repositories` e `FacShopAPI.Pedidos.Common` em todo o código de Pedidos:

```csharp
// Em PedidoCommandRepository.cs, PedidoQueryRepository.cs, Program.cs, endpoints:
// DE:
using FacShopAPI.Pedidos.Repositories;
using FacShopAPI.Pedidos.Common;
// PARA:
using FacShopAPI.Pedidos.Domain;
```

#### P2.5 — Atualizar `Pedidos.Infrastructure.csproj`

```xml
<!-- REMOVER -->
<ProjectReference Include="../Pedidos.Application/Pedidos.Application.csproj" />

<!-- ADICIONAR (se não existir) -->
<ProjectReference Include="../Pedidos.Domain/Pedidos.Domain.csproj" />
```

#### P2.6 — Atualizar `Pedidos.API.csproj`

```xml
<!-- REMOVER -->
<ProjectReference Include="../Pedidos.Application/Pedidos.Application.csproj" />
<ProjectReference Include="../Pedidos.Common/Pedidos.Common.csproj" />
```

`Pedidos.Domain` já deve ser referenciado.

#### P2.7 — Deletar projetos `Pedidos.Application` e `Pedidos.Common`

1. Remover pastas
2. Remover ambas as linhas do `FacShop.slnx`

#### P2.8 — Validar

```bash
dotnet build src/Pedidos/Pedidos.Domain/Pedidos.Domain.csproj
dotnet build src/Pedidos/Pedidos.Infrastructure/Pedidos.Infrastructure.csproj
dotnet build src/Pedidos/Pedidos.API/Pedidos.API.csproj
```

---

### Fase P3 — Fundir `Pedidos.Endpoints` → `Pedidos.API`

#### P3.1 — Mover pastas de features para `Pedidos.API`

```
src/Pedidos/Pedidos.Endpoints/AddItemPedido/  → src/Pedidos/Pedidos.API/AddItemPedido/
src/Pedidos/Pedidos.Endpoints/CancelPedido/   → src/Pedidos/Pedidos.API/CancelPedido/
src/Pedidos/Pedidos.Endpoints/CreatePedido/   → src/Pedidos/Pedidos.API/CreatePedido/
src/Pedidos/Pedidos.Endpoints/GetPedido/      → src/Pedidos/Pedidos.API/GetPedido/
src/Pedidos/Pedidos.Endpoints/ListPedidos/    → src/Pedidos/Pedidos.API/ListPedidos/
```

#### P3.2 — Atualizar namespaces dos arquivos movidos

Todos os namespaces do tipo `FacShopAPI.Pedidos.*Feature*` permanecem inalterados — as features Vertical Slice já têm namespaces próprios por feature, não por projeto. Verificar se algum arquivo usa `namespace FacShopAPI.Pedidos.Endpoints.*` e corrigir se necessário.

#### P3.3 — Atualizar `Pedidos.API.csproj`

Adicionar pacotes que estavam em `Pedidos.Endpoints.csproj`:

```xml
<PackageReference Include="FluentValidation" Version="11.10.0" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.10.0" />
```

Remover referência:

```xml
<!-- REMOVER -->
<ProjectReference Include="../Pedidos.Endpoints/Pedidos.Endpoints.csproj" />
```

Garantir que estas estão presentes:

```xml
<ProjectReference Include="../Pedidos.Domain/Pedidos.Domain.csproj" />
<ProjectReference Include="../Pedidos.Infrastructure/Pedidos.Infrastructure.csproj" />
<ProjectReference Include="../../Shared/Web/Shared.Web.csproj" />
<ProjectReference Include="../../Shared/Kernel/Shared.Kernel.csproj" />
```

#### P3.4 — Atualizar `Program.cs`

Os `using` das features devem continuar funcionando — as features usam namespaces por feature (Vertical Slice). Verificar se há `using FacShopAPI.Pedidos.Endpoints.*` e corrigir.

O autodiscovery via `IEndpoint` (reflection) não muda — continua funcionando porque os tipos agora estão no assembly do API.

#### P3.5 — Atualizar `Pedidos.Tests.csproj` (se existir)

```xml
<!-- REMOVER -->
<ProjectReference Include="../Pedidos.Endpoints/Pedidos.Endpoints.csproj" />
```

#### P3.6 — Deletar projeto `Pedidos.Endpoints`

1. Remover pasta `src/Pedidos/Pedidos.Endpoints/`
2. Remover do `FacShop.slnx`

#### P3.7 — Validar final

```bash
dotnet build FacShop.slnx
dotnet test src/Pedidos/Pedidos.Tests/
```

---

### Resultado Final — Pedidos

```
Antes                         Depois
─────────────────────         ─────────────────────
Pedidos.Domain          →     Pedidos.Domain
                                + PedidoResponse (de Common)
                                + IPedidoCommandRepository (de Application)
                                + IPedidoQueryRepository (de Application)
Pedidos.Application     →     (eliminado)
Pedidos.Infrastructure  →     Pedidos.Infrastructure
+ Pedidos.Data          →       + DbContext, DbContextFactory, Migrations
+ Pedidos.Common        →     (eliminado)
Pedidos.Endpoints       →     Pedidos.API
+ Pedidos.API           →       + features verticais
Pedidos.Tests           →     Pedidos.Tests (inalterado)
```

**Dependências resultantes:**

```
Pedidos.API
  ├── Pedidos.Domain
  └── Pedidos.Infrastructure
        └── Pedidos.Domain
```

---

## Resumo Executivo

|                 | Antes  | Depois | Redução  |
| --------------- | :----: | :----: | :------: |
| Catálogo (prod) |   7    |   4    |   -43%   |
| Pedidos (prod)  |   7    |   3    |   -57%   |
| **Total**       | **14** | **7**  | **-50%** |

**Sequência recomendada de execução:**

```
C1 (Data→Infra Catálogo)  →  C2 (Common→Domain Catálogo)  →  C3 (Endpoints→API Catálogo)
         ↓
P1 (Data→Infra Pedidos)   →  P2 (App+Common→Domain Pedidos) →  P3 (Endpoints→API Pedidos)
```

Cada fase termina com `dotnet build` e `dotnet test` antes de avançar.
