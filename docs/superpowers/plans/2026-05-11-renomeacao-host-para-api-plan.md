# Plano: Renomeação Host → API e FacShopAPI → FacShop

**Data:** 2026-05-11  
**Status:** pendente

## Objetivo

- `Catalogo.Host` → `Catalogo.API`
- `Pedidos.Host` → `Pedidos.API`
- Solução `FacShopAPI.slnx` → `FacShop.slnx`

**Fora do escopo:** namespaces `FacShopAPI.*` (não foram pedidos).

---

## Fase 1 — File system (rename de pastas e arquivos)

Usar `git mv` para preservar histórico Git.

| Operação           | De                            | Para                         |
| ------------------ | ----------------------------- | ---------------------------- |
| Renomear pasta     | `src/Catalogo/Catalogo.Host/` | `src/Catalogo/Catalogo.API/` |
| Renomear `.csproj` | `Catalogo.Host.csproj`        | `Catalogo.API.csproj`        |
| Renomear `.xml`    | `Catalogo.Host.xml`           | `Catalogo.API.xml`           |
| Renomear pasta     | `src/Pedidos/Pedidos.Host/`   | `src/Pedidos/Pedidos.API/`   |
| Renomear `.csproj` | `Pedidos.Host.csproj`         | `Pedidos.API.csproj`         |
| Renomear solução   | `FacShopAPI.slnx`             | `FacShop.slnx`               |

### Comandos

```bash
git mv src/Catalogo/Catalogo.Host/Catalogo.Host.csproj src/Catalogo/Catalogo.Host/Catalogo.API.csproj
git mv src/Catalogo/Catalogo.Host/Catalogo.Host.xml src/Catalogo/Catalogo.Host/Catalogo.API.xml
git mv src/Catalogo/Catalogo.Host src/Catalogo/Catalogo.API

git mv src/Pedidos/Pedidos.Host/Pedidos.Host.csproj src/Pedidos/Pedidos.Host/Pedidos.API.csproj
git mv src/Pedidos/Pedidos.Host src/Pedidos/Pedidos.API

git mv FacShopAPI.slnx FacShop.slnx
```

> **Nota:** `git mv` não renomeia arquivos dentro de pastas em dois passos; renomear o arquivo `.csproj` primeiro, depois a pasta.

---

## Fase 2 — Arquivos de projeto (csproj)

### `src/Catalogo/Catalogo.API/Catalogo.API.csproj`

- `<RootNamespace>Catalogo.Host</RootNamespace>` → `<RootNamespace>Catalogo.API</RootNamespace>`
- `<AssemblyName>Catalogo.Host</AssemblyName>` → `<AssemblyName>Catalogo.API</AssemblyName>`
- `<DocumentationFile>Catalogo.Host.xml</DocumentationFile>` → `<DocumentationFile>Catalogo.API.xml</DocumentationFile>`

### `src/Pedidos/Pedidos.API/Pedidos.API.csproj`

- `<RootNamespace>FacShopAPI.Pedidos.Host</RootNamespace>` → `<RootNamespace>FacShopAPI.Pedidos.API</RootNamespace>`
- `<AssemblyName>Pedidos.Host</AssemblyName>` → `<AssemblyName>Pedidos.API</AssemblyName>`

---

## Fase 3 — Solução e referências entre projetos

### `FacShop.slnx`

- `src/Catalogo/Catalogo.Host/Catalogo.Host.csproj` → `src/Catalogo/Catalogo.API/Catalogo.API.csproj`
- `src/Pedidos/Pedidos.Host/Pedidos.Host.csproj` → `src/Pedidos/Pedidos.API/Pedidos.API.csproj`

### `src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj`

- `<ProjectReference Include="../Catalogo.Host/Catalogo.Host.csproj" />` → `../Catalogo.API/Catalogo.API.csproj`

### `src/Pedidos/Pedidos.Tests/Pedidos.Tests.csproj`

- `<ProjectReference Include="../Pedidos.Host/Pedidos.Host.csproj" />` → `../Pedidos.API/Pedidos.API.csproj`

---

## Fase 4 — VS Code (`.vscode/`)

### `.vscode/tasks.json`

- Label `"build: Catalogo.Host"` → `"build: Catalogo.API"`
- Path `src/Catalogo/Catalogo.Host/Catalogo.Host.csproj` → `src/Catalogo/Catalogo.API/Catalogo.API.csproj`
- Label `"build: Pedidos.Host"` → `"build: Pedidos.API"`
- Path `src/Pedidos/Pedidos.Host/Pedidos.Host.csproj` → `src/Pedidos/Pedidos.API/Pedidos.API.csproj`

### `.vscode/launch.json`

Para cada configuração de Catalogo (2 configs):

- `"preLaunchTask": "build: Catalogo.Host"` → `"build: Catalogo.API"`
- `"program": "...Catalogo.Host/bin/Debug/net10.0/Catalogo.Host.dll"` → `...Catalogo.API/bin/Debug/net10.0/Catalogo.API.dll`
- `"cwd": ".../Catalogo.Host"` → `.../Catalogo.API`

Para cada configuração de Pedidos (2 configs):

- `"preLaunchTask": "build: Pedidos.Host"` → `"build: Pedidos.API"`
- `"program": "...Pedidos.Host/bin/Debug/net10.0/Pedidos.Host.dll"` → `...Pedidos.API/bin/Debug/net10.0/Pedidos.API.dll`
- `"cwd": ".../Pedidos.Host"` → `.../Pedidos.API`

---

## Fase 5 — Código-fonte

### `samples/Pix/Pix.MockServer/Security/PixMtlsCertificateStore.cs`

- `"FacShopAPI.slnx"` → `"FacShop.slnx"` (linha ~146, detecção de raiz da workspace)

---

## Fase 6 — Documentação e CLAUDE.md

### Arquivos afetados

| Arquivo                                                                 | O que muda                                                              |
| ----------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| `CLAUDE.md`                                                             | `Catalogo.Host` → `Catalogo.API`, `Pedidos.Host` → `Pedidos.API`        |
| `README.md`                                                             | `Catalogo.Host`, `Pedidos.Host`, `FacShopAPI.slnx`, título `FacShopAPI` |
| `docs/00-VISAO-GERAL.md`                                                | `Catalogo.Host`, `Pedidos.Host`, `FacShopAPI.slnx`                      |
| `docs/01-ARQUITETURA.md`                                                | `Catalogo.Host`, `Pedidos.Host`                                         |
| `docs/02-CATALOGO.md`                                                   | `Catalogo.Host`                                                         |
| `docs/05-TESTES.md`                                                     | `FacShopAPI.slnx`                                                       |
| `docs/06-ESTRUTURA.MD`                                                  | `Catalogo.Host`, `Pedidos.Host`                                         |
| `docs/07-EXECUTAR.MD`                                                   | `Catalogo.Host`, `Pedidos.Host`, `FacShopAPI.slnx`                      |
| `docs/08-IDEMPOTENCIA.md`                                               | `Catalogo.Host`                                                         |
| `docs/guias/COMO-CRIAR-NOVO-MICROSSERVICO.md`                           | `FacShopAPI.slnx`, `Catalogo.Host`, `Pedidos.Host`                      |
| `docs/guias/MELHORES-PRATICAS-MINIMAL-API.md`                           | `FacShopAPI.slnx`                                                       |
| `docs/plans/2026-05-08-separacao-auth-microservico-plan.md`             | `Catalogo.Host`, `Pedidos.Host`                                         |
| `docs/superpowers/plans/2026-05-08-correcao-readme.md`                  | `Catalogo.Host`, `Pedidos.Host`                                         |
| `docs/superpowers/plans/2026-05-07-alinhamento-testes-por-contexto.md`  | `Pedidos.Host`                                                          |
| `docs/superpowers/plans/2026-05-06-reuso-shared-evitar-duplicacao.md`   | `Catalogo.Host`, `Pedidos.Host`                                         |
| `docs/superpowers/plans/2026-05-06-padronizacao-estrutura-solucao.md`   | `Catalogo.Host`, `Pedidos.Host`, `FacShopAPI.slnx`                      |
| `docs/superpowers/plans/2026-04-17-fase3-rate-limiting.md`              | `FacShopAPI.slnx`                                                       |
| `docs/superpowers/plans/2026-04-17-fase1-migrar-produtos-catalogo.md`   | `FacShopAPI.slnx`                                                       |
| `docs/superpowers/plans/2026-04-07-cqrs-repositories.md`                | `FacShopAPI.slnx`                                                       |
| `docs/superpowers/plans/2026-05-07-padrao-consumo-api-compartilhado.md` | `Pedidos.Host`                                                          |
| `docs/plans/2026-03-02-produtos-clean-architecture.md`                  | `FacShopAPI.slnx`                                                       |
| `docs/guias/COMO-CRIAR-NOVO-MICROSSERVICO.md`                           | `Faturamento.Host` (guia de exemplo — manter padrão `.API`)             |

> **Decisão pendente:** Os planos históricos (`docs/superpowers/plans/`, `docs/plans/`) são registros do passado. Confirmar se devem ser atualizados ou mantidos como estão.

---

## Fase 7 — Validação

```bash
dotnet build FacShop.slnx
dotnet test FacShop.slnx -v minimal
```

---

## Critério de conclusão

- [ ] `dotnet build FacShop.slnx` sem erros
- [ ] `dotnet test FacShop.slnx` passa todos os testes
- [ ] `git status` não mostra arquivos com nome antigo fora de `bin/` e `obj/`
- [ ] VS Code: tasks e launch configs funcionando com os novos nomes
