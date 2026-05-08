# ReestruturaÃ§Ã£o da DocumentaÃ§Ã£o â€” Plano de ImplementaÃ§Ã£o

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** Reestruturar toda a documentaÃ§Ã£o do projeto para refletir os dois casos de uso (Produtos com camadas horizontais, Pedidos com Vertical Slice) e criar um novo guia conceitual sobre Vertical Slice Architecture e DomÃ­nio Rico.

**Architecture:** O projeto hoje tem dois padrÃµes arquiteturais coexistindo: horizontal layers (Produtos) e Vertical Slice + Rich Domain (Pedidos). A documentaÃ§Ã£o atual sÃ³ cobre o primeiro. Este plano atualiza todos os docs existentes e cria um novo guia conceitual (`VERTICAL-SLICE-DOMINIO-RICO.md`).

**Tech Stack:** Markdown. Todos os arquivos sÃ£o documentaÃ§Ã£o pura â€” sem cÃ³digo a compilar. VerificaÃ§Ã£o = `grep` para confirmar presenÃ§a de seÃ§Ãµes-chave + `dotnet test` para confirmar que os snippets de cÃ³digo no doc correspondem ao que o projeto faz.

**Contagem de testes atual:** 121 testes passando (verificar com `dotnet test --list-tests 2>/dev/null | grep -v "^Build\|^Test run\|^$" | wc -l`).

**Caminhos de referÃªncia importantes:**
- Design doc: `docs/plans/2026-02-27-documentacao-reestruturacao-design.md`
- DomÃ­nio de Pedidos: `src/Features/Pedidos/Domain/`
- Slices: `src/Features/Pedidos/CreatePedido/`, `GetPedido/`, `ListPedidos/`, `AddItemPedido/`, `CancelPedido/`
- Common: `src/Features/Common/Result.cs`, `IEndpoint.cs`, `EndpointExtensions.cs`
- Modelo Rico: `src/Models/Produto.cs`
- Testes de domÃ­nio: `FacShopAPI.Tests/Unit/Domain/`
- Testes de integraÃ§Ã£o: `FacShopAPI.Tests/Integration/`

---

### Task 1: Atualizar README.md

**Files:**
- Modify: `README.md`

Esta Ã© a porta de entrada do projeto. Precisa refletir que o projeto demonstra dois padrÃµes arquiteturais e tem 121 testes.

**Step 1: Ler o arquivo atual**

```bash
cat README.md
```

**Step 2: Reescrever o README.md com as seguintes mudanÃ§as obrigatÃ³rias**

MudanÃ§as especÃ­ficas a fazer (nÃ£o alterar o que nÃ£o estÃ¡ listado):

1. **Badge de versÃ£o**: `2.0.0` â†’ `3.0.0`

2. **SeÃ§Ã£o "Sobre o Projeto"** â€” substituir o parÃ¡grafo atual por:
```markdown
**FacShopAPI** Ã© um projeto educacional demonstrando melhores prÃ¡ticas de APIs REST com **.NET 10 LTS** e **Minimal API**. O projeto cobre dois padrÃµes arquiteturais complementares, implementados como casos de uso reais com cobertura completa de testes (121 testes).
```

3. **SeÃ§Ã£o "Principais Recursos" â†’ "6 Endpoints REST"** â€” mudar tÃ­tulo para **"11 Endpoints REST (2 casos de uso)"** e adicionar tabela de Pedidos logo abaixo da tabela de Produtos:

```markdown
### Pedidos (Vertical Slice + JWT obrigatÃ³rio)

| MÃ©todo | Rota | DescriÃ§Ã£o | Status |
|--------|------|-----------|---------|
| `POST` | `/api/v1/pedidos` | Criar pedido | 201/400 |
| `GET` | `/api/v1/pedidos/{id}` | Obter pedido | 200/404 |
| `GET` | `/api/v1/pedidos` | Listar pedidos | 200 |
| `POST` | `/api/v1/pedidos/{id}/itens` | Adicionar item | 200/400/404 |
| `POST` | `/api/v1/pedidos/{id}/cancelar` | Cancelar pedido | 200/400/404 |
```

4. **Contagem de testes**: `50+` â†’ `121`

5. **Estrutura do projeto** â€” atualizar a Ã¡rvore `src/` para incluir:
```
â”‚   â”œâ”€â”€ Features/                            # Vertical Slice Architecture
â”‚   â”‚   â”œâ”€â”€ Common/
â”‚   â”‚   â”‚   â”œâ”€â”€ IEndpoint.cs               # Interface de registro automÃ¡tico
â”‚   â”‚   â”‚   â”œâ”€â”€ EndpointExtensions.cs      # Scanner de endpoints
â”‚   â”‚   â”‚   â””â”€â”€ Result.cs                  # Result pattern
â”‚   â”‚   â””â”€â”€ Pedidos/
â”‚   â”‚       â”œâ”€â”€ Domain/                    # Aggregate root + entities
â”‚   â”‚       â”œâ”€â”€ Common/                    # DTOs dos slices
â”‚   â”‚       â”œâ”€â”€ CreatePedido/              # Slice POST /pedidos
â”‚   â”‚       â”œâ”€â”€ GetPedido/                 # Slice GET /pedidos/{id}
â”‚   â”‚       â”œâ”€â”€ ListPedidos/               # Slice GET /pedidos
â”‚   â”‚       â”œâ”€â”€ AddItemPedido/             # Slice POST /pedidos/{id}/itens
â”‚   â”‚       â””â”€â”€ CancelPedido/              # Slice POST /pedidos/{id}/cancelar
```

E a Ã¡rvore `FacShopAPI.Tests/` para incluir:
```
â”‚   â”œâ”€â”€ Unit/Domain/
â”‚   â”‚   â”œâ”€â”€ ProdutoTests.cs                # 18 testes de domÃ­nio rico
â”‚   â”‚   â””â”€â”€ PedidoTests.cs                 # 16 testes do aggregate
â”‚   â”œâ”€â”€ Builders/
â”‚   â”‚   â””â”€â”€ ProdutoBuilder.cs              # Builder fluente para testes
â”‚   â””â”€â”€ Integration/
â”‚       â”œâ”€â”€ ApiFactory.cs                  # WebApplicationFactory
â”‚       â”œâ”€â”€ AuthHelper.cs                  # JWT helper
â”‚       â”œâ”€â”€ CreatePedidoTests.cs
â”‚       â”œâ”€â”€ GetPedidoTests.cs
â”‚       â”œâ”€â”€ CancelPedidoTests.cs
â”‚       â”œâ”€â”€ AddItemPedidoTests.cs
â”‚       â””â”€â”€ ListPedidosTests.cs
```

6. **SeÃ§Ã£o "Objetivo de Aprendizado"** â€” adicionar ao final da lista:
```markdown
âœ… Vertical Slice Architecture
âœ… DomÃ­nio Rico e Aggregate Root
âœ… Result Pattern
âœ… Testes de domÃ­nio e integraÃ§Ã£o HTTP
```

7. **Adicionar exemplos de Pedidos** â€” nova seÃ§Ã£o apÃ³s os exemplos de Produtos:

```markdown
### AutenticaÃ§Ã£o (necessÃ¡ria para Pedidos)
```bash
# Obter token JWT
curl -X POST "http://localhost:5000/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@example.com", "senha": "senha123"}'
# Copie o "token" da resposta
```

```markdown
### Criar Pedido
```bash
curl -X POST "http://localhost:5000/api/v1/pedidos" \
  -H "Authorization: Bearer SEU_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}'
```

```markdown
### Adicionar Item ao Pedido
```bash
curl -X POST "http://localhost:5000/api/v1/pedidos/1/itens" \
  -H "Authorization: Bearer SEU_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"produtoId": 1, "quantidade": 2}'
```

**Step 3: Verificar**

```bash
grep -c "Pedidos\|pedidos" README.md
# Esperado: >= 10 ocorrÃªncias

grep "121" README.md
# Esperado: linha com contagem de testes

grep "3.0.0" README.md
# Esperado: badge de versÃ£o
```

**Step 4: Commit**

```bash
git add README.md
git commit -m "docs: atualizar README com Pedidos, Vertical Slice e 121 testes"
```

---

### Task 2: Reescrever 00-LEIA-PRIMEIRO.md

**Files:**
- Modify: `docs/00-LEIA-PRIMEIRO.md`

O arquivo atual Ã© uma listagem exaustiva de arquivos. Deve virar uma introduÃ§Ã£o narrativa que apresenta os dois casos de uso e aponta para as duas trilhas.

**Step 1: Escrever o novo conteÃºdo**

Substituir o arquivo inteiro por:

```markdown
# Bem-vindo ao FacShopAPI

Este Ã© um projeto educacional em **.NET 10 Minimal API** com dois casos de uso reais, cada um demonstrando um padrÃ£o arquitetural diferente.

---

## O Que Este Projeto Demonstra

### Caso 1 â€” Produtos (Camadas Horizontais)
A gestÃ£o de Produtos segue a arquitetura em camadas clÃ¡ssica: Endpoints â†’ Services â†’ Data. O modelo de domÃ­nio Ã© **rico**: a classe `Produto` encapsula suas prÃ³prias regras de negÃ³cio com factory method e mÃ©todos comportamentais â€” sem setters pÃºblicos.

**Aprenda com Produtos:**
- REST API design com Minimal API
- FluentValidation, AutoMapper, Serilog
- EF Core com SQLite
- DomÃ­nio rico vs anÃªmico
- Testes unitÃ¡rios de serviÃ§o

### Caso 2 â€” Pedidos (Vertical Slice)
A gestÃ£o de Pedidos usa **Vertical Slice Architecture**: cada operaÃ§Ã£o (criar, buscar, adicionar item, cancelar) vive em sua prÃ³pria pasta com Command/Handler/Validator/Endpoint. O aggregate `Pedido` encapsula regras de negÃ³cio complexas (merge de itens, valor mÃ­nimo, cancelamento).

**Aprenda com Pedidos:**
- Vertical Slice Architecture
- Aggregate Root e Domain-Driven Design
- Result Pattern (erros sem exceptions)
- Registro automÃ¡tico de endpoints via `IEndpoint`
- Testes de integraÃ§Ã£o HTTP com WebApplicationFactory

---

## Como ComeÃ§ar

### RÃ¡pido (5 min)
```bash
dotnet run
# Abra: http://localhost:5000
```

### Aprender (escolha sua trilha)
â†’ Veja [INDEX.md](INDEX.md) para as duas trilhas de aprendizado

### ReferÃªncias
- [ARQUITETURA.md](ARQUITETURA.md) â€” diagramas dos dois padrÃµes lado a lado
- [ESTRATEGIA-DE-TESTES.md](../FacShopAPI.Tests/ESTRATEGIA-DE-TESTES.md) â€” 121 testes em 3 categorias
```

**Step 2: Verificar**

```bash
grep "Vertical Slice\|Pedidos\|Produtos\|trilha" docs/00-LEIA-PRIMEIRO.md | wc -l
# Esperado: >= 8
```

**Step 3: Commit**

```bash
git add docs/00-LEIA-PRIMEIRO.md
git commit -m "docs: reescrever 00-LEIA-PRIMEIRO com narrativa dos dois padrÃµes"
```

---

### Task 3: Reescrever INDEX.md

**Files:**
- Modify: `docs/INDEX.md`

O INDEX atual tem um Ãºnico caminho linear e ainda sugere "adicione Pedidos" como prÃ³ximo passo. Deve ser reescrito com duas trilhas e o mapa mental atualizado.

**Step 1: Escrever o novo INDEX.md**

Estrutura obrigatÃ³ria:

```markdown
# Ãndice Completo do Projeto

## Por Onde ComeÃ§ar?

### âš¡ RÃ¡pido (5 minutos)
1. Execute: `dotnet run`
2. Abra: http://localhost:5000
3. Explore o Swagger UI â€” veja os grupos `/api/v1/produtos` e `/api/v1/pedidos`

---

## Duas Trilhas de Aprendizado

### Trilha 1 â€” REST + Camadas Horizontais (Produtos)
Para quem quer aprender fundamentos de REST API com .NET 10:

1. [MELHORES-PRATICAS-API.md](MELHORES-PRATICAS-API.md) â€” teoria REST (30min)
2. [MELHORES-PRATICAS-MINIMAL-API.md](MELHORES-PRATICAS-MINIMAL-API.md) â†’ seÃ§Ã£o Produtos (30min)
3. CÃ³digo: `src/Endpoints/`, `src/Services/`, `src/Models/Produto.cs`
4. Testes: `FacShopAPI.Tests/Services/` e `FacShopAPI.Tests/Validators/`

### Trilha 2 â€” Vertical Slice + DomÃ­nio Rico (Pedidos)
Para quem quer ir alÃ©m e estudar padrÃµes avanÃ§ados:

1. [VERTICAL-SLICE-DOMINIO-RICO.md](VERTICAL-SLICE-DOMINIO-RICO.md) â€” teoria (30min)
2. [MELHORES-PRATICAS-MINIMAL-API.md](MELHORES-PRATICAS-MINIMAL-API.md) â†’ seÃ§Ã£o Pedidos (20min)
3. CÃ³digo: `src/Features/Pedidos/`
4. Testes: `FacShopAPI.Tests/Unit/Domain/` e `FacShopAPI.Tests/Integration/`

### Profundo (completo)
Ambas as trilhas â†’ [ARQUITETURA.md](ARQUITETURA.md) â†’ [ESTRATEGIA-DE-TESTES.md](../FacShopAPI.Tests/ESTRATEGIA-DE-TESTES.md)

---

## DocumentaÃ§Ã£o

[tabela com todos os docs e seus papÃ©is â€” incluindo VERTICAL-SLICE-DOMINIO-RICO.md como novo guia]

---

## Estrutura do CÃ³digo

[Ã¡rvore completa de src/ incluindo src/Features/Pedidos/]

---

## Mapa Mental de Aprendizado

[diagrama ASCII atualizado com Features/ e os dois caminhos]

---

## ReferÃªncias RÃ¡pidas

[tabela atualizada incluindo referÃªncias de Pedidos]
```

Pontos obrigatÃ³rios:
- Remover toda a seÃ§Ã£o "PrÃ³ximos Passos Sugeridos" que sugere adicionar Pedidos (jÃ¡ existe)
- Incluir `VERTICAL-SLICE-DOMINIO-RICO.md` como um dos guias conceituais
- Mapa mental deve incluir `src/Features/` como ramo separado de `src/`
- Data no rodapÃ©: `27 de Fevereiro de 2026` / VersÃ£o: `3.0.0`

**Step 2: Verificar**

```bash
grep "Trilha\|Pedidos\|Vertical Slice\|VERTICAL-SLICE" docs/INDEX.md | wc -l
# Esperado: >= 8

grep "Adicione um novo modelo\|prÃ³ximo passo.*Pedido" docs/INDEX.md
# Esperado: nenhuma saÃ­da (seÃ§Ã£o removida)
```

**Step 3: Commit**

```bash
git add docs/INDEX.md
git commit -m "docs: reescrever INDEX com duas trilhas de aprendizado"
```

---

### Task 4: Reescrever ARQUITETURA.md

**Files:**
- Modify: `docs/ARQUITETURA.md`

O arquivo atual sÃ³ mostra a arquitetura em camadas dos Produtos. Precisa mostrar os dois padrÃµes lado a lado e o modelo de dados completo.

**Step 1: Ler os arquivos de referÃªncia**

```bash
cat src/Features/Common/IEndpoint.cs
cat src/Features/Common/EndpointExtensions.cs
cat src/Features/Pedidos/CreatePedido/CreatePedidoEndpoint.cs
```

**Step 2: Estrutura do novo ARQUITETURA.md**

SeÃ§Ãµes obrigatÃ³rias:

**SeÃ§Ã£o 1 â€” CoexistÃªncia dos Dois PadrÃµes**
Diagrama ASCII side-by-side mostrando:
```
Produtos (Horizontal Layers)     â”‚  Pedidos (Vertical Slice)
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
ProdutoEndpoints.cs              â”‚  Features/Pedidos/
  â””â”€ chama IProdutoService        â”‚    CreatePedido/
       â””â”€ usa AppDbContext         â”‚      â”œâ”€ CreatePedidoCommand.cs
                                   â”‚      â”œâ”€ CreatePedidoValidator.cs
                                   â”‚      â””â”€ CreatePedidoEndpoint.cs
                                   â”‚           â””â”€ usa AppDbContext
```
Ambos compartilham: mesmo `AppDbContext`, mesma pipeline JWT, mesmo middleware de exceÃ§Ãµes.

**SeÃ§Ã£o 2 â€” Arquitetura em Camadas (Produtos)**
Manter o diagrama vertical existente, atualizado para refletir JWT ativo (nÃ£o "preparado").

**SeÃ§Ã£o 3 â€” Vertical Slice (Pedidos)**
Novo diagrama mostrando anatomia de um slice:
```
HTTP Request
   â†“
[CreatePedidoEndpoint]   â† sÃ³ roteamento
   â†“
[CreatePedidoValidator]  â† FluentValidation do DTO
   â†“
[CreatePedidoCommand]    â† lÃ³gica de aplicaÃ§Ã£o (Handler inline)
   â†“
[Pedido.AdicionarItem()] â† regras no domÃ­nio, retorna Result<T>
   â†“
[AppDbContext]           â† persistÃªncia
```

**SeÃ§Ã£o 4 â€” Modelo de DomÃ­nio Rico**
ComparaÃ§Ã£o Produto (antes anÃªmico â†’ agora rico):
- Antes: `public string Nome { get; set; }` â€” setter pÃºblico, sem comportamento
- Depois: `public string Nome { get; private set; }` + `static Result<Produto> Criar(...)`

**SeÃ§Ã£o 5 â€” Data Model**
Tabela de Produtos (existente) + novas tabelas:
```
Pedidos               PedidoItens
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€        â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Id (PK)               Id (PK)
Status (TEXT)         PedidoId (FK â†’ Pedidos)
Total (DECIMAL)       ProdutoId (INT)
CriadoEm             NomeProduto (TEXT snapshot)
ConfirmadoEm         PrecoUnitario (DECIMAL snapshot)
CanceladoEm          Quantidade (INT)
MotivoCancelamento
```

**SeÃ§Ã£o 6 â€” DI Container**
Atualizar para incluir:
```
â”œâ”€ IEndpoint scan (AddEndpointsFromAssembly)
â”‚  â”œâ”€ CreatePedidoEndpoint
â”‚  â”œâ”€ GetPedidoEndpoint
â”‚  â”œâ”€ ListPedidosEndpoint
â”‚  â”œâ”€ AddItemEndpoint
â”‚  â””â”€ CancelPedidoEndpoint
```

**Step 3: Verificar**

```bash
grep "CoexistÃªncia\|Vertical Slice\|PedidoItens\|AddEndpointsFromAssembly" docs/ARQUITETURA.md | wc -l
# Esperado: >= 4
```

**Step 4: Commit**

```bash
git add docs/ARQUITETURA.md
git commit -m "docs: reescrever ARQUITETURA com diagrama duplo e modelo de dados de Pedidos"
```

---

### Task 5: Reescrever ESTRATEGIA-DE-TESTES.md

**Files:**
- Modify: `FacShopAPI.Tests/ESTRATEGIA-DE-TESTES.md`

O arquivo atual descreve apenas testes de Produto com mocks e ainda lista "Integration Tests com WebApplicationFactory" como passo futuro â€” quando esses testes jÃ¡ existem e funcionam. Precisa de reescrita completa.

**Step 1: Verificar a estrutura real de testes**

```bash
find FacShopAPI.Tests -name "*.cs" | grep -v "\.csproj" | sort
dotnet test --list-tests 2>/dev/null | grep -v "^Build\|^Test run\|^$" | sort
```

**Step 2: Escrever novo ESTRATEGIA-DE-TESTES.md**

Estrutura obrigatÃ³ria:

```markdown
# EstratÃ©gia de Testes â€” FacShopAPI

## SumÃ¡rio

**121 testes** organizados em 3 categorias. Execute todos com: `dotnet test`

---

## Categoria 1 â€” Testes de DomÃ­nio (Unit)

Sem infraestrutura, sem banco, sem HTTP. Testam as regras de negÃ³cio puras.

### ProdutoTests.cs (18 testes)
Testa o modelo rico de Produto:
- `Criar` â€” validaÃ§Ãµes do factory method
- `AtualizarPreco` â€” valor > 0, diferente do atual
- `ReporEstoque` â€” positivo, mÃ¡ximo 99.999
- `Desativar` â€” nÃ£o pode desativar jÃ¡ inativo
- `TemEstoqueDisponivel` â€” combina Ativo + Estoque

### PedidoTests.cs (16 testes)
Testa o aggregate root Pedido:
- `AdicionarItem` â€” sÃ³ Rascunho, qtd 1-999, merge de itens, limite 20
- `Confirmar` â€” requer itens, valor mÃ­nimo R$ 10
- `Cancelar` â€” motivo obrigatÃ³rio, nÃ£o re-cancelar

PadrÃ£o usado: sem mocks, sem banco â€” objetos de domÃ­nio puro.

### ProdutoBuilder.cs
Builder fluente para criar `Produto` sem boilerplate em testes:
[exemplo de uso]

---

## Categoria 2 â€” Testes de ServiÃ§o (Unit com mocks)

Testam `ProdutoService` com AppDbContext e AutoMapper mockados.

### ProdutoServiceTests.cs (~16 testes)
[descriÃ§Ã£o dos cenÃ¡rios]

### ProdutoValidatorTests.cs (~20 testes)
[descriÃ§Ã£o dos cenÃ¡rios]

---

## Categoria 3 â€” Testes de IntegraÃ§Ã£o HTTP

Ponta a ponta: HTTP real, banco SQLite em memÃ³ria. Sem mocks.

### ApiFactory.cs
`WebApplicationFactory<Program>` que:
- Substitui SQLite por InMemory para isolamento
- Faz seed de dados via `CreateHost` override
- Configurada via variÃ¡vel de ambiente `ASPNETCORE_ENVIRONMENT=Testing`

### AuthHelper.cs
Gera token JWT vÃ¡lido para os testes autenticados.
Credenciais: `admin@example.com` / `senha123`

### Testes de IntegraÃ§Ã£o (13 testes)
| Arquivo | Testes | Cobertura |
|---|---|---|
| CreatePedidoTests | 4 | POST /pedidos |
| GetPedidoTests | 2 | GET /pedidos/{id} |
| CancelPedidoTests | 3 | POST /pedidos/{id}/cancelar |
| AddItemPedidoTests | 2 | POST /pedidos/{id}/itens |
| ListPedidosTests | 2 | GET /pedidos |

---

## Como Executar

[comandos dotnet test com filtros por namespace/categoria]

---

## ConvenÃ§Ãµes

[padrÃ£o MethodName_Scenario_ExpectedResult]
```

Pontos obrigatÃ³rios:
- Remover a seÃ§Ã£o "PrÃ³ximos Passos" que lista WebApplicationFactory como futuro
- Refletir 121 testes no total
- Incluir exemplos do ProdutoBuilder e ApiFactory

**Step 3: Verificar**

```bash
grep "WebApplicationFactory.*futuro\|prÃ³ximo.*WebApplication\|Integration Tests com WebApplication" \
  FacShopAPI.Tests/ESTRATEGIA-DE-TESTES.md
# Esperado: nenhuma saÃ­da

grep "121\|ApiFactory\|ProdutoBuilder" FacShopAPI.Tests/ESTRATEGIA-DE-TESTES.md | wc -l
# Esperado: >= 3
```

**Step 4: Commit**

```bash
git add FacShopAPI.Tests/ESTRATEGIA-DE-TESTES.md
git commit -m "docs: reescrever ESTRATEGIA-DE-TESTES com 3 categorias reais e 121 testes"
```

---

### Task 6: Criar VERTICAL-SLICE-DOMINIO-RICO.md (novo guia conceitual)

**Files:**
- Create: `docs/VERTICAL-SLICE-DOMINIO-RICO.md`

Este Ã© o maior entregÃ¡vel. Um guia conceitual educacional sobre Vertical Slice Architecture e DomÃ­nio Rico, usando o prÃ³prio projeto como referÃªncia. Similar em propÃ³sito ao `MELHORES-PRATICAS-API.md`, mas cobrindo padrÃµes arquiteturais avanÃ§ados.

**Step 1: Ler os arquivos de referÃªncia do projeto**

```bash
cat src/Features/Common/Result.cs
cat src/Features/Common/IEndpoint.cs
cat src/Features/Common/EndpointExtensions.cs
cat src/Features/Pedidos/Domain/Pedido.cs
cat src/Features/Pedidos/Domain/PedidoItem.cs
cat src/Features/Pedidos/CreatePedido/CreatePedidoCommand.cs
cat src/Features/Pedidos/CreatePedido/CreatePedidoValidator.cs
cat src/Features/Pedidos/CreatePedido/CreatePedidoEndpoint.cs
cat src/Models/Produto.cs
```

**Step 2: Escrever o guia com as seguintes seÃ§Ãµes obrigatÃ³rias**

**SeÃ§Ã£o 1 â€” O Problema com Camadas Horizontais**

Mostre que uma nova feature (ex: "criar pedido") requer tocar 5 arquivos em 5 pastas diferentes:
```
Nova feature "Criar Pedido":
  src/Models/Pedido.cs          â† novo modelo
  src/DTOs/PedidoDTO.cs         â† novo DTO
  src/Validators/...            â† novo validador
  src/Services/PedidoService.cs â† nova lÃ³gica
  src/Endpoints/PedidoEndpoints â† novo endpoint
```
O custo: coupling entre camadas, difÃ­cil de encontrar tudo de uma feature.

**SeÃ§Ã£o 2 â€” Vertical Slice Architecture**

Conceito: organizar por feature, nÃ£o por camada.
```
src/Features/Pedidos/
  CreatePedido/   â† tudo de "criar pedido" em um lugar
  GetPedido/      â† tudo de "buscar pedido"
  ...
```

Anatomia de um slice (usando CreatePedido como exemplo real do projeto):
- **Command**: DTO de entrada + Handler com lÃ³gica de aplicaÃ§Ã£o
- **Validator**: FluentValidation do DTO de entrada
- **Endpoint**: sÃ³ roteamento HTTP, sem lÃ³gica

Mostrar o cÃ³digo real de `CreatePedidoEndpoint.cs`, `CreatePedidoCommand.cs` e `CreatePedidoValidator.cs` com anotaÃ§Ãµes explicando cada responsabilidade.

**Registro automÃ¡tico com `IEndpoint`:**
Mostrar a interface e o `AddEndpointsFromAssembly` â€” nenhum slice precisa ser registrado manualmente em `Program.cs`.

**SeÃ§Ã£o 3 â€” Modelo de DomÃ­nio AnÃªmico vs Rico**

ComparaÃ§Ã£o direta usando cÃ³digo do projeto:

```csharp
// âŒ AnÃªmico â€” Produto antes da refatoraÃ§Ã£o
public class Produto
{
    public string Nome { get; set; } = "";
    public decimal Preco { get; set; }
    // sem validaÃ§Ã£o, sem comportamento
}
// Quem garante que Preco > 0? O Service. E se alguÃ©m setar diretamente?
```

```csharp
// âœ… Rico â€” Produto atual (src/Models/Produto.cs)
public class Produto
{
    public string Nome { get; private set; } = "";
    public decimal Preco { get; private set; }

    public static Result<Produto> Criar(string nome, decimal preco, ...)
    {
        if (preco <= 0) return Result<Produto>.Fail("PreÃ§o deve ser maior que zero");
        // ...
    }

    public Result AtualizarPreco(decimal novoPreco)
    {
        if (novoPreco <= 0) return Result.Fail("PreÃ§o invÃ¡lido");
        if (novoPreco == Preco) return Result.Fail("PreÃ§o jÃ¡ Ã© esse valor");
        Preco = novoPreco;
        return Result.Ok();
    }
}
```
Mostrar o cÃ³digo real de `src/Models/Produto.cs` (mÃ©todos principais).

**SeÃ§Ã£o 4 â€” Result Pattern**

Por que nÃ£o usar exceptions para erros de domÃ­nio:
- Exceptions sÃ£o caras (stack trace)
- Exceptions como controle de fluxo sÃ£o "code smell"
- Result torna o contrato explÃ­cito

Mostrar o cÃ³digo real de `src/Features/Common/Result.cs`.

Como usar no endpoint:
```csharp
var result = pedido.AdicionarItem(produto, request.Quantidade);
if (!result.IsSuccess)
    return TypedResults.BadRequest(new ErrorResponse { Detail = result.Error });
```

**SeÃ§Ã£o 5 â€” Aggregate Root com Pedido**

O que Ã© um aggregate: fronteira de consistÃªncia. Tudo que pertence ao `Pedido` muda junto.

Regras encapsuladas no `Pedido` (listar com cÃ³digo real):
- `AdicionarItem`: sÃ³ em Rascunho, merge de quantidade, limite 20 itens
- `Confirmar`: precisa de itens e valor >= R$ 10
- `Cancelar`: motivo obrigatÃ³rio, pedido jÃ¡ cancelado nÃ£o pode cancelar de novo

`PedidoItem` como entidade filha: preÃ§o e nome sÃ£o **snapshots** do momento do pedido (o produto pode mudar de preÃ§o depois).

**SeÃ§Ã£o 6 â€” CoexistÃªncia no Mesmo Projeto**

```
Produtos (Horizontal)         Pedidos (Vertical Slice)
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€         â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Bom para: CRUD simples        Bom para: features com
e equipes pequenas            regras de negÃ³cio ricas
```

Ambos compartilham:
- Mesmo `AppDbContext` (`DbSet<Produto>`, `DbSet<Pedido>`, `DbSet<PedidoItem>`)
- Mesma pipeline JWT
- Mesmo middleware de exceÃ§Ãµes

**SeÃ§Ã£o 7 â€” Onde Ver no CÃ³digo**

Tabela mapeando conceito â†’ arquivo do projeto.

**Step 3: Verificar**

```bash
# Verificar que todas as seÃ§Ãµes obrigatÃ³rias estÃ£o presentes
grep -c "AnÃªmico\|Result\|Aggregate\|Coexist\|IEndpoint\|Vertical Slice" \
  docs/VERTICAL-SLICE-DOMINIO-RICO.md
# Esperado: >= 6
```

**Step 4: Commit**

```bash
git add docs/VERTICAL-SLICE-DOMINIO-RICO.md
git commit -m "docs: criar guia conceitual de Vertical Slice Architecture e DomÃ­nio Rico"
```

---

### Task 7: Adicionar capÃ­tulo de Pedidos ao MELHORES-PRATICAS-MINIMAL-API.md

**Files:**
- Modify: `docs/MELHORES-PRATICAS-MINIMAL-API.md`

O guia de implementaÃ§Ã£o atual sÃ³ cobre Produtos. Adicionar um novo capÃ­tulo no final sobre como as prÃ¡ticas se aplicam ao caso Pedidos (Vertical Slice).

**Step 1: Ler o final do arquivo atual para saber onde inserir**

```bash
tail -50 docs/MELHORES-PRATICAS-MINIMAL-API.md
```

**Step 2: Adicionar no final do arquivo**

Adicionar nova seÃ§Ã£o (nÃ£o alterar o conteÃºdo existente):

```markdown
---

## Pedidos â€” Vertical Slice em AÃ§Ã£o

Esta seÃ§Ã£o demonstra como as mesmas boas prÃ¡ticas se aplicam com Vertical Slice Architecture.

### Registro AutomÃ¡tico de Endpoints

Em vez de registrar cada endpoint manualmente em `Program.cs`, os slices de Pedidos usam scan automÃ¡tico via `IEndpoint`:

[mostrar cÃ³digo de IEndpoint.cs e AddEndpointsFromAssembly â€” ler de src/Features/Common/]

### Anatomia de um Slice: CreatePedido

**Endpoint** (`src/Features/Pedidos/CreatePedido/CreatePedidoEndpoint.cs`):
[mostrar cÃ³digo real â€” sÃ³ roteamento, sem lÃ³gica]

**Command/Handler** (`src/Features/Pedidos/CreatePedido/CreatePedidoCommand.cs`):
[mostrar cÃ³digo real â€” lÃ³gica de aplicaÃ§Ã£o, chama domÃ­nio]

**Validator** (`src/Features/Pedidos/CreatePedido/CreatePedidoValidator.cs`):
[mostrar cÃ³digo real â€” FluentValidation]

### Result Pattern nos Endpoints

Como o endpoint trata o `Result<T>` retornado pelo domÃ­nio:
[exemplo de cÃ³digo mostrando if (!result.IsSuccess)]

### ReferÃªncia

Para entender os conceitos por trÃ¡s destes padrÃµes:
â†’ [VERTICAL-SLICE-DOMINIO-RICO.md](VERTICAL-SLICE-DOMINIO-RICO.md)
```

**Step 3: Verificar**

```bash
grep "Vertical Slice\|CreatePedido\|IEndpoint\|Result" \
  docs/MELHORES-PRATICAS-MINIMAL-API.md | wc -l
# Esperado: >= 6
```

**Step 4: Commit**

```bash
git add docs/MELHORES-PRATICAS-MINIMAL-API.md
git commit -m "docs: adicionar capÃ­tulo de Pedidos (Vertical Slice) ao guia de implementaÃ§Ã£o"
```

---

### Task 8: Adicionar features dos slices ao MELHORIAS-DOTNET-10.md

**Files:**
- Modify: `docs/MELHORIAS-DOTNET-10.md`

O guia atual cobre Typed Results, MapGroup, etc. â€” features usadas em Produtos. Adicionar features usadas nos slices de Pedidos.

**Step 1: Ler o final do arquivo para saber onde inserir**

```bash
tail -30 docs/MELHORIAS-DOTNET-10.md
```

**Step 2: Adicionar no final**

Nova seÃ§Ã£o com trÃªs features obrigatÃ³rias:

**Feature: Scan automÃ¡tico de endpoints via reflection**
```csharp
// Program.cs â€” nenhum slice Ã© registrado manualmente
builder.Services.AddEndpointsFromAssembly(typeof(Program).Assembly);
// ...
app.MapRegisteredEndpoints();
```
Mostrar o cÃ³digo real de `EndpointExtensions.cs`.

**Feature: Collection expressions (C# 12 / .NET 8+, adotado em .NET 10)**
```csharp
// No aggregate Pedido
private readonly List<PedidoItem> _itens = [];  // collection expression
```
Antes: `new List<PedidoItem>()`. Mais conciso e legÃ­vel.

**Feature: Primary constructors em handlers**
```csharp
// Handler inline com primary constructor
public class CreatePedidoHandler(AppDbContext db)
{
    public async Task<Result<PedidoResponse>> Handle(CreatePedidoCommand cmd)
    { ... }
}
```
Antes: campo `private readonly AppDbContext _db;` + construtor explÃ­cito.

**Step 3: Verificar**

```bash
grep "AddEndpointsFromAssembly\|collection expression\|Primary constructor\|\[\]" \
  docs/MELHORIAS-DOTNET-10.md | wc -l
# Esperado: >= 3
```

**Step 4: Commit**

```bash
git add docs/MELHORIAS-DOTNET-10.md
git commit -m "docs: adicionar features .NET 10 usadas nos slices de Pedidos"
```

---

### Task 9: Atualizar ENTREGA-FINAL.md e CHECKLIST.md

**Files:**
- Modify: `docs/ENTREGA-FINAL.md`
- Modify: `docs/CHECKLIST.md`

**ENTREGA-FINAL.md â€” mudanÃ§as obrigatÃ³rias:**

1. TÃ­tulo "TrÃªs Pilares Educacionais" â†’ "Quatro Pilares Educacionais" (adicionar o novo guia VERTICAL-SLICE-DOMINIO-RICO.md como pilar 4)
2. SeÃ§Ã£o "AplicaÃ§Ã£o ExecutÃ¡vel": atualizar "6 endpoints" â†’ "11 endpoints (6 Produtos + 5 Pedidos)"
3. SeÃ§Ã£o "Arquitetura": substituir o diagrama de camadas Ãºnico por:
```
Produtos: Endpoint â†’ Service â†’ Data (Horizontal Layers)
Pedidos:  Slice (Command+Handler+Validator+Endpoint) â†’ Data (Vertical Slice)
```
4. SeÃ§Ã£o "O Que VocÃª AprenderÃ¡" â€” adicionar ao bloco Arquitetural:
```
- Vertical Slice Architecture
- Aggregate Root e Domain-Driven Design
- Result Pattern
```
5. SeÃ§Ã£o "Tecnologias" â€” corrigir versÃ£o ".NET" de "9 LTS" para "10 LTS" (erro no doc atual), remover versÃµes desatualizadas
6. Remover "PrÃ³ximos Passos" que sugere adicionar autenticaÃ§Ã£o JWT e testes (ambos jÃ¡ existem)
7. Data/versÃ£o no rodapÃ©: `27 de Fevereiro de 2026` / `3.0.0`

**CHECKLIST.md â€” mudanÃ§as obrigatÃ³rias:**

Ler o arquivo inteiro primeiro. Adicionar nova seÃ§Ã£o ao final:

```markdown
## Features/Pedidos (Vertical Slice)

### DomÃ­nio
- [x] `src/Features/Pedidos/Domain/Pedido.cs` â€” Aggregate root com regras de negÃ³cio
- [x] `src/Features/Pedidos/Domain/PedidoItem.cs` â€” Entity filha (snapshot de preÃ§o)
- [x] `src/Features/Pedidos/Domain/StatusPedido.cs` â€” Enum: Rascunho, Confirmado, Cancelado
- [x] `src/Features/Common/Result.cs` â€” Result pattern para erros de domÃ­nio

### Slices
- [x] `CreatePedido/` â€” POST /api/v1/pedidos (Command + Validator + Endpoint)
- [x] `GetPedido/` â€” GET /api/v1/pedidos/{id}
- [x] `ListPedidos/` â€” GET /api/v1/pedidos (paginaÃ§Ã£o + filtro por status)
- [x] `AddItemPedido/` â€” POST /api/v1/pedidos/{id}/itens
- [x] `CancelPedido/` â€” POST /api/v1/pedidos/{id}/cancelar

### Infraestrutura
- [x] `IEndpoint` + `AddEndpointsFromAssembly` â€” registro automÃ¡tico
- [x] `AppDbContext` â€” DbSet<Pedido> e DbSet<PedidoItem> configurados

### Testes
- [x] `FacShopAPI.Tests/Unit/Domain/PedidoTests.cs` â€” 16 testes de domÃ­nio
- [x] `FacShopAPI.Tests/Integration/` â€” 13 testes de integraÃ§Ã£o HTTP
- [x] `FacShopAPI.Tests/Builders/ProdutoBuilder.cs` â€” builder para testes
```

**Step 1: Ler os arquivos**

```bash
cat docs/ENTREGA-FINAL.md
cat docs/CHECKLIST.md
```

**Step 2: Aplicar as mudanÃ§as descritas acima**

**Step 3: Verificar**

```bash
grep "Vertical Slice\|Pedidos\|11 endpoint" docs/ENTREGA-FINAL.md | wc -l
# Esperado: >= 3

grep "CreatePedido\|PedidoTests\|Integration" docs/CHECKLIST.md | wc -l
# Esperado: >= 5
```

**Step 4: Commit**

```bash
git add docs/ENTREGA-FINAL.md docs/CHECKLIST.md
git commit -m "docs: atualizar ENTREGA-FINAL e CHECKLIST para refletir Pedidos e Vertical Slice"
```

---

### Task 10: Atualizar INICIO-RAPIDO.md

**Files:**
- Modify: `docs/INICIO-RAPIDO.md`

O guia atual nÃ£o menciona autenticaÃ§Ã£o nem Pedidos, e ainda sugere "como adicionar autenticaÃ§Ã£o JWT" como prÃ³ximo passo (jÃ¡ existe).

**Step 1: Ler o arquivo atual**

```bash
cat docs/INICIO-RAPIDO.md
```

**Step 2: Aplicar as seguintes mudanÃ§as**

1. **SeÃ§Ã£o "TrÃªs Documentos Principais"** â€” adicionar como item 4:
```markdown
### 4ï¸âƒ£ Vertical Slice e DomÃ­nio Rico
**Arquivo**: [VERTICAL-SLICE-DOMINIO-RICO.md](VERTICAL-SLICE-DOMINIO-RICO.md)
Guia conceitual sobre os padrÃµes usados no caso de uso de Pedidos.
```

2. **SeÃ§Ã£o "Fluxo de Aprendizado"** â€” substituir o fluxo Ãºnico pelas duas trilhas (apontar para INDEX.md para o detalhe).

3. **SeÃ§Ã£o "Testar a API"** â€” adicionar apÃ³s os exemplos de Produto:

```markdown
### Testar Pedidos (requer autenticaÃ§Ã£o)

**Passo 1 â€” Obter token JWT:**
```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@example.com", "senha": "senha123"}'
```
Copie o campo `token` da resposta.

**Passo 2 â€” Criar um pedido:**
```bash
TOKEN="seu_token_aqui"
curl -X POST http://localhost:5000/api/v1/pedidos \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}'
```

**Passo 3 â€” Adicionar item:**
```bash
curl -X POST http://localhost:5000/api/v1/pedidos/1/itens \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"produtoId": 1, "quantidade": 2}'
```

4. **Tabela "Conceitos Demonstrados"** â€” adicionar linhas:
```markdown
| **Vertical Slice** | [src/Features/Pedidos/](../src/Features/Pedidos/) |
| **DomÃ­nio Rico** | [src/Models/Produto.cs](../src/Models/Produto.cs) |
| **Result Pattern** | [src/Features/Common/Result.cs](../src/Features/Common/Result.cs) |
```

5. **FAQ** â€” substituir "Como adicionar autenticaÃ§Ã£o JWT?" por "Como funciona a autenticaÃ§Ã£o?" com resposta descrevendo o que jÃ¡ existe.

6. **"PrÃ³ximos Passos"** â€” remover itens jÃ¡ implementados (autenticaÃ§Ã£o JWT, testes â€” ambos jÃ¡ existem).

7. Data/versÃ£o no rodapÃ©: `27 de Fevereiro de 2026` / `3.0.0`

**Step 3: Verificar**

```bash
grep "Pedidos\|JWT\|VERTICAL-SLICE\|Bearer" docs/INICIO-RAPIDO.md | wc -l
# Esperado: >= 5
```

**Step 4: Commit final**

```bash
git add docs/INICIO-RAPIDO.md
git commit -m "docs: atualizar INICIO-RAPIDO com auth JWT e exemplos de Pedidos"
```

---

### Task 11: VerificaÃ§Ã£o final e push

**Step 1: Confirmar que todos os arquivos foram atualizados**

```bash
git log --oneline -15
# Esperado: ver os 9+ commits das tasks anteriores
```

**Step 2: Verificar que todos os links internos dos docs funcionam**

Verificar que os arquivos referenciados existem:
```bash
# Links crÃ­ticos que devem existir
ls docs/VERTICAL-SLICE-DOMINIO-RICO.md
ls docs/MELHORES-PRATICAS-API.md
ls docs/MELHORES-PRATICAS-MINIMAL-API.md
ls docs/MELHORIAS-DOTNET-10.md
ls docs/ARQUITETURA.md
ls docs/CHECKLIST.md
ls docs/ENTREGA-FINAL.md
ls docs/INICIO-RAPIDO.md
ls docs/INDEX.md
ls FacShopAPI.Tests/ESTRATEGIA-DE-TESTES.md
ls src/Features/Pedidos/Domain/Pedido.cs
ls src/Features/Common/Result.cs
```

**Step 3: Confirmar que o projeto ainda compila e testes passam**

```bash
cd /Users/marco.mendes/code/net-minimal-api
dotnet build -c Release 2>&1 | tail -5
# Esperado: Build succeeded

dotnet test 2>&1 | tail -5
# Esperado: X passed, 0 failed
```

**Step 4: Push**

```bash
git push origin main
```
