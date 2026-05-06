# CQRS Repositories — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Validar e integrar a segregação de repositórios CQRS (Dapper para leitura, EF Core para escrita) em Produtos e Pedidos, mesclando a feature branch `feature/cqrs-repositories` no `main`.

**Architecture:** A feature branch já contém 3 commits com a implementação completa: `IProdutoQueryRepository`/`IProdutoCommandRepository` para Produtos e `IPedidoQueryRepository`/`IPedidoCommandRepository` para Pedidos. Todos os handlers de Pedidos e o `ProdutoService` já foram atualizados para usar os novos repositórios. O plano é verificar, corrigir eventuais falhas e fazer o merge.

**Tech Stack:** .NET 10, EF Core (writes + tracking), Dapper (reads + raw SQL), xUnit + WebApplicationFactory (testes de integração com InMemory SQLite).

**Worktree:** `.worktrees/feature/cqrs-repositories` — execute todos os comandos de build/test a partir desse diretório.

---

## Mapa de Arquivos

### Arquivos criados na feature branch (não existem no main)

| Arquivo                                                                             | Responsabilidade                                                                               |
| ----------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| `src/Produtos/Produtos.Application/Repositories/IProdutoQueryRepository.cs`         | Interface de leitura — `ObterPorIdAsync`, `ListarAsync` → `ProdutoResponse`                    |
| `src/Produtos/Produtos.Application/Repositories/IProdutoCommandRepository.cs`       | Interface de escrita — `ObterPorIdAsync`, `AdicionarAsync`, `DeletarAsync`, `SaveChangesAsync` |
| `src/Produtos/Produtos.Infrastructure/Repositories/DapperProdutoQueryRepository.cs` | Dapper, mapeia raw SQL → `ProdutoResponse` inline                                              |
| `src/Produtos/Produtos.Infrastructure/Repositories/EfProdutoCommandRepository.cs`   | EF Core tracking via `IProdutoContext`                                                         |
| `src/Pedidos/Repositories/IPedidoQueryRepository.cs`                                | Interface de leitura de Pedidos                                                                |
| `src/Pedidos/Repositories/IPedidoCommandRepository.cs`                              | Interface de escrita de Pedidos (inclui `ObterProdutoParaItemAsync`)                           |
| `src/Pedidos/Infrastructure/PedidoQueryRepository.cs`                               | Dapper, mapeia raw SQL → `PedidoResponse`                                                      |
| `src/Pedidos/Infrastructure/PedidoCommandRepository.cs`                             | EF Core direto no `AppDbContext`                                                               |

### Arquivos modificados na feature branch

| Arquivo                                                             | O que mudou                                                                                   |
| ------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| `src/Produtos/Produtos.Application/Services/ProdutoService.cs`      | Injeta `IProdutoQueryRepository` + `IProdutoCommandRepository` em vez de `IProdutoRepository` |
| `src/Produtos/Produtos.API/Extensions/ProdutosServiceExtensions.cs` | Registra os dois novos repositórios no DI; remove `IProdutoRepository`                        |
| `src/Pedidos/CreatePedido/CreatePedidoCommand.cs`                   | `CreatePedidoHandler` injeta `IPedidoCommandRepository`                                       |
| `src/Pedidos/GetPedido/GetPedidoQuery.cs`                           | `GetPedidoHandler` injeta `IPedidoQueryRepository`                                            |
| `src/Pedidos/ListPedidos/ListPedidosQuery.cs`                       | `ListPedidosHandler` injeta `IPedidoQueryRepository`                                          |
| `src/Pedidos/AddItemPedido/AddItemCommand.cs`                       | `AddItemHandler` injeta `IPedidoCommandRepository`                                            |
| `src/Pedidos/CancelPedido/CancelPedidoCommand.cs`                   | `CancelPedidoHandler` injeta `IPedidoCommandRepository`                                       |
| `Program.cs`                                                        | Adiciona `AddScoped` para `IPedidoQueryRepository` e `IPedidoCommandRepository`               |

### Arquivos removidos na feature branch

| Arquivo                                                                        | Motivo                                                                        |
| ------------------------------------------------------------------------------ | ----------------------------------------------------------------------------- |
| `src/Produtos/Produtos.Application/Repositories/IProdutoRepository.cs`         | Substituído pelos dois repositórios segregados                                |
| `src/Produtos/Produtos.Infrastructure/Repositories/DapperProdutoRepository.cs` | Substituído por `DapperProdutoQueryRepository` + `EfProdutoCommandRepository` |

---

## Task 1: Verificar compilação na feature branch

**Diretório:** `.worktrees/feature/cqrs-repositories`

- [ ] **Step 1: Rodar build**

```bash
cd /Volumes/Marco-Dev/dev/net-minimal-api/.worktrees/feature/cqrs-repositories
dotnet build FacShopAPI.slnx
```

Esperado:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

- [ ] **Step 2: Se houver erros, identificar o tipo**

Erros comuns e correções:

**"The name 'IProdutoRepository' does not exist"** → algum arquivo ainda referencia a interface antiga. Buscar e substituir:

```bash
grep -rn "IProdutoRepository" src/ --include="*.cs"
```

Substituir cada ocorrência por `IProdutoQueryRepository` (leituras) ou `IProdutoCommandRepository` (escritas) conforme o contexto.

**"DapperProdutoRepository does not exist"** → mesmo padrão:

```bash
grep -rn "DapperProdutoRepository" src/ --include="*.cs"
```

Substituir por `DapperProdutoQueryRepository`.

**"AppDbContext does not contain a definition for 'Pedidos'"** → verificar se `PedidoCommandRepository.cs` está referenciando o DbSet correto (`db.Pedidos`).

- [ ] **Step 3: Reconfirmar build limpo após correções**

```bash
dotnet build FacShopAPI.slnx
```

Esperado: `Build succeeded. 0 Error(s)`

---

## Task 2: Rodar testes de Produtos

**Diretório:** `.worktrees/feature/cqrs-repositories`

- [ ] **Step 1: Rodar testes de Produtos**

```bash
cd /Volumes/Marco-Dev/dev/net-minimal-api/.worktrees/feature/cqrs-repositories
dotnet test tests/ProdutosAPI.Tests/ProdutosAPI.Tests.csproj -v normal
```

Esperado: todos os testes passando. A suíte cobre:

- `ProdutoEndpointsTests` — 23 testes HTTP (GET list, GET by id, POST, PUT, PATCH, DELETE, paginação, filtro por categoria, busca, soft delete, auth)
- `ProdutoValidatorTests` — validações de entrada
- `ProdutoTests` (unitários) — domain model, value objects
- `ResultTests` — Result pattern

- [ ] **Step 2: Se houver falhas, identificar padrão**

**Falha em testes de leitura (GET /produtos):**
Sintoma: `NullReferenceException` ou resultado vazio onde se esperava dados.
Causa provável: `DapperProdutoQueryRepository` não está recebendo a conexão corretamente no banco InMemory de testes.
Verificar: o `ApiFactory` usa `UseInMemoryDatabase` — Dapper precisa de uma conexão real. Checar se `WithConnectionAsync` abre corretamente a conexão do EF InMemory (SQLite in-memory, não EF InMemory provider).

> **Nota:** O banco de testes usa `Microsoft.Data.Sqlite` com `DataSource=:memory:` e `Cache=Shared`, não o provider `UseInMemoryDatabase`. Confirmar em `tests/ProdutosAPI.Tests/Integration/ApiFactory.cs`:

```bash
grep -n "UseInMemory\|UseSqlite\|DataSource" tests/ProdutosAPI.Tests/Integration/ApiFactory.cs
```

Se usar `UseSqlite`, Dapper funciona normalmente. Se usar `UseInMemoryDatabase` puro (sem SQLite), a conexão não existe e Dapper falhará — nesse caso, o `ApiFactory` precisa ser migrado para SQLite in-memory.

**Falha em testes de escrita (POST/PUT/PATCH/DELETE):**
Sintoma: `404` onde se esperava `200/201`, ou entidade não persiste.
Causa provável: `EfProdutoCommandRepository.AdicionarAsync` chama `SaveChangesAsync` internamente; verificar se não está chamando duas vezes (double save).

- [ ] **Step 3: Corrigir falhas encontradas**

Se `ApiFactory` usa `UseInMemoryDatabase` puro, atualizar para SQLite in-memory compartilhado:

Arquivo: `tests/ProdutosAPI.Tests/Integration/ApiFactory.cs`

Localizar o bloco que configura o DbContext nos testes e substituir:

```csharp
// ANTES (se existir):
options.UseInMemoryDatabase("TestDb");

// DEPOIS:
options.UseSqlite("DataSource=:memory:;Cache=Shared");
```

Garantir que a connection string compartilhe a mesma conexão entre o contexto EF e o Dapper (mesmo `DataSource` e `Cache=Shared`).

- [ ] **Step 4: Reconfirmar após correções**

```bash
dotnet test tests/ProdutosAPI.Tests/ProdutosAPI.Tests.csproj -v normal
```

Esperado: todos passando.

- [ ] **Step 5: Commit se houve correções**

```bash
cd /Volumes/Marco-Dev/dev/net-minimal-api/.worktrees/feature/cqrs-repositories
git add tests/ProdutosAPI.Tests/Integration/ApiFactory.cs
git commit -m "fix(tests): migrar ApiFactory para SQLite in-memory para compatibilidade com Dapper"
```

Se não houve correções, pular este step.

---

## Task 3: Rodar testes de Pedidos

**Diretório:** `.worktrees/feature/cqrs-repositories`

- [ ] **Step 1: Rodar testes de Pedidos**

```bash
cd /Volumes/Marco-Dev/dev/net-minimal-api/.worktrees/feature/cqrs-repositories
dotnet test tests/Pedidos.Tests/Pedidos.Tests.csproj -v normal
```

Esperado: todos os testes passando. A suíte cobre:

- `CreatePedidoEndpointTests` — criação com itens válidos, produto inativo, estoque insuficiente
- `GetPedidoEndpointTests` — pedido existente, não encontrado
- `ListPedidosEndpointTests` — listagem com e sem filtro de status
- `AddItemEndpointTests` — adicionar item a pedido em rascunho, pedido confirmado, produto inexistente
- `CancelPedidoEndpointTests` — cancelamento com motivo, pedido já cancelado
- `PedidoTests` (unitários) — FSM de status, regras de negócio
- `PedidosIntegrationTests` / `PedidosWorkflowTests`

- [ ] **Step 2: Se houver falhas, identificar padrão**

**Falha em `GetPedido` ou `ListPedidos` (testes de query):**
Sintoma: retorna `null` ou lista vazia após criar um pedido.
Causa provável: `PedidoQueryRepository` usa Dapper e pode não ver dados persistidos pelo EF no mesmo banco in-memory.
Verificar o `PedidosApiFactory`:

```bash
grep -n "UseInMemory\|UseSqlite\|DataSource" tests/Pedidos.Tests/Integration/PedidosApiFactory.cs
```

Aplicar a mesma correção de SQLite in-memory compartilhado se necessário.

**Falha em `CreatePedido` (produto não encontrado):**
Sintoma: handler retorna erro de produto inexistente mas o seeder deveria ter populado produtos.
Causa provável: `PedidoCommandRepository.ObterProdutoParaItemAsync` busca por `db.Produtos.FindAsync([produtoId])` — confirmar que o seeder popula produtos no banco de testes.

**Falha em `AddItemPedido` com `NullReferenceException`:**
Causa provável: `IPedidoCommandRepository.ObterProdutoParaItemAsync` retornou `null` — produto não existe no banco de testes. Verificar se o `ProdutoTestBuilder` cria produtos com os IDs esperados pelos testes.

- [ ] **Step 3: Corrigir falhas encontradas**

Se `PedidosApiFactory` usa `UseInMemoryDatabase` puro:

Arquivo: `tests/Pedidos.Tests/Integration/PedidosApiFactory.cs`

```csharp
// ANTES (se existir):
options.UseInMemoryDatabase("PedidosTestDb");

// DEPOIS:
options.UseSqlite("DataSource=:memory:;Cache=Shared");
```

- [ ] **Step 4: Reconfirmar após correções**

```bash
dotnet test tests/Pedidos.Tests/Pedidos.Tests.csproj -v normal
```

Esperado: todos passando.

- [ ] **Step 5: Commit se houve correções**

```bash
cd /Volumes/Marco-Dev/dev/net-minimal-api/.worktrees/feature/cqrs-repositories
git add tests/Pedidos.Tests/Integration/PedidosApiFactory.cs
git commit -m "fix(tests): migrar PedidosApiFactory para SQLite in-memory para compatibilidade com Dapper"
```

Se não houve correções, pular este step.

---

## Task 4: Rodar suíte completa

- [ ] **Step 1: Rodar todos os testes juntos**

```bash
cd /Volumes/Marco-Dev/dev/net-minimal-api/.worktrees/feature/cqrs-repositories
dotnet test FacShopAPI.slnx -v minimal
```

Esperado:

```
Passed!  - Failed: 0, Passed: N, Skipped: 0, Total: N
```

- [ ] **Step 2: Anotar o número total de testes**

Copiar a linha de resumo final (ex: `Passed: 47`). Isso serve como baseline para comparar após o merge.

---

## Task 5: Criar Pull Request

- [ ] **Step 1: Confirmar que a branch está atualizada em relação ao main**

```bash
cd /Volumes/Marco-Dev/dev/net-minimal-api/.worktrees/feature/cqrs-repositories
git fetch origin
git log origin/main..HEAD --oneline
```

Esperado: os 3 commits da feature (mais quaisquer correções de teste adicionadas nas Tasks 2-3).

Se o `main` avançou desde que a branch foi criada:

```bash
git rebase origin/main
```

- [ ] **Step 2: Push da branch**

```bash
git push origin feature/cqrs-repositories
```

- [ ] **Step 3: Criar PR**

```bash
gh pr create \
  --base main \
  --head feature/cqrs-repositories \
  --title "feat: segregar repositórios em CQRS (Dapper reads + EF Core writes)" \
  --body "$(cat <<'EOF'
## Resumo

- Separa `IProdutoRepository` em `IProdutoQueryRepository` (Dapper → DTO) e `IProdutoCommandRepository` (EF Core tracking)
- Separa acesso direto ao `AppDbContext` nos handlers de Pedidos em `IPedidoQueryRepository` (Dapper → DTO) e `IPedidoCommandRepository` (EF Core tracking)
- `ProdutoService` atualizado para injetar ambos os repositórios
- Handlers de Pedidos (Create, Get, List, AddItem, Cancel) atualizados para injetar repositório correspondente
- DI registrado em `ProdutosServiceExtensions` (Produtos) e `Program.cs` (Pedidos)

## O que NÃO mudou

Endpoints, validators, domain model (Produto + Pedido + value objects), migrations, middleware.

## Plano de testes

- [ ] `dotnet test FacShopAPI.slnx` passa sem falhas
- [ ] Testes de integração HTTP de Produtos (GET, POST, PUT, PATCH, DELETE)
- [ ] Testes de integração HTTP de Pedidos (Create, Get, List, AddItem, Cancel)
- [ ] Testes unitários de domínio

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

- [ ] **Step 4: Anotar a URL do PR**

A URL será impressa pelo comando acima. Guardar para referência.

---

## Task 6: Merge e limpeza

- [ ] **Step 1: Revisar o PR**

Acessar a URL do PR criado. Verificar:

- Diff dos 3 commits (+ correções de teste se houver)
- Nenhum arquivo inesperado incluído (sem `.db`, sem `bin/`, sem `obj/`)

- [ ] **Step 2: Fazer o merge**

```bash
gh pr merge --merge --delete-branch
```

Flag `--merge` preserva os commits individuais (sem squash). Flag `--delete-branch` remove a branch remota após o merge.

- [ ] **Step 3: Atualizar main local**

```bash
cd /Volumes/Marco-Dev/dev/net-minimal-api
git pull origin main
```

- [ ] **Step 4: Confirmar que os testes passam no main**

```bash
cd /Volumes/Marco-Dev/dev/net-minimal-api
dotnet test FacShopAPI.slnx -v minimal
```

Esperado: mesmo número de testes passando anotado na Task 4.

- [ ] **Step 5: Remover a worktree local**

```bash
cd /Volumes/Marco-Dev/dev/net-minimal-api
git worktree remove .worktrees/feature/cqrs-repositories
```

Se o comando reclamar de mudanças não commitadas (não deveria, mas por precaução):

```bash
git worktree remove --force .worktrees/feature/cqrs-repositories
```

---

## Referências

- Spec: `docs/superpowers/specs/2026-04-07-cqrs-repositories-design.md`
- Feature branch: `feature/cqrs-repositories`
- Worktree: `.worktrees/feature/cqrs-repositories`
- 3 commits na branch: `3d60b5d`, `a67ab19`, `fc5a4d6`
