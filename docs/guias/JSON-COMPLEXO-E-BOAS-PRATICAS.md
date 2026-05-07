# JSON Complexo e Boas Práticas de APIs

## Objetivo

Este guia consolida as práticas usadas no projeto para modelagem, validação e evolução de contratos JSON, com foco em cenários financeiros (PIX) e APIs REST em geral.

## Princípios adotados

- Estruturar payload por contexto de domínio (`devedor`, `recebedor`, `valor`, `metadata`) em vez de JSON monolítico.
- Usar nomes consistentes em `camelCase`.
- Diferenciar contrato de entrada e saída (request vs response).
- Versionar contrato no nível de rota (`/api/v1`, `/pix/v1`) quando houver breaking change.
- Padronizar erros com `application/problem+json`.
- Tornar operações sensíveis idempotentes com `Idempotency-Key`.
- Correlacionar chamadas com `X-Correlation-Id`.

## Organização recomendada de contratos

- `Contracts/` para payloads HTTP explícitos.
- `Domain/` para estado interno (não expor entidades de domínio como resposta pública).
- `Application/` para validação e transformação entre request/response e regras de negócio.

No projeto:

- `samples/Pix/Pix.MockServer/Contracts/`
- `samples/Pix/Pix.MockServer/Application/PixValidation.cs`
- `samples/Pix/Pix.MockServer/Application/JsonContext.cs`

## Exemplos JSON do projeto

Os exemplos prontos estão em `docs/examples/pix-json/`:

- [oauth-token-request.json](examples/pix-json/oauth-token-request.json)
- [oauth-token-response.json](examples/pix-json/oauth-token-response.json)
- [cobranca-create-request.json](examples/pix-json/cobranca-create-request.json)
- [cobranca-create-response.json](examples/pix-json/cobranca-create-response.json)
- [devolucao-create-request.json](examples/pix-json/devolucao-create-request.json)
- [devolucao-create-response.json](examples/pix-json/devolucao-create-response.json)
- [problem-validation-response.json](examples/pix-json/problem-validation-response.json)
- [problem-idempotency-conflict-response.json](examples/pix-json/problem-idempotency-conflict-response.json)

## Boas práticas de validação

- Validar formato e obrigatoriedade de campos antes de lógica de negócio.
- Validar limites monetários (`valor > 0`, teto por operação).
- Validar documento e campos de endereço essenciais.
- Retornar mensagens claras por campo no `ValidationProblem`.

## Boas práticas de serialização

- Centralizar opções `System.Text.Json` (naming policy, null handling, datas ISO-8601 UTC).
- Evitar dicionários arbitrários sem controle para dados críticos; quando necessário, usar `metadata` restrito.
- Para performance e previsibilidade, usar `JsonSerializerContext` (source generation) em payloads críticos.

## Erros padronizados

Para erros de contrato/negócio, usar `application/problem+json` com:

- `title`
- `detail`
- `status`
- `traceId`
- `correlationId`

## Checklist rápido para novos contratos JSON

- [ ] Payload separado por contexto (sem flat JSON excessivo)
- [ ] Exemplo request e response documentados
- [ ] Validação por campo implementada
- [ ] Erro padronizado com problem+json
- [ ] Idempotência definida para operações de criação/processamento
