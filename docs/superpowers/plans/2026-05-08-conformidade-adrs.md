# Plano de Ação — Conformidade ADRs

Data levantamento: 2026-05-08
Última atualização: 2026-05-11
Status: Em andamento (4/5 concluídas)

---

## Contexto

Revisão das 16 ADRs identificou 5 não-conformidades. 12 ADRs estão plenamente conformes.

---

## Ações

### 🔴 CRÍTICO — 1. Implementar `TemProdutosAtivosAsync` real (ADR-0015)

**Problema:** Regra "Categoria não pode ser desativada com produtos ativos" é inoperante.
A implementação sempre retorna `false` (TODO hardcoded).

**Causa raiz:** `Produto` armazena categoria como string via value object `CategoriaProduto`,
sem FK inteira para a tabela `Categorias`. A validação por ID nunca funciona.

**Arquivo com bug:**

- `src/Catalogo/Catalogo.Infrastructure/Repositories/EfCategoriaCommandRepository.cs` linha 22

**Abordagem recomendada:**
Validar por string de nome em vez de ID, cruzando `Produto.Categoria.Value` com `Categoria.Nome`
enquanto a FK real não é criada. Exemplo:

```csharp
context.Produtos.AnyAsync(p => p.Ativo && p.Categoria.Value == categoria.Nome)
```

Obter `categoria.Nome` buscando a entidade pelo `categoriaId` (já feito antes no fluxo de desativação).

**Alternativa mais robusta (maior escopo):** Adicionar coluna `CategoriaId` (int?) em `Produto`
com migration, criar FK real e atualizar a query.

**Arquivos a modificar:**

- `src/Catalogo/Catalogo.Infrastructure/Repositories/EfCategoriaCommandRepository.cs`

**Teste de verificação:** Criar produto com categoria X via API, tentar `DELETE /categorias/{id}`
dessa categoria — deve retornar 422 com mensagem de erro.

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
