# Plano de Acao - Padrao Compartilhado de Consumo de APIs

Data: 2026-05-07
Status: Proposto
Escopo: samples/Catalogo.HttpClientDemo, samples/Pix.ClientDemo, src/Shared (novo modulo de cliente HTTP)

## Objetivo

Definir e implantar um padrao unico de consumo de APIs para a solucao, reaproveitavel por Catalogo, Pedidos, Pix e futuras APIs, reduzindo duplicacao de configuracao de HttpClient, resiliencia e observabilidade.

## Analise do Estado Atual

### 1. Padrao atual em Catalogo.HttpClientDemo

Caracteristicas observadas:

- Typed client simples (CatalogoHttpClient) com metodos orientados a caso de uso.
- Registro via extensao em IHostApplicationBuilder (AddCatalogoClient).
- Pipeline de resiliencia custom com timeout por tentativa, retry exponencial com jitter, circuit breaker e timeout global.
- Uso de URL base via argumento de linha de comando.

Pontos fortes:

- Boa separacao entre programa e configuracao do client.
- Resiliencia explicita para 429 e erros transientes.
- Exemplo didatico e facil de executar.

Gaps para escala na solucao:

- Sem contrato forte de request/response (usa string e object em pontos criticos).
- Sem options tipadas padronizadas (BaseUrl, timeout, politicas, headers).
- Sem handlers cross-cutting reutilizaveis (correlation id, idempotencia, logging).
- Resiliencia definida localmente no sample, sem reaproveitamento por outros clientes.

### 2. Padrao atual em Pix.ClientDemo

Caracteristicas observadas:

- Options tipadas (PixClientOptions).
- Handlers dedicados para correlation id, idempotency key e logging.
- Cliente typed com contratos de dominio e leitura JSON tipada.
- Uso de AddStandardResilienceHandler.
- Fluxo de autenticacao encapsulado (AuthTokenProvider) com cache de token.

Pontos fortes:

- Estrutura mais madura para reaproveitamento.
- Boa separacao entre transporte, seguranca e casos de uso.
- Melhor observabilidade e composicao de pipeline.

Gaps:

- Componentes ainda presos ao contexto Pix.
- Nao existe pacote/projeto compartilhado para bootstrap padronizado de clientes HTTP na solucao.

## Decisao Conceitual

Padrao alvo:

1. Consumo entre bounded contexts por API (nao por DbContext compartilhado).
2. Snapshot local no contexto consumidor quando necessario para consistencia historica.
3. Infra de cliente HTTP compartilhada em modulo neutro (sem regra de negocio).

## Arquitetura Alvo (Padrao Compartilhado)

Criar um modulo compartilhado (sugestao: src/Shared/Http/Shared.Http.csproj) com:

1. Contratos base

- ApiClientOptionsBase (BaseUrl, timeout, politicas de retry/circuit breaker).
- IApiAuthProvider opcional para clientes que exigem token.

2. Handlers reutilizaveis

- CorrelationIdHandler.
- IdempotencyKeyHandler parametrizavel por metodo/rota.
- RequestLoggingHandler.

3. Extensoes de DI

- AddApiClientWithResilience<TClient, TOptions>().
- AddDefaultApiResiliencePipeline(...).
- AddApiObservabilityHandlers().

4. Utilitarios de serializacao/erro

- Politica padrao de System.Text.Json.
- Conversao padrao de falhas HTTP para excecoes de aplicacao (com status code e payload).

5. Convencoes de projeto

- Cada API cliente concreta fica no modulo consumidor, mas reaproveita o bootstrap do Shared.Http.
- Samples demonstram uso do padrao (nao implementam tudo localmente).

## Plano de Execucao (Passo a Passo)

### Fase 1 - Consolidar padrao tecnico (sem quebrar samples)

1. Criar projeto Shared.Http em src/Shared.
2. Extrair do Pix.ClientDemo os componentes genericos:

- CorrelationIdHandler.
- RequestLoggingHandler (sem dependencia de dominio Pix).
- Estrutura base de options.

3. Extrair do Catalogo.HttpClientDemo a politica custom de resiliencia (retry para 429 + Retry-After).
4. Criar API publica de extensoes DI para montar clients com defaults comuns.
5. Adicionar testes unitarios do Shared.Http para:

- aplicacao de handlers;
- retry para 429;
- timeout e circuit breaker;
- propagacao de correlation id.

Criterio de saida:

- Shared.Http compilando, com testes verdes e sem dependencias de dominio Catalogo/Pedidos/Pix.

### Fase 2 - Aplicar no Catalogo.HttpClientDemo

1. Refatorar o sample para usar Shared.Http em vez de configurar resiliencia inline.
2. Introduzir options tipadas (CatalogoClientOptions).
3. Evoluir CatalogoHttpClient para contratos tipados de request/response (evitar object/string cru no caminho principal).
4. Preservar o cenario didatico atual (leituras e rajada de criacao), agora sobre o padrao comum.

Criterio de saida:

- Sample Catalogo roda com o mesmo comportamento funcional, usando o bootstrap compartilhado.

### Fase 3 - Aplicar no Pix.ClientDemo

1. Trocar handlers locais pelos handlers do Shared.Http quando equivalentes.
2. Manter apenas o que e especifico de Pix (mTLS, token OAuth, semantica de endpoints).
3. Uniformizar configuracao de resiliencia para usar extensoes do Shared.Http (com overrides locais quando necessario).

Criterio de saida:

- Sample Pix permanece funcional, com reducao de codigo duplicado de infraestrutura.

### Fase 4 - Levar para APIs internas (primeiro consumidor real)

1. Criar cliente de Catalogo para uso no fluxo de Pedidos (ex.: consulta de produto para adicionar item).
2. Registrar cliente no Pedidos.Host via Shared.Http.
3. Encapsular chamada em porta de aplicacao (interface) para nao vazar HttpClient no dominio.
4. Garantir fallback e tratamento de indisponibilidade de Catalogo com mensagens de erro claras.

Criterio de saida:

- Pedidos consome Catalogo via API com resiliencia padronizada, sem acesso direto ao banco de Catalogo.

### Fase 5 - Governanca e adocao incremental

1. Definir guia curto em docs/guias com:

- template de novo API client;
- convencoes de naming;
- politicas default de resiliencia.

2. Criar checklist de PR para novos clientes HTTP.
3. Migrar gradualmente outros consumidores para Shared.Http.

Criterio de saida:

- Novo consumo de API na solucao segue um unico padrao e passa por checklist objetivo.

## Regras de Design

1. Shared.Http nao pode referenciar projetos de dominio (Catalogo/Pedidos/Pix).
2. Segredos e URLs devem vir de configuracao (Options), nunca hardcoded em codigo de cliente.
3. Handlers devem ser composiveis e testaveis isoladamente.
4. Retry deve respeitar idempotencia:

- GET pode retry por default.
- POST so com idempotency key (quando aplicavel).

5. Cliente typed deve expor metodos de caso de uso e contratos fortes.

## Riscos e Mitigacoes

1. Risco: acoplamento acidental do Shared.Http com Pix.
   Mitigacao: revisar referencias de projeto e mover apenas componentes neutros.

2. Risco: divergencia entre resiliencia custom de Catalogo e resiliencia padrao de Pix.
   Mitigacao: oferecer pipeline default + pontos de override por cliente.

3. Risco: regressao de comportamento nos samples.
   Mitigacao: validar cenarios atuais antes/depois e manter demos equivalentes.

## Checklist Operacional

- [x] Criado projeto Shared.Http e adicionado a solution.
- [x] Implementadas extensoes de DI para registro padronizado de clientes.
- [x] Implementados handlers compartilhados (correlation, idempotencia, logging).
- [x] Implementada politica de resiliencia default com suporte explicito a 429.
- [ ] Testes do Shared.Http verdes.
- [x] Catalogo.HttpClientDemo migrado para Shared.Http.
- [x] Pix.ClientDemo migrado para Shared.Http (mantendo mTLS/OAuth especificos).
- [x] Primeiro consumidor real (Pedidos -> Catalogo API) implementado via Shared.Http.
- [ ] Guia de uso publicado em docs/guias.

## Ordem Recomendada de Implementacao

1. Fase 1 (fundacao compartilhada).
2. Fase 2 (sample Catalogo).
3. Fase 3 (sample Pix).
4. Fase 4 (Pedidos consumindo Catalogo).
5. Fase 5 (governanca e expansao).
