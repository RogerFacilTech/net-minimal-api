# Plano — Padronização da Estrutura de Pastas

**Objetivo:** Fazer a estrutura de pastas da solução espelhar o padrão definido em `docs/Estrutura-desejada.md`, tratando Catalogo e Pedidos como **microserviços independentes** que não se referenciam diretamente. Cada bounded context tem seu próprio host, suas próprias camadas e nunca importa tipos do outro contexto. A comunicação futura entre eles ocorre por API HTTP.

**Restrições arquiteturais:**

- `Catalogo.*` não referencia `Pedidos.*` (e vice-versa) — nem mesmo `Catalogo.Domain` em `Pedidos.Domain`
- Cada host (`Catalogo.Host`, `Pedidos.Host`) sobe de forma completamente independente
- O banco SQLite fica em `data/` na raiz da solution, compartilhado pelos hosts em desenvolvimento
- Testes só são executados e corrigidos na fase final (Fase 9), após a migração estrutural estar completa

---

---

## Estado atual → Estado desejado (delta)

| Situação          | Projeto atual                                                | Destino                                                                           |
| ----------------- | ------------------------------------------------------------ | --------------------------------------------------------------------------------- |
| ✅ Concluído      | —                                                            | `src/Catalogo/Catalogo.Common/` (Fase 1 ✅)                                       |
| ✅ Mantém         | `src/Shared/Common/`                                         | `src/Shared/Common/`                                                              |
| ✅ Mantém         | `src/Shared/Data/`                                           | `src/Shared/Data/`                                                                |
| ✅ Mantém         | `src/Catalogo/Catalogo.Domain/`                              | `src/Catalogo/Catalogo.Domain/`                                                   |
| ✅ Mantém         | `src/Catalogo/Catalogo.Application/`                         | `src/Catalogo/Catalogo.Application/`                                              |
| ✅ Mantém         | `src/Catalogo/Catalogo.Infrastructure/`                      | `src/Catalogo/Catalogo.Infrastructure/`                                           |
| ✅ Mantém         | `src/Catalogo/Catalogo.Endpoints/`                           | `src/Catalogo/Catalogo.Endpoints/`                                                |
| ✅ Mantém         | `samples/Catalogo.HttpClientDemo/`                           | `samples/Catalogo.HttpClientDemo/`                                                |
| 🔀 Formalizar     | `src/Catalogo/Catalogo.Host/` (disco, fora do `.slnx`)       | `src/Catalogo/Catalogo.Host/` — adicionar ao slnx; Program.cs **apenas Catalogo** |
| 🔀 Eliminar       | `src/Host/`                                                  | Removido depois que Catalogo.Host for o host ativo                                |
| 🔀 Criar pasta    | `data/` na raiz                                              | SQLite compartilhado entre Catalogo.Host e Pedidos.Host                           |
| ⚠️ Quebrar        | `src/Pedidos/Pedidos.Domain/` → referencia `Catalogo.Domain` | Criar `ProdutoSnapshot` local em `Pedidos.Domain`; remover dependência            |
| 🔀 Renomear pasta | `src/Pedidos/Domain/`                                        | `src/Pedidos/Pedidos.Domain/` (csproj já tem o nome certo)                        |
| 🔀 Extrair        | `src/Pedidos/Pedidos.csproj` (monólito)                      | `src/Pedidos/Pedidos.Application/`                                                |
| 🔀 Extrair        | `src/Pedidos/Pedidos.csproj` (monólito)                      | `src/Pedidos/Pedidos.Endpoints/`                                                  |
| 🔀 Extrair        | `src/Pedidos/Pedidos.csproj` (monólito)                      | `src/Pedidos/Pedidos.Infrastructure/`                                             |
| 🆕 Criar          | —                                                            | `src/Pedidos/Pedidos.Common/`                                                     |
| 🆕 Criar          | —                                                            | `src/Pedidos/Pedidos.Host/` (independente, sem referência a Catalogo)             |
| 🔀 Mover          | `src/Pix/Pix.MockServer/`                                    | `samples/Pix/Pix.MockServer/`                                                     |
| 🔀 Mover          | `src/Pix/Pix.ClientDemo/`                                    | `samples/Pix/Pix.ClientDemo/`                                                     |
| 🔄 Final          | —                                                            | Corrigir e executar todos os testes                                               |

---

## Fase 1 — Catalogo.Common ✅ CONCLUÍDA

Projeto `src/Catalogo/Catalogo.Common/Catalogo.Common.csproj` criado e adicionado ao `FacShopAPI.slnx`.

---

## Fase 2 — Catalogo.Host: formalizar na solução

**Por que:** `src/Catalogo/Catalogo.Host/` já tem todos os arquivos no disco mas não está no `.slnx` e o `Program.cs` existente só registra o Catálogo — **correto para microserviço independente**. Apenas adicionar à solução.

**O que NÃO fazer:** não referenciar `Pedidos.*` aqui. Catalogo.Host hospeda apenas endpoints do Catálogo.

### Passos

1. Adicionar `src/Catalogo/Catalogo.Host/Catalogo.Host.csproj` ao `FacShopAPI.slnx` na pasta `/Catalogo/`.
2. Remover a pasta `/Host/` do `FacShopAPI.slnx` e o projeto `src/Host/` da solução (o `src/Host/` permanece no disco até ser substituído na Fase 6).
3. Atualizar `CLAUDE.md`: substituir a referência ao host principal de `src/Host/` para `src/Catalogo/Catalogo.Host/`.

**Critério de conclusão:** `dotnet build src/Catalogo/Catalogo.Host` compilando sem erros.

---

## Fase 3 — Banco de dados em pasta `data/` compartilhada

**Por que:** Cada host tem hoje o SQLite na sua própria pasta (`produtos-api.db` relativo ao diretório de execução). Com dois hosts independentes, ambos precisam apontar para o mesmo arquivo em desenvolvimento.

### Passos

1. Criar `data/` na raiz da solution (`.gitkeep` para versionar a pasta).
2. Atualizar `src/Catalogo/Catalogo.Host/appsettings.json`:
    ```json
    "DefaultConnection": "Data Source=../../../data/facshop.db"
    ```
    _(relativo ao working dir `src/Catalogo/Catalogo.Host/` quando rodado com `dotnet run`)_
3. Quando `Pedidos.Host` for criado (Fase 7), usar o mesmo caminho relativo `../../../data/facshop.db`.
4. Adicionar `data/*.db` e `data/*.db-*` ao `.gitignore`.

**Critério de conclusão:** `dotnet run --project src/Catalogo/Catalogo.Host` cria o banco em `data/`.

---

## Fase 4 — Quebrar o acoplamento Pedidos → Catalogo.Domain

**Por que:** Este é o acoplamento mais profundo e deve ser resolvido antes de decompor o monólito de Pedidos. Atualmente:

- `Pedidos.Domain.Pedido.AdicionarItem(Produto produto, ...)` recebe `Catalogo.Domain.Produto`
- `Pedidos.Domain.PedidoItem.Criar(Produto produto, ...)` lê `produto.Id`, `produto.Nome`, `produto.Preco.Value`
- `Pedidos.Repositories.IPedidoCommandRepository.ObterProdutoParaItemAsync()` retorna `Catalogo.Domain.Produto`
- `Pedidos.Infrastructure.PedidoCommandRepository` carrega `Produto` via EF (tabela compartilhada)

**Solução:** criar um value object `ProdutoSnapshot` no domínio de Pedidos que representa o produto **no momento da inclusão no pedido** — um snapshot imutável com apenas os dados relevantes.

### Passos

1. Criar `src/Pedidos/Domain/ProdutoSnapshot.cs`:

    ```csharp
    namespace ProdutosAPI.Pedidos.Domain;
    public record ProdutoSnapshot(int Id, string Nome, decimal Preco);
    ```

2. Atualizar `PedidoItem.cs`: substituir `Produto` por `ProdutoSnapshot` em `Criar()`.

3. Atualizar `Pedido.cs`: substituir `Produto` por `ProdutoSnapshot` em `AdicionarItem()`.

4. Atualizar `IPedidoCommandRepository.cs`: `ObterProdutoParaItemAsync` retorna `ProdutoSnapshot?`.

5. Atualizar `PedidoCommandRepository.cs`: projetar de `AppDbContext.Produtos` para `ProdutoSnapshot` (query EF com `Select`), sem importar `Catalogo.Domain`.

6. Remover `using ProdutosAPI.Catalogo.Domain` de todos os arquivos de Pedidos.

7. Remover `<ProjectReference>` para `Catalogo.Domain.csproj` do `Pedidos.csproj`.

**Critério de conclusão:** `dotnet build src/Pedidos/Pedidos.csproj` compila sem `using ProdutosAPI.Catalogo.Domain`.

---

## Fase 5 — Renomear pasta `Domain/` → `Pedidos.Domain/`

**Por que:** Padronizar nomenclatura da pasta com o nome do projeto (o `.csproj` já se chama `Pedidos.Domain.csproj`).

### Passos

1. `git mv src/Pedidos/Domain src/Pedidos/Pedidos.Domain`
2. Atualizar `<Compile Remove="Domain/**" />` no `Pedidos.csproj` para `Pedidos.Domain/**`.
3. Atualizar `<ProjectReference>` no `Pedidos.csproj`: `Domain/Pedidos.Domain.csproj` → `Pedidos.Domain/Pedidos.Domain.csproj`.
4. Atualizar path no `FacShopAPI.slnx`: `src/Pedidos/Domain/Pedidos.Domain.csproj` → `src/Pedidos/Pedidos.Domain/Pedidos.Domain.csproj`.

**Critério de conclusão:** `dotnet sln FacShopAPI.slnx list` mostra o caminho atualizado; `dotnet build` limpo.

---

## Fase 6 — Decompor o monólito `Pedidos.csproj`

**Por que:** O único projeto `Pedidos.csproj` mistura Application, Endpoints e Infrastructure. Cada camada deve ser um projeto separado.

### Mapa de extração

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

### Grafo de dependências dos novos projetos

```
Pedidos.Domain
  └── Pedidos.Common (→ Pedidos.Domain, Shared.Common)
        └── Pedidos.Application (→ Pedidos.Common, Shared.Data)
              ├── Pedidos.Infrastructure (→ Pedidos.Application, Shared.Data)
              └── Pedidos.Endpoints (→ Pedidos.Application, Pedidos.Common, Shared.Common)
```

Nenhum desses projetos referencia nada de `Catalogo.*`.

### Passos

1. Criar os quatro `.csproj` acima com as referências corretas.
2. Mover os arquivos conforme o mapa.
3. Ajustar namespaces se necessário.
4. Remover `Pedidos.csproj` da solução e do disco.
5. Atualizar `FacShopAPI.slnx`.
6. Atualizar `src/Host/Host.csproj` para referenciar os novos projetos (temporariamente, até Fase 7 e 8).

**Critério de conclusão:** `dotnet build FacShopAPI.slnx` compila sem erros; `Pedidos.csproj` removido.

---

## Fase 7 — Criar Pedidos.Host

**Por que:** Pedidos precisa de seu próprio host para ser executado de forma completamente independente.

### Passos

1. Criar `src/Pedidos/Pedidos.Host/Pedidos.Host.csproj`:
    - Referencia: `Pedidos.Endpoints`, `Pedidos.Infrastructure`, `Shared.Common`, `Shared.Data`
    - Nenhuma referência a `Catalogo.*`

2. Criar `Program.cs` do `Pedidos.Host`:
    - Registra JWT, Swagger, CORS, Serilog
    - `builder.Services.AddEndpointsFromAssembly(typeof(PedidosEndpointMarker).Assembly)`
    - Registra repositórios e handlers de Pedidos
    - `appsettings.json` com `DefaultConnection` apontando para `../../../data/facshop.db`

3. Adicionar ao `FacShopAPI.slnx` na pasta `/Pedidos/`.

**Critério de conclusão:** `dotnet run --project src/Pedidos/Pedidos.Host` sobe API de Pedidos na porta configurada.

---

## Fase 8 — Remover `src/Host/` e tornar `Catalogo.Host` o host padrão

**Por que:** Com Catalogo.Host e Pedidos.Host funcionando, `src/Host/` é redundante.

### Passos

1. Verificar que `Catalogo.Host` está completo (registra apenas endpoints do Catálogo + Auth).
2. Remover `src/Host/` da solução (`FacShopAPI.slnx`).
3. Apagar `src/Host/` do disco (`git rm -r src/Host/`).

**Critério de conclusão:** `src/Host/` removido; `dotnet build FacShopAPI.slnx` limpo.

---

## Fase 9 — Mover Pix para `samples/`

**Por que:** Pix é material de demonstração, não bounded context de produção.

### Passos

1. `git mv src/Pix/Pix.MockServer samples/Pix/Pix.MockServer`
2. `git mv src/Pix/Pix.ClientDemo samples/Pix/Pix.ClientDemo`
3. Atualizar caminhos relativos nos `.csproj` movidos.
4. Atualizar `FacShopAPI.slnx`: remover pasta `/Pix/`, adicionar projetos Pix à pasta `/Samples/`.
5. Remover `src/Pix/` (ficará vazia).

**Critério de conclusão:** `src/Pix/` removido; solução lista Pix em `/Samples/`.

---

## Fase 10 — Testes: migrar e validar ✅ PASSO FINAL

**Por que:** Somente após a estrutura estar completamente migrada os testes de integração são ajustados.

### Estado atual dos projetos de teste

| Projeto                       | Estado                                                                                                    |
| ----------------------------- | --------------------------------------------------------------------------------------------------------- |
| `tests/Catalogo.Tests/`       | ✅ Compila; `ApiFactory` usa `WebApplicationFactory<Program>` de `Catalogo.Host`                          |
| `tests/Pedidos.Tests/`        | ⚠️ Compila; `PedidosApiFactory` é stub sem `WebApplicationFactory`; testes de integração são placeholders |
| `tests/Pix.MockServer.Tests/` | ✅ Compila; 7/7 testes passam                                                                             |

### O que ainda precisa ser feito

1. **Remover pasta `/Pix/` vazia do `FacShopAPI.slnx`** — ficou após a migração dos projetos Pix para `/Samples/`.

2. **Implementar `PedidosApiFactory` real** em `tests/Pedidos.Tests/Integration/PedidosApiFactory.cs`:
    - Trocar `IAsyncLifetime` por `WebApplicationFactory<Program>` (referenciando `Pedidos.Host`)
    - Configurar `UseEnvironment("Testing")` e banco InMemory/SQLite temporário
    - Semear dados de teste necessários

3. **Implementar os testes de integração de Pedidos** (todos os arquivos em `Integration/` e `Endpoints/` são placeholders com valores hardcoded — não testam a API de fato).

4. **Rodar `dotnet test` completo** e corrigir o que quebrou:
    ```bash
    dotnet test FacShopAPI.slnx --nologo
    ```

### Observação sobre a estrutura final dos samples Pix

Os projetos Pix foram movidos para `samples/Pix.MockServer/` e `samples/Pix.ClientDemo/` **sem** subpasta `Pix/` intermediária. A estrutura final real difere ligeiramente do que estava planejado — a seção "Estrutura final esperada" abaixo já reflete a realidade.

**Critério de conclusão:** todos os testes passam (`dotnet test FacShopAPI.slnx`).

---

## Ordem de execução

```
Fase 1 ✅ → Fase 2 → Fase 3 → Fase 4 → Fase 5 → Fase 6 → Fase 7 → Fase 8 → Fase 9 → Fase 10
            (slnx)  (data/)  (desac.) (pasta)  (split) (host)  (rm Host) (pix)  (testes)
```

Fase 9 (Pix) é independente e pode ser feita em qualquer momento entre Fase 2 e Fase 10.

---

## Estrutura final esperada na solução

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
