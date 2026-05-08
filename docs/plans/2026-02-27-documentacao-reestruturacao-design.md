# Design: ReestruturaÃ§Ã£o da DocumentaÃ§Ã£o

**Data:** 2026-02-27
**Status:** Aprovado

---

## Contexto

A documentaÃ§Ã£o do projeto reflete apenas a primeira fase (Produtos com camadas horizontais). A segunda fase (Pedidos com Vertical Slice + DomÃ­nio Rico) estÃ¡ no cÃ³digo mas invisÃ­vel nos docs. Todos os guias omitem: Vertical Slice Architecture, DomÃ­nio Rico, Result Pattern, o agregado Pedido, e os 13 testes de integraÃ§Ã£o HTTP. A contagem de testes nos docs diz "50+" quando o projeto tem 111.

---

## Objetivo

Reestruturar toda a documentaÃ§Ã£o para:
1. Refletir fielmente o estado atual do cÃ³digo
2. Organizar o aprendizado em duas trilhas complementares
3. Criar um novo guia conceitual sobre Vertical Slice e DomÃ­nio Rico

---

## Nova Arquitetura de InformaÃ§Ã£o

```
docs/
â”œâ”€â”€ NavegaÃ§Ã£o
â”‚   â”œâ”€â”€ 00-LEIA-PRIMEIRO.md     â† reescrita: narrativa dos 2 casos de uso
â”‚   â”œâ”€â”€ INDEX.md                â† reescrita: 2 trilhas de aprendizado
â”‚   â””â”€â”€ INICIO-RAPIDO.md        â† atualizaÃ§Ã£o: endpoints de Pedidos + auth
â”‚
â”œâ”€â”€ Guias Conceituais
â”‚   â”œâ”€â”€ MELHORES-PRATICAS-API.md           â† sem alteraÃ§Ãµes
â”‚   â””â”€â”€ VERTICAL-SLICE-DOMINIO-RICO.md     â† NOVO
â”‚
â”œâ”€â”€ Guias de ImplementaÃ§Ã£o
â”‚   â”œâ”€â”€ MELHORES-PRATICAS-MINIMAL-API.md   â† adiÃ§Ã£o: capÃ­tulo Pedidos
â”‚   â””â”€â”€ MELHORIAS-DOTNET-10.md             â† adiÃ§Ã£o: features dos slices
â”‚
â””â”€â”€ ReferÃªncia
    â”œâ”€â”€ ARQUITETURA.md     â† reescrita: diagrama duplo + coexistÃªncia
    â”œâ”€â”€ CHECKLIST.md       â† adiÃ§Ã£o: seÃ§Ã£o Pedidos
    â””â”€â”€ ENTREGA-FINAL.md   â† atualizaÃ§Ã£o: reflete os 2 casos de uso

FacShopAPI.Tests/
â””â”€â”€ ESTRATEGIA-DE-TESTES.md  â† reescrita: 3 categorias, 111 testes

README.md  â† atualizaÃ§Ã£o pesada: porta de entrada para os 2 padrÃµes
```

---

## Duas Trilhas de Aprendizado

### Trilha 1 â€” REST + Camadas Horizontais (Produtos)
1. `MELHORES-PRATICAS-API.md` â€” teoria REST universal
2. `MELHORES-PRATICAS-MINIMAL-API.md` â†’ seÃ§Ã£o Produtos
3. CÃ³digo: `src/Endpoints/` + `src/Services/` + `src/Models/Produto.cs`
4. Testes: unit tests de serviÃ§o e validaÃ§Ã£o

### Trilha 2 â€” Vertical Slice + DomÃ­nio Rico (Pedidos)
1. `VERTICAL-SLICE-DOMINIO-RICO.md` â€” teoria: slices + domÃ­nio rico + Result pattern
2. `MELHORES-PRATICAS-MINIMAL-API.md` â†’ seÃ§Ã£o Pedidos
3. CÃ³digo: `src/Features/Pedidos/`
4. Testes: domain unit tests + integration tests HTTP

---

## Escopo de MudanÃ§as por Arquivo

| Arquivo | Tipo | O que muda |
|---|---|---|
| `README.md` | AtualizaÃ§Ã£o pesada | +Pedidos endpoints, +111 testes, estrutura atualizada, v3.0.0 |
| `00-LEIA-PRIMEIRO.md` | Reescrita | IntroduÃ§Ã£o narrativa aos 2 casos de uso, sem lista exaustiva de arquivos |
| `INDEX.md` | Reescrita | 2 trilhas, mapa mental atualizado, remove sugestÃµes jÃ¡ implementadas |
| `ARQUITETURA.md` | Reescrita | Diagrama duplo lado a lado, coexistÃªncia, data model com Pedidos+PedidoItens |
| `ESTRATEGIA-DE-TESTES.md` | Reescrita | 3 categorias reais: Domain Unit, Service Unit, Integration HTTP; 111 testes |
| `MELHORES-PRATICAS-MINIMAL-API.md` | AdiÃ§Ã£o | Novo capÃ­tulo sobre Pedidos (slices, handlers, Result pattern) |
| `MELHORIAS-DOTNET-10.md` | AdiÃ§Ã£o | IEndpoint scan, collection expressions `[]`, primary constructors em handlers |
| `ENTREGA-FINAL.md` | AtualizaÃ§Ã£o | Reflete os 2 casos de uso, domÃ­nio rico, testes de integraÃ§Ã£o |
| `CHECKLIST.md` | AdiÃ§Ã£o | SeÃ§Ã£o Pedidos (5 slices, domÃ­nio, testes) |
| `INICIO-RAPIDO.md` | AtualizaÃ§Ã£o | Exemplos curl com JWT + endpoints de Pedidos |
| `VERTICAL-SLICE-DOMINIO-RICO.md` | **NOVO** | Guia conceitual completo |

---

## ConteÃºdo do Novo Guia (VERTICAL-SLICE-DOMINIO-RICO.md)

1. **O Problema** â€” por que camadas horizontais tÃªm limites (feature atravessa 5 arquivos)
2. **Vertical Slice Architecture** â€” o que Ã©, anatomia de um slice (Command/Handler/Validator/Endpoint), IEndpoint + scan automÃ¡tico
3. **Modelo AnÃªmico vs DomÃ­nio Rico** â€” comparaÃ§Ã£o direta com cÃ³digo do prÃ³prio projeto (`Produto` antes/depois, `Pedido` como exemplo maduro)
4. **Result Pattern** â€” erros de domÃ­nio sem exceptions, `Result<T>` records
5. **Aggregate Root** â€” `Pedido` como exemplo: invariantes, encapsulamento, regras de negÃ³cio no domÃ­nio
6. **CoexistÃªncia dos PadrÃµes** â€” quando usar cada um, como compartilham AppDbContext e middleware
7. **ReferÃªncias** para o cÃ³digo em `src/Features/Pedidos/`
