# Plano â€” PadronizaÃ§Ã£o da Estrutura de Pastas

**Objetivo:** Fazer a estrutura de pastas da soluÃ§Ã£o espelhar o padrÃ£o definido em `docs/Estrutura-desejada.md`, tratando Catalogo e Pedidos como **microserviÃ§os independentes** que nÃ£o se referenciam diretamente. Cada bounded context tem seu prÃ³prio host, suas prÃ³prias camadas e nunca importa tipos do outro contexto. A comunicaÃ§Ã£o futura entre eles ocorre por API HTTP.

**RestriÃ§Ãµes arquiteturais:**

- `Catalogo.*` nÃ£o referencia `Pedidos.*` (e vice-versa) â€” nem mesmo `Catalogo.Domain` em `Pedidos.Domain`
- Cada host (`Catalogo.Host`, `Pedidos.Host`) sobe de forma completamente independente
- O banco SQLite fica em `data/` na raiz da solution, compartilhado pelos hosts em desenvolvimento
- Testes sÃ³ sÃ£o executados e corrigidos na fase final (Fase 9), apÃ³s a migraÃ§Ã£o estrutural estar completa

---

---

## Estado atual â†’ Estado desejado (delta)

| SituaÃ§Ã£o          | Projeto atual                                                | Destino                                                                           |
| ----------------- | ------------------------------------------------------------ | --------------------------------------------------------------------------------- |
| âœ… ConcluÃ­do      | â€”                                                            | `src/Catalogo/Catalogo.Common/` (Fase 1 âœ…)                                       |
| âœ… MantÃ©m         | `src/Shared/Common/`                                         | `src/Shared/Common/`                                                              |
| âœ… MantÃ©m         | `src/Shared/Data/`                                           | `src/Shared/Data/`                                                                |
| âœ… MantÃ©m         | `src/Catalogo/Catalogo.Domain/`                              | `src/Catalogo/Catalogo.Domain/`                                                   |
| âœ… MantÃ©m         | `src/Catalogo/Catalogo.Application/`                         | `src/Catalogo/Catalogo.Application/`                                              |
| âœ… MantÃ©m         | `src/Catalogo/Catalogo.Infrastructure/`                      | `src/Catalogo/Catalogo.Infrastructure/`                                           |
| âœ… MantÃ©m         | `src/Catalogo/Catalogo.Endpoints/`                           | `src/Catalogo/Catalogo.Endpoints/`                                                |
| âœ… MantÃ©m         | `samples/Catalogo.HttpClientDemo/`                           | `samples/Catalogo.HttpClientDemo/`                                                |
| ðŸ”€ Formalizar     | `src/Catalogo/Catalogo.Host/` (disco, fora do `.slnx`)       | `src/Catalogo/Catalogo.Host/` â€” adicionar ao slnx; Program.cs **apenas Catalogo** |
| ðŸ”€ Eliminar       | `src/Host/`                                                  | Removido depois que Catalogo.Host for o host ativo                                |
| ðŸ”€ Criar pasta    | `data/` na raiz                                              | SQLite compartilhado entre Catalogo.Host e Pedidos.Host                           |
| âš ï¸ Quebrar        | `src/Pedidos/Pedidos.Domain/` â†’ referencia `Catalogo.Domain` | Criar `ProdutoSnapshot` local em `Pedidos.Domain`; remover dependÃªncia            |
| ðŸ”€ Renomear pasta | `src/Pedidos/Domain/`                                        | `src/Pedidos/Pedidos.Domain/` (csproj jÃ¡ tem o nome certo)                        |
| ðŸ”€ Extrair        | `src/Pedidos/Pedidos.csproj` (monÃ³lito)                      | `src/Pedidos/Pedidos.Application/`                                                |
| ðŸ”€ Extrair        | `src/Pedidos/Pedidos.csproj` (monÃ³lito)                      | `src/Pedidos/Pedidos.Endpoints/`                                                  |
| ðŸ”€ Extrair        | `src/Pedidos/Pedidos.csproj` (monÃ³lito)                      | `src/Pedidos/Pedidos.Infrastructure/`                                             |
| ðŸ†• Criar          | â€”                                                            | `src/Pedidos/Pedidos.Common/`                                                     |
| ðŸ†• Criar          | â€”                                                            | `src/Pedidos/Pedidos.Host/` (independente, sem referÃªncia a Catalogo)             |
| ðŸ”€ Mover          | `src/Pix/Pix.MockServer/`                                    | `samples/Pix/Pix.MockServer/`                                                     |
| ðŸ”€ Mover          | `src/Pix/Pix.ClientDemo/`                                    | `samples/Pix/Pix.ClientDemo/`                                                     |
| ðŸ”„ Final          | â€”                                                            | Corrigir e executar todos os testes                                               |

---

## Fase 1 â€” Catalogo.Common âœ… CONCLUÃDA

Projeto `src/Catalogo/Catalogo.Common/Catalogo.Common.csproj` criado e adicionado ao `FacShopAPI.slnx`.

---

## Fase 2 â€” Catalogo.Host: formalizar na soluÃ§Ã£o

**Por que:** `src/Catalogo/Catalogo.Host/` jÃ¡ tem todos os arquivos no disco mas nÃ£o estÃ¡ no `.slnx` e o `Program.cs` existente sÃ³ registra o CatÃ¡logo â€” **correto para microserviÃ§o independente**. Apenas adicionar Ã  soluÃ§Ã£o.

**O que NÃƒO fazer:** nÃ£o referenciar `Pedidos.*` aqui. Catalogo.Host hospeda apenas endpoints do CatÃ¡logo.

### Passos

1. Adicionar `src/Catalogo/Catalogo.Host/Catalogo.Host.csproj` ao `FacShopAPI.slnx` na pasta `/Catalogo/`.
2. Remover a pasta `/Host/` do `FacShopAPI.slnx` e o projeto `src/Host/` da soluÃ§Ã£o (o `src/Host/` permanece no disco atÃ© ser substituÃ­do na Fase 6).
3. Atualizar `CLAUDE.md`: substituir a referÃªncia ao host principal de `src/Host/` para `src/Catalogo/Catalogo.Host/`.

**CritÃ©rio de conclusÃ£o:** `dotnet build src/Catalogo/Catalogo.Host` compilando sem erros.

---

## Fase 3 â€” Banco de dados em pasta `data/` compartilhada

**Por que:** Cada host tem hoje o SQLite na sua prÃ³pria pasta (`produtos-api.db` relativo ao diretÃ³rio de execuÃ§Ã£o). Com dois hosts independentes, ambos precisam apontar para o mesmo arquivo em desenvolvimento.

### Passos

1. Criar `data/` na raiz da solution (`.gitkeep` para versionar a pasta).
2. Atualizar `src/Catalogo/Catalogo.Host/appsettings.json`:
    ```json
    "DefaultConnection": "Data Source=../../../data/facshop.db"
    ```
    _(relativo ao working dir `src/Catalogo/Catalogo.Host/` quando rodado com `dotnet run`)_
3. Quando `Pedidos.Host` for criado (Fase 7), usar o mesmo caminho relativo `../../../data/facshop.db`.
4. Adicionar `data/*.db` e `data/*.db-*` ao `.gitignore`.

**CritÃ©rio de conclusÃ£o:** `dotnet run --project src/Catalogo/Catalogo.Host` cria o banco em `data/`.

---

## Fase 4 â€” Quebrar o acoplamento Pedidos â†’ Catalogo.Domain

**Por que:** Este Ã© o acoplamento mais profundo e deve ser resolvido antes de decompor o monÃ³lito de Pedidos. Atualmente:

- `Pedidos.Domain.Pedido.AdicionarItem(Produto produto, ...)` recebe `Catalogo.Domain.Produto`
- `Pedidos.Domain.PedidoItem.Criar(Produto produto, ...)` lÃª `produto.Id`, `produto.Nome`, `produto.Preco.Value`
- `Pedidos.Repositories.IPedidoCommandRepository.ObterProdutoParaItemAsync()` retorna `Catalogo.Domain.Produto`
- `Pedidos.Infrastructure.PedidoCommandRepository` carrega `Produto` via EF (tabela compartilhada)

**SoluÃ§Ã£o:** criar um value object `ProdutoSnapshot` no domÃ­nio de Pedidos que representa o produto **no momento da inclusÃ£o no pedido** â€” um snapshot imutÃ¡vel com apenas os dados relevantes.

### Passos

1. Criar `src/Pedidos/Domain/ProdutoSnapshot.cs`:

    ```csharp
    namespace FacShopAPI.Pedidos.Domain;
    public record ProdutoSnapshot(int Id, string Nome, decimal Preco);
    ```

2. Atualizar `PedidoItem.cs`: substituir `Produto` por `ProdutoSnapshot` em `Criar()`.

3. Atualizar `Pedido.cs`: substituir `Produto` por `ProdutoSnapshot` em `AdicionarItem()`.

4. Atualizar `IPedidoCommandRepository.cs`: `ObterProdutoParaItemAsync` retorna `ProdutoSnapshot?`.

5. Atualizar `PedidoCommandRepository.cs`: projetar de `AppDbContext.Produtos` para `ProdutoSnapshot` (query EF com `Select`), sem importar `Catalogo.Domain`.

6. Remover `using FacShopAPI.Catalogo.Domain` de todos os arquivos de Pedidos.

7. Remover `<ProjectReference>` para `Catalogo.Domain.csproj` do `Pedidos.csproj`.

**CritÃ©rio de conclusÃ£o:** `dotnet build src/Pedidos/Pedidos.csproj` compila sem `using FacShopAPI.Catalogo.Domain`.

---

## Fase 5 â€” Renomear pasta `Domain/` â†’ `Pedidos.Domain/`

**Por que:** Padronizar nomenclatura da pasta com o nome do projeto (o `.csproj` jÃ¡ se chama `Pedidos.Domain.csproj`).

### Passos

1. `git mv src/Pedidos/Domain src/Pedidos/Pedidos.Domain`
2. Atualizar `<Compile Remove="Domain/**" />` no `Pedidos.csproj` para `Pedidos.Domain/**`.
3. Atualizar `<ProjectReference>` no `Pedidos.csproj`: `Domain/Pedidos.Domain.csproj` â†’ `Pedidos.Domain/Pedidos.Domain.csproj`.
4. Atualizar path no `FacShopAPI.slnx`: `src/Pedidos/Domain/Pedidos.Domain.csproj` â†’ `src/Pedidos/Pedidos.Domain/Pedidos.Domain.csproj`.

**CritÃ©rio de conclusÃ£o:** `dotnet sln FacShopAPI.slnx list` mostra o caminho atualizado; `dotnet build` limpo.

---

## Fase 6 â€” Decompor o monÃ³lito `Pedidos.csproj`

**Por que:** O Ãºnico projeto `Pedidos.csproj` mistura Application, Endpoints e Infrastructure. Cada camada deve ser um projeto separado.

### Mapa de extraÃ§Ã£o

| Arquivo atual                                          | Projeto destino           |
| ------------------------------------------------------ | ------------------------- |
| `Common/PedidoResponse.cs`                             | `Pedidos.Common/`         |
| `Repositories/IPedido*.cs`                             | `Pedidos.Application/`    |
| `CreatePedido/CreatePedidoCommand.cs`, `*Validator.cs` | `Pedidos.Application/`    |
| `GetPedido/GetPedidoQuery.cs`                          | `Pedidos.Application/`    |
| `ListPedidos/ListPedidosQuery.cs`                      | `Pedidos.Application/`    |
| `AddItemPedido/AddItemCommand.cs`, `*Validator.cs`     | `Pedidos.Application/`    |
| `CancelPedido/CancelPedidoCommand.cs`                  | `Pedidos.Application/`    |
| `*Endpoint.cs` (todos os slices)                       | `Pedidos.Endpoints/`      |
| `Infrastructure/PedidoCommandRepository.cs`            | `Pedidos.Infrastructure/` |
| `Infrastructure/PedidoQueryRepository.cs`              | `Pedidos.Infrastructure/` |

### Grafo de dependÃªncias dos novos projetos

```
Pedidos.Domain
  â””â”€â”€ Pedidos.Common (â†’ Pedidos.Domain, Shared.Common)
        â””â”€â”€ Pedidos.Application (â†’ Pedidos.Common, Shared.Data)
              â”œâ”€â”€ Pedidos.Infrastructure (â†’ Pedidos.Application, Shared.Data)
              â””â”€â”€ Pedidos.Endpoints (â†’ Pedidos.Application, Pedidos.Common, Shared.Common)
```

Nenhum desses projetos referencia nada de `Catalogo.*`.

### Passos

1. Criar os quatro `.csproj` acima com as referÃªncias corretas.
2. Mover os arquivos conforme o mapa.
3. Ajustar namespaces se necessÃ¡rio.
4. Remover `Pedidos.csproj` da soluÃ§Ã£o e do disco.
5. Atualizar `FacShopAPI.slnx`.
6. Atualizar `src/Host/Host.csproj` para referenciar os novos projetos (temporariamente, atÃ© Fase 7 e 8).

**CritÃ©rio de conclusÃ£o:** `dotnet build FacShopAPI.slnx` compila sem erros; `Pedidos.csproj` removido.

---

## Fase 7 â€” Criar Pedidos.Host

**Por que:** Pedidos precisa de seu prÃ³prio host para ser executado de forma completamente independente.

### Passos

1. Criar `src/Pedidos/Pedidos.Host/Pedidos.Host.csproj`:
    - Referencia: `Pedidos.Endpoints`, `Pedidos.Infrastructure`, `Shared.Common`, `Shared.Data`
    - Nenhuma referÃªncia a `Catalogo.*`

2. Criar `Program.cs` do `Pedidos.Host`:
    - Registra JWT, Swagger, CORS, Serilog
    - `builder.Services.AddEndpointsFromAssembly(typeof(PedidosEndpointMarker).Assembly)`
    - Registra repositÃ³rios e handlers de Pedidos
    - `appsettings.json` com `DefaultConnection` apontando para `../../../data/facshop.db`

3. Adicionar ao `FacShopAPI.slnx` na pasta `/Pedidos/`.

**CritÃ©rio de conclusÃ£o:** `dotnet run --project src/Pedidos/Pedidos.Host` sobe API de Pedidos na porta configurada.

---

## Fase 8 â€” Remover `src/Host/` e tornar `Catalogo.Host` o host padrÃ£o

**Por que:** Com Catalogo.Host e Pedidos.Host funcionando, `src/Host/` Ã© redundante.

### Passos

1. Verificar que `Catalogo.Host` estÃ¡ completo (registra apenas endpoints do CatÃ¡logo + Auth).
2. Remover `src/Host/` da soluÃ§Ã£o (`FacShopAPI.slnx`).
3. Apagar `src/Host/` do disco (`git rm -r src/Host/`).

**CritÃ©rio de conclusÃ£o:** `src/Host/` removido; `dotnet build FacShopAPI.slnx` limpo.

---

## Fase 9 â€” Mover Pix para `samples/`

**Por que:** Pix Ã© material de demonstraÃ§Ã£o, nÃ£o bounded context de produÃ§Ã£o.

### Passos

1. `git mv src/Pix/Pix.MockServer samples/Pix/Pix.MockServer`
2. `git mv src/Pix/Pix.ClientDemo samples/Pix/Pix.ClientDemo`
3. Atualizar caminhos relativos nos `.csproj` movidos.
4. Atualizar `FacShopAPI.slnx`: remover pasta `/Pix/`, adicionar projetos Pix Ã  pasta `/Samples/`.
5. Remover `src/Pix/` (ficarÃ¡ vazia).

**CritÃ©rio de conclusÃ£o:** `src/Pix/` removido; soluÃ§Ã£o lista Pix em `/Samples/`.

---

## Fase 10 â€” Testes: migrar e validar âœ… PASSO FINAL

**Por que:** Somente apÃ³s a estrutura estar completamente migrada os testes de integraÃ§Ã£o sÃ£o ajustados.

### Estado atual dos projetos de teste

| Projeto                       | Estado                                                                                                    |
| ----------------------------- | --------------------------------------------------------------------------------------------------------- |
| `tests/Catalogo.Tests/`       | âœ… Compila; `ApiFactory` usa `WebApplicationFactory<Program>` de `Catalogo.Host`                          |
| `tests/Pedidos.Tests/`        | âš ï¸ Compila; `PedidosApiFactory` Ã© stub sem `WebApplicationFactory`; testes de integraÃ§Ã£o sÃ£o placeholders |
| `tests/Pix.MockServer.Tests/` | âœ… Compila; 7/7 testes passam                                                                             |

### O que ainda precisa ser feito

1. **Remover pasta `/Pix/` vazia do `FacShopAPI.slnx`** â€” ficou apÃ³s a migraÃ§Ã£o dos projetos Pix para `/Samples/`.

2. **Implementar `PedidosApiFactory` real** em `tests/Pedidos.Tests/Integration/PedidosApiFactory.cs`:
    - Trocar `IAsyncLifetime` por `WebApplicationFactory<Program>` (referenciando `Pedidos.Host`)
    - Configurar `UseEnvironment("Testing")` e banco InMemory/SQLite temporÃ¡rio
    - Semear dados de teste necessÃ¡rios

3. **Implementar os testes de integraÃ§Ã£o de Pedidos** (todos os arquivos em `Integration/` e `Endpoints/` sÃ£o placeholders com valores hardcoded â€” nÃ£o testam a API de fato).

4. **Rodar `dotnet test` completo** e corrigir o que quebrou:
    ```bash
    dotnet test FacShopAPI.slnx --nologo
    ```

### ObservaÃ§Ã£o sobre a estrutura final dos samples Pix

Os projetos Pix foram movidos para `samples/Pix.MockServer/` e `samples/Pix.ClientDemo/` **sem** subpasta `Pix/` intermediÃ¡ria. A estrutura final real difere ligeiramente do que estava planejado â€” a seÃ§Ã£o "Estrutura final esperada" abaixo jÃ¡ reflete a realidade.

**CritÃ©rio de conclusÃ£o:** todos os testes passam (`dotnet test FacShopAPI.slnx`).

---

## Ordem de execuÃ§Ã£o

```
Fase 1 âœ… â†’ Fase 2 â†’ Fase 3 â†’ Fase 4 â†’ Fase 5 â†’ Fase 6 â†’ Fase 7 â†’ Fase 8 â†’ Fase 9 â†’ Fase 10
            (slnx)  (data/)  (desac.) (pasta)  (split) (host)  (rm Host) (pix)  (testes)
```

Fase 9 (Pix) Ã© independente e pode ser feita em qualquer momento entre Fase 2 e Fase 10.

---

## Estrutura final esperada na soluÃ§Ã£o

```xml
<Solution>
  <Folder Name="/Shared/">
    <Project Path="src/Shared/Common/Shared.Common.csproj" />
    <Project Path="src/Shared/Data/Shared.Data.csproj" />
  </Folder>
  <Folder Name="/Catalogo/">
    <Project Path="src/Catalogo/Catalogo.Common/Catalogo.Common.csproj" />
    <Project Path="src/Catalogo/Catalogo.Domain/Catalogo.Domain.csproj" />
    <Project Path="src/Catalogo/Catalogo.Application/Catalogo.Application.csproj" />
    <Project Path="src/Catalogo/Catalogo.Infrastructure/Catalogo.Infrastructure.csproj" />
    <Project Path="src/Catalogo/Catalogo.Endpoints/Catalogo.Endpoints.csproj" />
    <Project Path="src/Catalogo/Catalogo.Host/Catalogo.Host.csproj" />
  </Folder>
  <Folder Name="/Pedidos/">
    <Project Path="src/Pedidos/Pedidos.Common/Pedidos.Common.csproj" />
    <Project Path="src/Pedidos/Pedidos.Domain/Pedidos.Domain.csproj" />
    <Project Path="src/Pedidos/Pedidos.Application/Pedidos.Application.csproj" />
    <Project Path="src/Pedidos/Pedidos.Infrastructure/Pedidos.Infrastructure.csproj" />
    <Project Path="src/Pedidos/Pedidos.Endpoints/Pedidos.Endpoints.csproj" />
    <Project Path="src/Pedidos/Pedidos.Host/Pedidos.Host.csproj" />
  </Folder>
  <Folder Name="/Samples/">
    <Project Path="samples/Catalogo.HttpClientDemo/Catalogo.HttpClientDemo.csproj" />
    <Project Path="samples/Pix.MockServer/Pix.MockServer.csproj" />
    <Project Path="samples/Pix.ClientDemo/Pix.ClientDemo.csproj" />
  </Folder>
  <Folder Name="/Tests/">
    <Project Path="tests/Catalogo.Tests/Catalogo.Tests.csproj" />
    <Project Path="tests/Pedidos.Tests/Pedidos.Tests.csproj" />
    <Project Path="tests/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj" />
  </Folder>
</Solution>
```
