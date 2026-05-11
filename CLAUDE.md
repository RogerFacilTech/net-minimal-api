# CLAUDE.md

## Contexto do Projeto

Projeto **educacional** demonstrando padrões arquiteturais coexistindo intencionalmente:

- **Catálogo** — Clean Architecture híbrida com 4 sub-projetos (`Catalogo.Domain`, `Catalogo.Application`, `Catalogo.Infrastructure`, `Catalogo.API`) em `src/Catalogo/`. Cinco recursos: Produto, Categoria, Variante, Atributo, Mídia. Rate limiting com 3 políticas.
- **Pedidos** — Vertical Slice + Domínio Rico em `src/Pedidos/`. Organização por feature, agregado rico com Result pattern.
- **Pix** — Mock Server (`samples/Pix/Pix.MockServer/`) + cliente HTTP (`samples/Pix/Pix.ClientDemo/`). mTLS + OAuth2.

## Convenções Não-Óbvias

**Soft delete:** `DELETE /catalogo/produtos/{id}` seta `Ativo = false`. Produto inativo retorna 404 em **todos** os endpoints — o filtro é responsabilidade do service/repositório.

**Error handling:**

- Services do Catálogo retornam `Result<T>` — nunca lançam exceções. Sempre checar `IsSuccess` antes de `.Value`.
- Código mais antigo (Pedidos) pode retornar `null` para not found em alguns serviços — verifique o contrato de cada service antes de assumir.

**Auth:** Endpoints de escrita exigem `RequireAuthorization()`. Endpoints GET usam `AllowAnonymous()`.

**Rate limiting:** 3 políticas definidas no Catálogo:

- `leitura` — FixedWindow, 60 req/min
- `escrita` — SlidingWindow, 20 req/min
- `criacao-produto` — TokenBucket, 5 req/min

Rate limiting **não é registrado** no ambiente `Testing`. Cada subclasse de `ApiFactory` registra suas próprias políticas com limites ajustados para testes.

**Idempotência automática:** `IdempotencyMiddleware` intercepta POST/PUT/PATCH via header `Idempotency-Key`. Não reimplemente no handler.

**Novos endpoints em Pedidos** devem implementar `IEndpoint` (`src/Shared/Web/IEndpoint.cs`) — são registrados automaticamente por reflection. **Não** cadastre manualmente no `Program.cs`.

**Endpoints do Catálogo** devem declarar `RequireRateLimiting("leitura")`, `RequireRateLimiting("escrita")` ou `RequireRateLimiting("criacao-produto")` conforme o tipo de operação.

## Testes

Banco InMemory ativado por `Environment = "Testing"`.

O `DbSeeder` popula:

- **8 produtos** (IDs 1–8) — testes que criam produtos começam do ID 9
- **5 categorias** (IDs 1–5) — testes que criam categorias começam do ID 6

Autenticação nos testes via `AuthHelper.ObterTokenAsync(client)` — credenciais hardcoded `admin@example.com` / `senha123`.

**Factories:**

- `ApiFactory` — uso geral. Registra todas as políticas de rate limiting com limite 10.000 para não interferir nos testes.
- `RateLimitingApiFactory` — exclusivo para testes de rate limiting. Limites reduzidos: `leitura=3`, `escrita=3`, `criacao-produto=2`.

## Planos de Implementação

Planos ficam em `docs/superpowers/plans/`. ADRs ficam em `docs/ADRs/` (16 ADRs no formato MADR 3.x com frontmatter). Leia o ADR relevante antes de qualquer mudança arquitetural significativa.
