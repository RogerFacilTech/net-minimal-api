# Overview — Controle de Idempotência

**Data:** 2026-05-08  
**Status:** Aprovado  
**ADR relacionado:** [ADR-0006](ADRs/ADR-0006-idempotencia-via-middleware.md)

---

## Resumo

O projeto implementa idempotência em **dois pontos distintos**: no servidor (middleware centralizado) e no cliente HTTP (delegating handler). Há ainda uma terceira implementação específica para o Pix MockServer que replica o comportamento da API real do Banco Central.

---

## 1. Lado Servidor — `IdempotencyMiddleware`

**Arquivo:** `src/Catalogo/Catalogo.API/Middleware/IdempotencyMiddleware.cs`  
**Registrado em:** `src/Catalogo/Catalogo.API/Program.cs` via `app.UseMiddleware<IdempotencyMiddleware>()`

### Fluxo

```
POST/PUT/PATCH chegando
    │
    ├─ Header "Idempotency-Key" presente?
    │   ├─ Não → passa para o próximo middleware sem proteção (permissivo)
    │   └─ Sim → verifica IMemoryCache
    │               ├─ Chave encontrada → curto-circuita, devolve resposta cacheada (sem executar handler)
    │               └─ Chave nova → executa handler normalmente
    │                               └─ Resposta 2xx → grava (StatusCode + ContentType + Body) no cache por 24h
    │
    └─ GET/HEAD/OPTIONS/DELETE → passa sem verificação (já idempotentes por natureza)
```

### Regras

| Situação                     | Comportamento                                     |
| ---------------------------- | ------------------------------------------------- |
| Header ausente               | Passa sem proteção                                |
| Chave nova, resposta 2xx     | Processa e cacheia por 24h                        |
| Chave nova, resposta não-2xx | Processa sem cachear (erros não são reproduzidos) |
| Chave já vista               | Devolve resposta cacheada sem executar handler    |

### Limitações conhecidas

- Usa `IMemoryCache` — volátil, não sobrevive a restarts
- Em múltiplas instâncias (load balancer), cada instância tem seu próprio cache — a garantia é quebrada
- Em produção: substituir por Redis ou tabela de idempotência persistida em banco

---

## 2. Lado Cliente — `IdempotencyKeyHandler`

**Arquivos:**

- `src/Shared/Http/IdempotencyKeyHandler.cs`
- `src/Shared/Http/IdempotencyKeyOptions.cs`

É um `DelegatingHandler` para injeção na cadeia do `HttpClient`. Injeta automaticamente o header `Idempotency-Key` com um `Guid` novo por requisição, desde que:

- O método HTTP esteja na lista `Options.Methods` (padrão: `POST`, `PUT`, `PATCH`)
- O path satisfaça `Options.PathContains` (lista vazia = todos os paths)
- A requisição **ainda não tenha** o header (não sobrescreve valor existente)

### Opções configuráveis

```csharp
public class IdempotencyKeyOptions
{
    public string HeaderName { get; set; } = "Idempotency-Key";
    public List<string> Methods { get; set; } = ["POST", "PUT", "PATCH"];
    public List<string> PathContains { get; set; } = [];  // vazio = todos os paths
}
```

---

## 3. Pix MockServer — Comportamento BCB

**Arquivos:**

- `samples/Pix/Pix.MockServer/` (implementação)
- `samples/Pix/Pix.MockServer.Tests/PixMockServerTests.cs` (testes)

O MockServer replica o comportamento da API real do Banco Central para cobranças Pix:

| Situação                        | Comportamento                          |
| ------------------------------- | -------------------------------------- |
| Mesma chave + mesmo payload     | Retorna a mesma cobrança (idempotente) |
| Mesma chave + payload diferente | Retorna `409 Conflict`                 |
| Chave nova                      | Cria nova cobrança                     |

Exemplo de requisição:

```http
POST /v2/cob/txid
Idempotency-Key: <uuid>
Content-Type: application/json
```

---

## Documentação Relacionada

| Documento                                                  | Conteúdo                                                                  |
| ---------------------------------------------------------- | ------------------------------------------------------------------------- |
| [ADR-0006](ADRs/ADR-0006-idempotencia-via-middleware.md)   | Decisão arquitetural: contexto, alternativas consideradas e consequências |
| [01-ARQUITETURA.md](01-ARQUITETURA.md)                     | Tabela de middlewares e diagrama do pipeline de requisição                |
| [MELHORES-PRATICAS-API.md](guias/MELHORES-PRATICAS-API.md) | Seção 3 — Verbos HTTP e Idempotência, com código de exemplo               |
| [04-PIX.md](04-PIX.md)                                     | Fluxo ponta a ponta de idempotência em operações financeiras simuladas    |
