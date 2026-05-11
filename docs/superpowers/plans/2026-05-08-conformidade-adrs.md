# Plano de Ação — Conformidade ADRs

Data levantamento: 2026-05-08
Última atualização: 2026-05-11
Status: Em andamento (4/5 concluídas)

---

## Contexto

Revisão das 16 ADRs identificou 5 não-conformidades. 12 ADRs estão plenamente conformes.

---

## Ações

### 🔴 CRÍTICO — 1. Implementar `TemProdutosAtivosAsync` real com FK (ADR-0015)

**Problema:** Regra "Categoria não pode ser desativada com produtos ativos" é inoperante.
A implementação sempre retorna `false` (TODO hardcoded), permitindo desativar categorias que
ainda possuem produtos ativos — violando a invariante de domínio do ADR-0015.

**Causa raiz:** `Produto` armazena a categoria via value object `CategoriaProduto` (string),
sem FK numérica para a tabela `Categorias`.

---

### Análise de abordagens

#### Alternativa A — Coexistência (coluna string + FK)

Adiciona `CategoriaId int? NULL` mantendo `Categoria TEXT` existente. Os dois campos coexistem.

| Aspecto                         | Detalhe                                                                    |
| ------------------------------- | -------------------------------------------------------------------------- |
| Escopo                          | 8 arquivos (plano abaixo)                                                  |
| Dado duplicado                  | Sim — `Categoria` (string) e `CategoriaId` guardam a mesma informação      |
| Risco de inconsistência         | Se `Categoria.Renomear()` for chamado, o string fica stale silenciosamente |
| Queries Dapper                  | Sem alteração — continuam lendo `Categoria TEXT`                           |
| `ProdutoResponse.Categoria`     | Sem alteração                                                              |
| `CategoriaProduto` value object | Mantido intacto                                                            |
| Migration                       | ADD COLUMN apenas                                                          |

Indicado se o objetivo é corrigir o bug com menor risco imediato.

---

#### Alternativa B — Remoção completa do campo string

Remove `Produto.Categoria` (value object + coluna), mantém apenas `CategoriaId int NOT NULL`.
`ProdutoResponse.Categoria` passa a ser resolvido via JOIN com a tabela `Categorias`.

| Aspecto                         | Detalhe                                                                                                 |
| ------------------------------- | ------------------------------------------------------------------------------------------------------- |
| Escopo                          | ~14 arquivos                                                                                            |
| Dado duplicado                  | Não — fonte única de verdade                                                                            |
| Risco de inconsistência         | Nenhum — FK garante integridade                                                                         |
| Queries Dapper                  | Precisam de JOIN com `Categorias` para montar `ProdutoResponse.Categoria`                               |
| `ProdutoResponse.Categoria`     | Preenchida pelo nome da categoria via JOIN                                                              |
| `CategoriaProduto` value object | Removido (ou mantido apenas como validação de entrada no request)                                       |
| `CriarProdutoRequest.Categoria` | Pode continuar string (validada contra categorias existentes no banco) ou trocar para `CategoriaId int` |
| Migration                       | ADD COLUMN `CategoriaId` + backfill + DROP COLUMN `Categoria`                                           |

Arquivos adicionais impactados além dos 8 da Alternativa A:

- `Catalogo.Domain/ValueObjects/CategoriaProduto.cs` — remover ou converter para validação de entrada apenas
- `Catalogo.Data/CatalogoDbContext.cs` — remover mapeamento de `Categoria`, tornar `CategoriaId NOT NULL`
- `Catalogo.Infrastructure/Queries/DapperProdutoQueryRepository.cs` — adicionar JOIN
- `Catalogo.Application/Validators/ProdutoValidator.cs` — ajustar validação de categoria
- `Catalogo.Application/Services/ProdutoService.cs` — `CriarProduto` passa a receber/resolver `CategoriaId`
- `Catalogo.Tests/Endpoints/ProdutoEndpointsTests.cs` — atualizar testes que usam o campo string

**Decisão pendente:** escolher entre Alternativa A (coexistência) ou Alternativa B (remoção completa).

---

#### Plano de execução (Alternativa A — coexistência)

**Passo 1 — Domínio: adicionar `CategoriaId` em `Produto`**

Arquivo: `src/Catalogo/Catalogo.Domain/Produto.cs`

- Adicionar `public int? CategoriaId { get; private set; }` (após `Categoria`)
- Adicionar método `public void DefinirCategoriaId(int id) => CategoriaId = id;`
- Atualizar `Reconstituir()`: adicionar parâmetro `int? categoriaId = null` e setar `CategoriaId = categoriaId`

`Criar()` **não recebe** `categoriaId` — o ID é definido externamente pelo service após a criação
(separação de responsabilidades: domínio não faz lookup de banco).

---

**Passo 2 — Repositório: adicionar `ObterPorNomeAsync` em `ICategoriaCommandRepository`**

Arquivo: `src/Catalogo/Catalogo.Application/Repositories/ICategoriaCommandRepository.cs`

Adicionar:

```csharp
Task<Categoria?> ObterPorNomeAsync(string nome);
```

Implementação em `EfCategoriaCommandRepository.cs`:

```csharp
public Task<Categoria?> ObterPorNomeAsync(string nome) =>
    context.Categorias.FirstOrDefaultAsync(c => c.Nome == nome && c.Ativa);
```

---

**Passo 3 — Service: resolver `CategoriaId` no `ProdutoService`**

Arquivo: `src/Catalogo/Catalogo.Application/Services/ProdutoService.cs`

- Injetar `ICategoriaCommandRepository _categoriaRepo` no construtor
- Em `CriarProdutoAsync()`: após `Produto.Criar(...)`, chamar `ObterPorNomeAsync(request.Categoria)` e `produto.DefinirCategoriaId(cat.Id)` se encontrada
- Em `AtualizarProdutoAsync()` e `AtualizarCompletoProdutoAsync()`: se `request.Categoria` foi alterado, também atualizar `CategoriaId` via `DefinirCategoriaId`

---

**Passo 4 — DbContext: configurar FK em `OnModelCreating`**

Arquivo: `src/Catalogo/Catalogo.Data/CatalogoDbContext.cs`

No bloco `modelBuilder.Entity<Produto>`, adicionar após a config de `Categoria`:

```csharp
entity.Property(p => p.CategoriaId)
    .UsePropertyAccessMode(PropertyAccessMode.Property);

entity.HasOne<Categoria>()
    .WithMany()
    .HasForeignKey(p => p.CategoriaId)
    .OnDelete(DeleteBehavior.Restrict)
    .IsRequired(false);
```

---

**Passo 5 — Migration**

Executar no terminal:

```
dotnet ef migrations add AdicionarCategoriaIdEmProduto \
  --project src/Catalogo/Catalogo.Data \
  --startup-project src/Catalogo/Catalogo.API
```

A migration gerada adicionará:

- Coluna `CategoriaId INTEGER NULL` na tabela `Produtos`
- `FK_Produtos_Categorias_CategoriaId` com `ON DELETE RESTRICT`
- Índice `IX_Produtos_CategoriaId`

---

**Passo 6 — DbSeeder: reordenar e popular `CategoriaId`**

Arquivo: `src/Catalogo/Catalogo.Infrastructure/Data/DbSeeder.cs`

- Inverter ordem em `Seed()`: chamar `SeedCategorias(context)` **antes** de `SeedProdutos(context)`
  (garantir que os IDs das categorias existam no momento da criação dos produtos)
- Em `SeedProdutos()`, após criar cada produto, fazer lookup do ID:

```csharp
var cat = context.Categorias.First(c => c.Nome == produto.Categoria.Value);
produto.DefinirCategoriaId(cat.Id);
```

---

**Passo 7 — Repositório: corrigir `TemProdutosAtivosAsync`**

Arquivo: `src/Catalogo/Catalogo.Infrastructure/Repositories/EfCategoriaCommandRepository.cs`

Substituir o `Task.FromResult(false)` por:

```csharp
public Task<bool> TemProdutosAtivosAsync(int categoriaId) =>
    context.Produtos.AnyAsync(p => p.Ativo && p.CategoriaId == categoriaId);
```

---

**Passo 8 — Teste de integração**

Arquivo: `src/Catalogo/Catalogo.Tests/Integration/Catalogo/CategoriaEndpointsTests.cs`

Novo teste `DELETE_DesativarCategoria_ComProdutoAtivo_Retorna422`:

1. Criar categoria nova via `POST /categorias` — obter seu `Id` e `Nome`
2. Criar produto com `Categoria = nome` via `POST /produtos` com token
3. `DELETE /categorias/{id}` — deve retornar **422**
   com mensagem "Não é possível desativar categoria com produtos ativos."

Nota: o `CriarProdutoRequest.Categoria` aceita apenas os valores de `CategoriaProduto.CategoriasValidas`
(`"Eletrônicos"`, `"Livros"`, `"Roupas"`, `"Alimentos"`, `"Outros"`). O teste deve usar
um desses nomes e garantir que a categoria criada tenha o mesmo nome.

---

**Arquivos a modificar (resumo):**

| #   | Arquivo                                                                | Tipo de mudança                                                               |
| --- | ---------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| 1   | `Catalogo.Domain/Produto.cs`                                           | Adicionar `CategoriaId` + `DefinirCategoriaId()` + atualizar `Reconstituir()` |
| 2   | `Catalogo.Application/Repositories/ICategoriaCommandRepository.cs`     | Adicionar `ObterPorNomeAsync`                                                 |
| 3   | `Catalogo.Application/Services/ProdutoService.cs`                      | Injetar repo categoria, setar `CategoriaId`                                   |
| 4   | `Catalogo.Data/CatalogoDbContext.cs`                                   | Configurar FK em `OnModelCreating`                                            |
| 5   | Migration (arquivo gerado)                                             | `AdicionarCategoriaIdEmProduto`                                               |
| 6   | `Catalogo.Infrastructure/Data/DbSeeder.cs`                             | Reordenar + popular `CategoriaId`                                             |
| 7   | `Catalogo.Infrastructure/Repositories/EfCategoriaCommandRepository.cs` | Implementar `ObterPorNomeAsync` + corrigir `TemProdutosAtivosAsync`           |
| 8   | `Catalogo.Tests/Integration/Catalogo/CategoriaEndpointsTests.cs`       | Novo teste de integração                                                      |

---

### ✅ CONCLUÍDO — 2. Corrigir path de IEndpoint no ADR-0009 e CLAUDE.md

**Problema:** ADR-0009 e CLAUDE.md referenciam `src/Shared/Common/IEndpoint.cs`.
Path real: `src/Shared/Web/IEndpoint.cs`.

**Arquivos modificados em 2026-05-11:**

- `docs/ADRs/ADR-0009-minimal-api-autodiscovery-endpoints.md` — path corrigido na seção Decisão
- `CLAUDE.md` — path corrigido na convenção sobre Pedidos

---

### ✅ CONCLUÍDO — 3. FluentValidation em AtributoEndpoints e MidiaEndpoints (ADR-0010)

**Decisão:** Opção A — adicionar validators (consistente com os demais recursos).

**Problema:** Endpoints de Atributo e Mídia não chamam `IValidator<T>.ValidateAsync()`,
violando o padrão definido no ADR-0010 e aplicado em Produto, Categoria, Variante.

---

#### Plano de execução (Opção A)

**Passo 1 — Criar `CriarAtributoValidator`**

Arquivo novo: `src/Catalogo/Catalogo.Application/Validators/AtributoValidator.cs`

Regras para `CriarAtributoRequest`:

- `ProdutoId` — `GreaterThan(0)`, mensagem "ProdutoId inválido."
- `Chave` — `NotEmpty`, `MaximumLength(100)`, mensagens padronizadas
- `Valor` — `NotEmpty`, `MaximumLength(500)`, mensagens padronizadas

Namespace: `FacShopAPI.Catalogo.Application.Validators`  
Padrão: idêntico a `VarianteValidator.cs` (mesmo namespace, mesmo estilo de regras)

---

**Passo 2 — Criar `CriarMidiaValidator`**

Arquivo novo: `src/Catalogo/Catalogo.Application/Validators/MidiaValidator.cs`

Regras para `CriarMidiaRequest`:

- `ProdutoId` — `GreaterThan(0)`, mensagem "ProdutoId inválido."
- `Url` — `NotEmpty`, `MaximumLength(2048)`, mensagem "URL é obrigatória." / "URL não pode exceder 2048 caracteres."
- `Ordem` — `GreaterThanOrEqualTo(0)`, mensagem "Ordem não pode ser negativa."

Namespace: `FacShopAPI.Catalogo.Application.Validators`

---

**Passo 3 — Atualizar `AtributoEndpoints.cs`**

Arquivo: `src/Catalogo/Catalogo.Endpoints/Endpoints/Atributos/AtributoEndpoints.cs`

No handler `Criar`:

- Adicionar parâmetro `IValidator<CriarAtributoRequest> validator`
- Chamar `await validator.ValidateAsync(request)` antes do service
- Retornar `Results.UnprocessableEntity(new ErrorResponse { ... })` com os erros concatenados se `!validation.IsValid`

Padrão de resposta 422 idêntico ao de `ProdutoEndpoints.cs`:

```csharp
Status = 422, Title = "Validação falhou",
Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
Type = "https://api.example.com/errors/validation"
```

---

**Passo 4 — Atualizar `MidiaEndpoints.cs`**

Arquivo: `src/Catalogo/Catalogo.Endpoints/Endpoints/Midias/MidiaEndpoints.cs`

Mesma alteração no handler `Criar` com `IValidator<CriarMidiaRequest> validator`.

---

**Observação:** Os validators são registrados automaticamente pelo assembly scanning
(`AddValidatorsFromAssemblyContaining<Program>()`) — não requer alteração no DI.

---

### ✅ CONCLUÍDO — 4. Atualizar contagem de ADRs no CLAUDE.md

**Problema:** CLAUDE.md diz "15 ADRs no formato MADR 3.x". Total real é 16 (ADR-0016 adicionado).

**Arquivo modificado:** `CLAUDE.md` — alterado "15 ADRs" para "16 ADRs" em 2026-05-11.

---

### ✅ CONCLUÍDO — 5. Cross-reference ADR-0007 → ADR-0016

**Problema:** ADR-0007 descreve emissão de JWT sem indicar que a responsabilidade foi
movida para Auth dedicado (ADR-0016). Leitor isolado pode concluir incorretamente.

**Arquivo modificado:** `docs/ADRs/ADR-0007-autenticacao-jwt-bearer.md` — seção "Ver também" adicionada em 2026-05-11, com link e descrição do ADR-0016.

---

## Ordem de execução recomendada

1. Ação 4 (CLAUDE.md contagem) — trivial, 1 linha
2. Ação 2 (path IEndpoint) — 2 arquivos, substituição pontual
3. Ação 5 (cross-reference ADR-0007) — 1 arquivo, adição de parágrafo
4. Ação 3 (FluentValidation Atributo/Mídia) — depende de decisão A ou B
5. Ação 1 (TemProdutosAtivosAsync) — maior impacto, requer teste de integração
