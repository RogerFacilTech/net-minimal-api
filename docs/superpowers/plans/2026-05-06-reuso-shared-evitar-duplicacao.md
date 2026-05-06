# Plano de Acao - Reuso de Shared para Evitar Duplicacao

Data: 2026-05-06
Status: Proposto
Escopo: Catalogo e Pedidos (impacto principal), Shared.Common e Shared.Data

## Contexto

A analise identificou duplicacao explicita do tipo Result no Catalogo e no Shared.Common. Ao mesmo tempo, IEndpoint e EndpointExtensions ja estao sendo reutilizados via Shared.Common, e AppDbContext ja esta centralizado no Shared.Data.

Objetivo do plano: reduzir duplicacao de codigo sem aumentar acoplamento indevido entre contextos.

## Achados Principais

1. Duplicacao de Result

- Shared: src/Shared/Common/Result.cs
- Duplicado: src/Catalogo/Catalogo.Domain/Common/Result.cs

2. Reuso existente e valido

- IEndpoint e EndpointExtensions em Shared.Common ja sao usados no Pedidos.
- AppDbContext em Shared.Data ja e usado por Catalogo e Pedidos.

3. Risco arquitetural atual

- Shared.Common mistura utilitarios neutros (Result) com componentes web/minimal API (IEndpoint, EndpointExtensions).
- Shared.Data esta acoplado a Catalogo e Pedidos (nao e um modulo neutro de dominio).

## Objetivos

1. Eliminar duplicacao de Result no Catalogo.
2. Preservar compatibilidade comportamental dos endpoints e servicos.
3. Melhorar fronteiras de compartilhamento para evitar acoplamento acidental.

## Plano de Execucao

### Fase 1 - Padronizacao de Result (baixo risco)

1. Adicionar referencia de projeto para compartilhamento de Result no Catalogo.Domain.
2. Atualizar usings do Catalogo (Domain e Application) para consumir Result compartilhado.
3. Remover arquivo duplicado de Result do Catalogo.Domain apos migracao completa.
4. Compilar a solucao e corrigir referencias residuais.

Entregavel da fase:

- Apenas um ponto de verdade para Result.

### Fase 2 - Separacao de responsabilidades em Shared (risco medio)

1. Criar projeto dedicado para contratos neutros (ex.: Shared.Kernel) contendo Result.
2. Manter contratos web em projeto separado (ex.: Shared.Web) com IEndpoint e EndpointExtensions.
3. Migrar referencias gradualmente:

- Catalogo.Domain e Catalogo.Application -> Shared.Kernel
- Pedidos.Domain/Pedidos.Common -> Shared.Kernel e, quando aplicavel, Shared.Web

4. Validar compilacao e testes em cada migracao.

Entregavel da fase:

- Shared com fronteiras claras por tipo de responsabilidade.

### Fase 3 - Revisao de Shared.Data (medio/longo prazo)

1. Confirmar decisao arquitetural: manter AppDbContext integrado entre Catalogo e Pedidos ou separar contextos.
2. Se mantido integrado:

- Documentar explicitamente que Shared.Data e uma camada de integracao entre bounded contexts.

3. Se separado:

- Planejar extracao de contextos por modulo e estrategia de migracoes.

Entregavel da fase:

- Diretriz arquitetural oficial para persistencia compartilhada.

## Validacao e Criterios de Conclusao

1. Nao existir mais de uma implementacao de Result na solucao.
2. Build da solucao sem regressao.
3. Testes de Catalogo e Pedidos passando.
4. Endpoints criticos validados (criacao, atualizacao e leitura principal).
5. Documentacao atualizada com fronteiras de compartilhamento.

## Riscos e Mitigacoes

1. Quebra de namespace/usings durante migracao.

- Mitigacao: migracao incremental por projeto e compilacao por etapa.

2. Acoplamento indevido entre dominio e componentes web.

- Mitigacao: separar Shared.Kernel de Shared.Web na Fase 2.

3. Mudanca ampla em referencias de projeto.

- Mitigacao: executar em pequenos lotes e validar testes ao final de cada lote.

## Ordem Recomendada

1. Executar Fase 1 imediatamente.
2. Executar Fase 2 na sequencia, no mesmo ciclo de refatoracao.
3. Tratar Fase 3 como decisao arquitetural planejada com ADR, se houver separacao de contexto.

## Definicao de Pronto

- Duplicacao removida.
- Compartilhamento padronizado.
- Fronteiras de responsabilidade no Shared documentadas e aplicadas.
