# FacShopAPI

Projeto educacional em .NET 10 Minimal API demonstrando três bounded contexts com padrões arquiteturais distintos coexistindo no mesmo repositório.

---

## Contexto

O projeto explora três bounded contexts como objetos de estudo:

- **Catálogo** — Clean Architecture híbrida (Domain / Application / Infrastructure / API), com 5 recursos: Produto, Categoria, Variante, Atributo e Mídia. Rate limiting com 3 políticas.
- **Pedidos** — Vertical Slice + Domínio Rico. Organização por feature, agregado rico com Result pattern.
- **Pix** — Mock Server com mTLS + OAuth2 e cliente HTTP tipado com resiliência. Demonstra integração com API externa de pagamentos.

Nenhum padrão é prescrito como "o correto" — a coexistência intencional é o ponto central do aprendizado.

---

## Início Rápido

**Pré-requisitos:** .NET 10 SDK

```bash
git clone https://github.com/seu-usuario/net-minimal-api.git
cd net-minimal-api
```

O projeto possui três hosts independentes. Rode cada um em um terminal separado:

```bash
# Auth (emissor JWT) — Swagger: http://localhost:5020
dotnet run --project src/Auth/Auth.Host/Auth.Host.csproj

# Catálogo — Swagger: http://localhost:5000
dotnet run --project src/Catalogo/Catalogo.Host/Catalogo.Host.csproj

# Pedidos — Swagger: http://localhost:5001
dotnet run --project src/Pedidos/Pedidos.Host/Pedidos.Host.csproj
```

---

## Bounded Contexts

| Contexto | Padrão                        | Rotas base           | Descrição                                                 |
| -------- | ----------------------------- | -------------------- | --------------------------------------------------------- |
| Auth     | Vertical Slice                | `/api/v1/auth/*`     | Emissor JWT; demais serviços apenas validam o token       |
| Catálogo | Clean Architecture híbrida    | `/api/v1/catalogo/*` | 5 recursos com CRUD completo, rate limiting e soft delete |
| Pedidos  | Vertical Slice + Domínio Rico | `/api/v1/pedidos/*`  | Agregado rico, Result pattern, auth obrigatório           |
| Pix      | Mock Server + HTTP Client     | `/pix/v1/*` (mock)   | mTLS, OAuth2, idempotência, resiliência                   |

---

## Estrutura de Diretórios

```
net-minimal-api/
├── FacShopAPI.slnx
│
├── src/
│   ├── Auth/
│   │   ├── Auth.Endpoints/
│   │   └── Auth.Host/
│   │
│   ├── Catalogo/
│   │   ├── Catalogo.Common/
│   │   ├── Catalogo.Data/
│   │   ├── Catalogo.Domain/
│   │   ├── Catalogo.Application/
│   │   ├── Catalogo.Infrastructure/
│   │   ├── Catalogo.Endpoints/
│   │   ├── Catalogo.Host/
│   │   └── Catalogo.Tests/
│   │
│   ├── Pedidos/
│   │   ├── Pedidos.Common/
│   │   ├── Pedidos.Data/
│   │   ├── Pedidos.Domain/
│   │   ├── Pedidos.Application/
│   │   ├── Pedidos.Infrastructure/
│   │   ├── Pedidos.Endpoints/
│   │   │   ├── CreatePedido/
│   │   │   ├── GetPedido/
│   │   │   ├── ListPedidos/
│   │   │   ├── AddItemPedido/
│   │   │   └── CancelPedido/
│   │   ├── Pedidos.Host/
│   │   └── Pedidos.Tests/
│   │
│   └── Shared/
│       ├── Data/
│       ├── Http/
│       ├── Kernel/
│       └── Web/
│
└── samples/
    ├── Catalogo.HttpClientDemo/
    └── Pix/
        ├── Pix.MockServer/
        ├── Pix.ClientDemo/
        └── Pix.MockServer.Tests/
```

---

## Endpoints

### Autenticação

| Método | Rota                 | Descrição                                                                                     |
| ------ | -------------------- | --------------------------------------------------------------------------------------------- |
| `POST` | `/api/v1/auth/login` | Retorna JWT no microserviço Auth. Body: `{"email": "admin@example.com", "senha": "senha123"}` |

Para execução local, obtenha o token no Auth.Host (ex.: `http://localhost:5020/api/v1/auth/login`) e use o bearer token nas chamadas protegidas de Catalogo/Pedidos.

### Catálogo

| Método   | Rota                                      | Auth | Observações                              |
| -------- | ----------------------------------------- | ---- | ---------------------------------------- |
| `GET`    | `/api/v1/catalogo/produtos`               | —    | Paginado; filtros: `categoria`, `search` |
| `GET`    | `/api/v1/catalogo/produtos/{id}`          | —    |                                          |
| `POST`   | `/api/v1/catalogo/produtos`               | JWT  | Rate limit: `criacao-produto`            |
| `PUT`    | `/api/v1/catalogo/produtos/{id}`          | JWT  |                                          |
| `PATCH`  | `/api/v1/catalogo/produtos/{id}`          | JWT  |                                          |
| `DELETE` | `/api/v1/catalogo/produtos/{id}`          | JWT  | Soft delete (seta `Ativo = false`)       |
| `GET`    | `/api/v1/catalogo/categorias`             | —    |                                          |
| `GET`    | `/api/v1/catalogo/categorias/{id}`        | —    |                                          |
| `POST`   | `/api/v1/catalogo/categorias`             | JWT  |                                          |
| `PUT`    | `/api/v1/catalogo/categorias/{id}`        | JWT  |                                          |
| `DELETE` | `/api/v1/catalogo/categorias/{id}`        | JWT  |                                          |
| `GET`    | `/api/v1/catalogo/variantes`              | —    | Query: `?produtoId={id}`                 |
| `GET`    | `/api/v1/catalogo/variantes/{id}`         | —    |                                          |
| `POST`   | `/api/v1/catalogo/variantes`              | JWT  |                                          |
| `PUT`    | `/api/v1/catalogo/variantes/{id}`         | JWT  |                                          |
| `PATCH`  | `/api/v1/catalogo/variantes/{id}/estoque` | JWT  |                                          |
| `DELETE` | `/api/v1/catalogo/variantes/{id}`         | JWT  |                                          |
| `GET`    | `/api/v1/catalogo/atributos`              | —    | Query: `?produtoId={id}`                 |
| `POST`   | `/api/v1/catalogo/atributos`              | JWT  |                                          |
| `PUT`    | `/api/v1/catalogo/atributos/{id}`         | JWT  |                                          |
| `DELETE` | `/api/v1/catalogo/atributos/{id}`         | JWT  |                                          |
| `GET`    | `/api/v1/catalogo/midias`                 | —    | Query: `?produtoId={id}`                 |
| `POST`   | `/api/v1/catalogo/midias`                 | JWT  |                                          |
| `PATCH`  | `/api/v1/catalogo/midias/{id}/ordem`      | JWT  |                                          |
| `DELETE` | `/api/v1/catalogo/midias/{id}`            | JWT  |                                          |

### Pedidos

| Método | Rota                            | Auth |
| ------ | ------------------------------- | ---- |
| `POST` | `/api/v1/pedidos`               | JWT  |
| `GET`  | `/api/v1/pedidos`               | JWT  |
| `GET`  | `/api/v1/pedidos/{id}`          | JWT  |
| `POST` | `/api/v1/pedidos/{id}/itens`    | JWT  |
| `POST` | `/api/v1/pedidos/{id}/cancelar` | JWT  |

---

## Testes

| Projeto                | Testes (aprox.) |
| ---------------------- | --------------- |
| `Catalogo.Tests`       | 116             |
| `Pedidos.Tests`        | 47              |
| `Pix.MockServer.Tests` | 7               |
| **Total**              | **170**         |

```bash
# Catálogo
dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj

# Pedidos
dotnet test src/Pedidos/Pedidos.Tests/Pedidos.Tests.csproj

# Mock server PIX
dotnet test samples/Pix/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj

# Solução completa
dotnet test FacShopAPI.slnx
```

---

## Documentação

| Arquivo                                            | Conteúdo                                                      |
| -------------------------------------------------- | ------------------------------------------------------------- |
| [docs/00-VISAO-GERAL.md](docs/00-VISAO-GERAL.md)   | Visão geral e orientação de leitura                           |
| [docs/01-ARQUITETURA.md](docs/01-ARQUITETURA.md)   | Diagramas e decisões arquiteturais                            |
| [docs/02-CATALOGO.md](docs/02-CATALOGO.md)         | Catálogo: Clean Architecture híbrida, recursos, rate limiting |
| [docs/03-PEDIDOS.md](docs/03-PEDIDOS.md)           | Pedidos: Vertical Slice, domínio rico, Result pattern         |
| [docs/04-PIX.md](docs/04-PIX.md)                   | Pix: Mock Server, mTLS, OAuth2, cliente HTTP                  |
| [docs/05-TESTES.md](docs/05-TESTES.md)             | Estratégia de testes, factories, helpers                      |
| [docs/06-ESTRUTURA.MD](docs/06-ESTRUTURA.MD)       | Estrutura de projetos e grafo de dependências                 |
| [docs/07-EXECUTAR.MD](docs/07-EXECUTAR.MD)         | Comandos para executar hosts, samples e testes                |
| [docs/08-IDEMPOTENCIA.md](docs/08-IDEMPOTENCIA.md) | Overview completo do controle de idempotência                 |
| [docs/ADRs/](docs/ADRs/)                           | 16 ADRs no formato MADR 3.x                                   |
