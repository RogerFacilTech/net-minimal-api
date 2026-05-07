# Plano: Correção das Migrations do CatalogoDbContext

## Contexto

Durante a criação do `CatalogoDbContext` (Fase 3), `CategoriaProduto` e `EstoqueProduto` foram
mapeados erroneamente como entidades com tabelas próprias, em vez de value objects com conversão
de coluna dentro de `Produtos` e `Variantes`.

O banco ainda não foi utilizado (protótipo). A correção ideal é reescrever a `InitialCatalogo`
com o schema correto, evitando a migration corretiva `FixValueObjectsAsConversions`.

## Schema correto da tabela Produtos

Colunas de value objects devem ser colunas simples (não FKs):

- `Categoria TEXT` (era `CategoriaValue TEXT` com FK para `CategoriaProdutos`)
- `Estoque INTEGER` (era `EstoqueValue INTEGER` com FK para `EstoqueProdutos`)

Tabelas `CategoriaProdutos` e `EstoqueProdutos` não devem existir.

## Passos

### Passo 1 — Deletar migration corretiva

- [x] Deletar `20260507163754_FixValueObjectsAsConversions.cs`
- [x] Deletar `20260507163754_FixValueObjectsAsConversions.Designer.cs`

### Passo 2 — Reescrever InitialCatalogo

Substituir o conteúdo de `20260507122649_InitialCatalogo.cs` com schema correto:

- Tabelas: `Atributos`, `Categorias`, `Midias`, `Produtos`, `Variantes`
- Sem `CategoriaProdutos`, sem `EstoqueProdutos`
- Sem FKs para value objects
- Colunas corretas: `Categoria TEXT`, `Estoque INTEGER` em `Produtos`
- Colunas corretas: `Estoque INTEGER` em `Variantes`
- Índices corretos: `idx_produto_ativo`, `idx_produto_categoria`, `idx_categoria_slug`, `idx_variante_produto_sku`
- [x] InitialCatalogo.cs reescrito

### Passo 3 — Atualizar snapshot e Designer

- [x] `InitialCatalogo.Designer.cs` regenerado
- [x] `CatalogoDbContextModelSnapshot.cs` atualizado

### Passo 4 — Validar build

- [x] `dotnet build` verde

### Passo 5 — Rodar testes

- [ ] `dotnet test` — todos os testes passando

## Critério de conclusão

- Testes de Catálogo e Pedidos passando sem erros de migration
- Nenhuma tabela de value object criada no banco
- Apenas uma migration (`InitialCatalogo`) no histórico do Catálogo
