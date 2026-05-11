# Plano de Ação — Conformidade ADRs

Data levantamento: 2026-05-08
Status: Pendente

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

### 🟡 MODERADO — 2. Corrigir path de IEndpoint no ADR-0009 e CLAUDE.md

**Problema:** ADR-0009 e CLAUDE.md referenciam `src/Shared/Common/IEndpoint.cs`.
Path real: `src/Shared/Web/IEndpoint.cs`.

**Arquivos a modificar:**

- `docs/ADRs/ADR-0009-minimal-api-autodiscovery-endpoints.md` — corrigir path na seção Decisão
- `CLAUDE.md` — corrigir linha "Novos endpoints em Pedidos devem implementar `IEndpoint` (`src/Shared/Common/IEndpoint.cs`)"

---

### 🟡 MODERADO — 3. FluentValidation em AtributoEndpoints e MidiaEndpoints (ADR-0010)

**Problema:** Endpoints de Atributo e Mídia não chamam `IValidator<T>.ValidateAsync()`,
violando o padrão definido no ADR-0010 e aplicado em Produto, Categoria, Variante.

**Decisão a tomar antes de implementar:**

- **Opção A:** Adicionar validators `CriarAtributoValidator` e `CriarMidiaValidator` com FluentValidation
  (consistente com ADR-0010 e os demais recursos)
- **Opção B:** Documentar explicitamente no ADR-0010 que Atributo e Mídia (recursos CRUD simples
  per ADR-0015) são exceção intencional ao padrão

**Arquivos a modificar (se Opção A):**

- `src/Catalogo/Catalogo.Endpoints/Validators/` — criar `CriarAtributoValidator.cs` e `CriarMidiaValidator.cs`
- `src/Catalogo/Catalogo.Endpoints/Endpoints/Atributos/AtributoEndpoints.cs` — injetar IValidator e chamar ValidateAsync
- `src/Catalogo/Catalogo.Endpoints/Endpoints/Midias/MidiaEndpoints.cs` — idem

**Arquivos a modificar (se Opção B):**

- `docs/ADRs/ADR-0010-fluentvalidation-validacao-entrada.md` — adicionar nota de exceção

---

### 🔵 MENOR — 4. Atualizar contagem de ADRs no CLAUDE.md

**Problema:** CLAUDE.md diz "15 ADRs no formato MADR 3.x". Total real é 16 (ADR-0016 adicionado).

**Arquivo a modificar:**

- `CLAUDE.md` — alterar "15 ADRs" para "16 ADRs"

---

### 🔵 MENOR — 5. Cross-reference ADR-0007 → ADR-0016

**Problema:** ADR-0007 descreve emissão de JWT sem indicar que a responsabilidade foi
movida para Auth dedicado (ADR-0016). Leitor isolado pode concluir incorretamente.

**Arquivo a modificar:**

- `docs/ADRs/ADR-0007-autenticacao-jwt-bearer.md` — adicionar seção "Ver também: ADR-0016"

---

## Ordem de execução recomendada

1. Ação 4 (CLAUDE.md contagem) — trivial, 1 linha
2. Ação 2 (path IEndpoint) — 2 arquivos, substituição pontual
3. Ação 5 (cross-reference ADR-0007) — 1 arquivo, adição de parágrafo
4. Ação 3 (FluentValidation Atributo/Mídia) — depende de decisão A ou B
5. Ação 1 (TemProdutosAtivosAsync) — maior impacto, requer teste de integração
