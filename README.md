# FacShopAPI

Projeto educacional em .NET 10 Minimal API demonstrando trÃªs bounded contexts com padrÃµes arquiteturais distintos coexistindo no mesmo repositÃ³rio.

---

## Contexto

O projeto explora trÃªs bounded contexts como objetos de estudo:

- **CatÃ¡logo** â€” Clean Architecture hÃ­brida (Domain / Application / Infrastructure / API), com 5 recursos: Produto, Categoria, Variante, Atributo e MÃ­dia. Rate limiting com 3 polÃ­ticas.
- **Pedidos** â€” Vertical Slice + DomÃ­nio Rico. OrganizaÃ§Ã£o por feature, agregado rico com Result pattern.
- **Pix** â€” Mock Server com mTLS + OAuth2 e cliente HTTP tipado com resiliÃªncia. Demonstra integraÃ§Ã£o com API externa de pagamentos.

Nenhum padrÃ£o Ã© prescrito como "o correto" â€” a coexistÃªncia intencional Ã© o ponto central do aprendizado.

---

## InÃ­cio RÃ¡pido

**PrÃ©-requisitos:** .NET 10 SDK

```bash
git clone https://github.com/seu-usuario/net-minimal-api.git
cd net-minimal-api
dotnet run
```

Swagger disponÃ­vel em: http://localhost:5000

---

## Bounded Contexts

| Contexto  | PadrÃ£o                        | Rotas base           | DescriÃ§Ã£o                                               |
| --------- | ------------------------------ | -------------------- | --------------------------------------------------------- |
| CatÃ¡logo | Clean Architecture hÃ­brida    | `/api/v1/catalogo/*` | 5 recursos com CRUD completo, rate limiting e soft delete |
| Pedidos   | Vertical Slice + DomÃ­nio Rico | `/api/v1/pedidos/*`  | Agregado rico, Result pattern, auth obrigatÃ³rio          |
| Pix       | Mock Server + HTTP Client      | `/pix/v1/*` (mock)   | mTLS, OAuth2, idempotÃªncia, resiliÃªncia                 |

---

## Estrutura de DiretÃ³rios

```
net-minimal-api/
â”œâ”€â”€ Program.cs
â”œâ”€â”€ FacShopAPI.csproj
â”œâ”€â”€ FacShopAPI.slnx
â”‚
â”œâ”€â”€ src/
â”‚   â”œâ”€â”€ Catalogo/
â”‚   â”‚   â”œâ”€â”€ Catalogo.Domain/
â”‚   â”‚   â”œâ”€â”€ Catalogo.Application/
â”‚   â”‚   â”œâ”€â”€ Catalogo.Infrastructure/
â”‚   â”‚   â”œâ”€â”€ Catalogo.API/
â”‚   â”‚   â””â”€â”€ Catalogo.ClientDemo/
â”‚   â”œâ”€â”€ Pedidos/
â”‚   â”‚   â”œâ”€â”€ CreatePedido/
â”‚   â”‚   â”œâ”€â”€ GetPedido/
â”‚   â”‚   â”œâ”€â”€ ListPedidos/
â”‚   â”‚   â”œâ”€â”€ CancelarPedido/
â”‚   â”‚   â”œâ”€â”€ AdicionarItem/
â”‚   â”‚   â””â”€â”€ Domain/
â”‚   â”œâ”€â”€ Pix/
â”‚   â”‚   â”œâ”€â”€ Pix.MockServer/
â”‚   â”‚   â””â”€â”€ Pix.ClientDemo/
â”‚   â””â”€â”€ Shared/
â”‚       â”œâ”€â”€ Common/
â”‚       â”œâ”€â”€ Data/
â”‚       â””â”€â”€ Middleware/
â”‚
â””â”€â”€ tests/
    â”œâ”€â”€ FacShopAPI.Tests/
    â””â”€â”€ Pix.MockServer.Tests/
```

---

## Endpoints

### AutenticaÃ§Ã£o

| MÃ©todo | Rota                 | DescriÃ§Ã£o                                                                                    |
| ------- | -------------------- | ---------------------------------------------------------------------------------------------- |
| `POST`  | `/api/v1/auth/login` | Retorna JWT no microserviÃ§o Auth. Body: `{"email": "admin@example.com", "senha": "senha123"}` |

Para execuÃ§Ã£o local, obtenha o token no Auth.Host (ex.: `http://localhost:5020/api/v1/auth/login`) e use o bearer token nas chamadas protegidas de Catalogo/Pedidos.

### CatÃ¡logo

| MÃ©todo  | Rota                                      | Auth | ObservaÃ§Ãµes                            |
| -------- | ----------------------------------------- | ---- | ---------------------------------------- |
| `GET`    | `/api/v1/catalogo/produtos`               | â€”  | Paginado; filtros: `categoria`, `search` |
| `GET`    | `/api/v1/catalogo/produtos/{id}`          | â€”  |                                          |
| `POST`   | `/api/v1/catalogo/produtos`               | JWT  | Rate limit: `criacao-produto`            |
| `PUT`    | `/api/v1/catalogo/produtos/{id}`          | JWT  |                                          |
| `PATCH`  | `/api/v1/catalogo/produtos/{id}`          | JWT  |                                          |
| `DELETE` | `/api/v1/catalogo/produtos/{id}`          | JWT  | Soft delete (seta `Ativo = false`)       |
| `GET`    | `/api/v1/catalogo/categorias`             | â€”  |                                          |
| `GET`    | `/api/v1/catalogo/categorias/{id}`        | â€”  |                                          |
| `POST`   | `/api/v1/catalogo/categorias`             | JWT  |                                          |
| `PUT`    | `/api/v1/catalogo/categorias/{id}`        | JWT  |                                          |
| `DELETE` | `/api/v1/catalogo/categorias/{id}`        | JWT  |                                          |
| `GET`    | `/api/v1/catalogo/variantes`              | â€”  | Query: `?produtoId={id}`                 |
| `GET`    | `/api/v1/catalogo/variantes/{id}`         | â€”  |                                          |
| `POST`   | `/api/v1/catalogo/variantes`              | JWT  |                                          |
| `PUT`    | `/api/v1/catalogo/variantes/{id}`         | JWT  |                                          |
| `PATCH`  | `/api/v1/catalogo/variantes/{id}/estoque` | JWT  |                                          |
| `DELETE` | `/api/v1/catalogo/variantes/{id}`         | JWT  |                                          |
| `GET`    | `/api/v1/catalogo/atributos`              | â€”  | Query: `?produtoId={id}`                 |
| `POST`   | `/api/v1/catalogo/atributos`              | JWT  |                                          |
| `PUT`    | `/api/v1/catalogo/atributos/{id}`         | JWT  |                                          |
| `DELETE` | `/api/v1/catalogo/atributos/{id}`         | JWT  |                                          |
| `GET`    | `/api/v1/catalogo/midias`                 | â€”  | Query: `?produtoId={id}`                 |
| `POST`   | `/api/v1/catalogo/midias`                 | JWT  |                                          |
| `PATCH`  | `/api/v1/catalogo/midias/{id}/ordem`      | JWT  |                                          |
| `DELETE` | `/api/v1/catalogo/midias/{id}`            | JWT  |                                          |

### Pedidos

| MÃ©todo | Rota                            | Auth |
| ------- | ------------------------------- | ---- |
| `POST`  | `/api/v1/pedidos`               | JWT  |
| `GET`   | `/api/v1/pedidos`               | JWT  |
| `GET`   | `/api/v1/pedidos/{id}`          | JWT  |
| `POST`  | `/api/v1/pedidos/{id}/itens`    | JWT  |
| `POST`  | `/api/v1/pedidos/{id}/cancelar` | JWT  |

---

## Testes

| Projeto                | Testes  |
| ---------------------- | ------- |
| `FacShopAPI.Tests`     | 143     |
| `Pix.MockServer.Tests` | 7       |
| **Total**              | **150** |

```bash
# Projeto principal
dotnet test tests/FacShopAPI.Tests/

# Mock server PIX
dotnet test samples/Pix/Pix.MockServer.Tests/

# SoluÃ§Ã£o completa
dotnet test FacShopAPI.slnx
```

---

## DocumentaÃ§Ã£o

| Arquivo                                          | ConteÃºdo                                                       |
| ------------------------------------------------ | --------------------------------------------------------------- |
| [docs/00-VISAO-GERAL.md](docs/00-VISAO-GERAL.md) | VisÃ£o geral e orientaÃ§Ã£o de leitura                          |
| [docs/01-ARQUITETURA.md](docs/01-ARQUITETURA.md) | Diagramas e decisÃµes arquiteturais                             |
| [docs/02-CATALOGO.md](docs/02-CATALOGO.md)       | CatÃ¡logo: Clean Architecture hÃ­brida, recursos, rate limiting |
| [docs/03-PEDIDOS.md](docs/03-PEDIDOS.md)         | Pedidos: Vertical Slice, domÃ­nio rico, Result pattern          |
| [docs/04-PIX.md](docs/04-PIX.md)                 | Pix: Mock Server, mTLS, OAuth2, cliente HTTP                    |
| [docs/05-TESTES.md](docs/05-TESTES.md)           | EstratÃ©gia de testes, factories, helpers                       |
| [docs/ADRs/](docs/ADRs/)                         | 15 ADRs no formato MADR 3.x                                     |
