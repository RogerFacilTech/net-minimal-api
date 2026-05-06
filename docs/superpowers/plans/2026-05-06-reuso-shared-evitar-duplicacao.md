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

#### Sub-plano detalhado da Fase 2 (para verificacao futura)

Objetivo operacional:

- Separar contratos neutros (Result) de contratos web (IEndpoint/EndpointExtensions) em lotes pequenos, reversiveis e com validacao a cada etapa.

Pre-condicoes de seguranca:

- Criar branch dedicada para a refatoracao.
- Validar build baseline antes de qualquer alteracao estrutural.
- Nao remover projeto da solution enquanto houver referencia ativa em qualquer .csproj.

Lote 1 - Estrutura inicial sem remocoes:

1. Criar os projetos Shared.Kernel e Shared.Web em src/Shared.
2. Adicionar ambos na solution mantendo Shared.Common ativo.
3. Mover/copiar Result para Shared.Kernel.
4. Mover/copiar IEndpoint e EndpointExtensions para Shared.Web.
5. Build da solution.

Criterio de saida do lote:

- Solution compila com Shared.Common ainda presente.

Lote 2 - Migracao Catalogo (Kernel primeiro):

1. Atualizar referencias:

- Catalogo.Domain e Catalogo.Application -> Shared.Kernel.

2. Atualizar usings necessarios para Result.
3. Validar build.
4. Executar testes de Catalogo.

Criterio de saida do lote:

- Catalogo compila e testes de Catalogo passam sem dependencia funcional de Shared.Common para Result.

Lote 3 - Migracao Pedidos (Kernel + Web quando aplicavel):

1. Atualizar referencias:

- Pedidos.Domain e Pedidos.Common -> Shared.Kernel.
- Pedidos.Endpoints e Pedidos.Host -> Shared.Web e Shared.Kernel (somente se houver uso de Result no projeto).

2. Atualizar usings de Result e IEndpoint/EndpointExtensions.
3. Validar build.
4. Executar testes de Pedidos.

Criterio de saida do lote:

- Pedidos compila e testes de Pedidos passam com dependencias segregadas.

Lote 4 - Limpeza controlada de Shared.Common:

1. Verificar que nao existe mais ProjectReference para Shared.Common em src e tests.
2. Verificar que nao existem usings de FacShopAPI.Shared.Common para tipos ja migrados.
3. Remover Shared.Common da solution apenas apos os itens 1 e 2 estarem zerados.
4. Build da solution e rodada final de testes Catalogo + Pedidos.

Criterio de saida do lote:

- Shared.Common removido sem regressao de compilacao e testes.

Lote 5 - Ajuste de namespace (opcional):

1. Renomear namespace dos contratos para refletir Kernel/Web explicitamente.
2. Atualizar usings por contexto (Catalogo/Pedidos) em lotes pequenos.
3. Build e testes por lote.

Criterio de saida do lote:

- Namespaces finalizados sem impacto comportamental.

Regras de verificacao por etapa:

1. Cada lote deve gerar commit pequeno e reversivel.
2. Nao agrupar mudancas de varios contextos no mesmo commit.
3. Em caso de erro de referencia, corrigir no mesmo lote antes de avancar.
4. Nao executar remocao fisica de pasta/projeto sem validacao previa de referencias.

Pontos de controle obrigatorios (go/no-go):

1. Go para Lote 2 somente apos build verde do Lote 1.
2. Go para Lote 3 somente apos testes de Catalogo verdes.
3. Go para Lote 4 somente apos testes de Pedidos verdes.
4. Go para Lote 5 (opcional) apenas se houver ganho claro de legibilidade/arquitetura.

#### Checklist operacional de PR (Fase 2)

Instrucoes de uso:

- Marcar cada item somente com evidencia objetiva (comando executado, build/teste verde ou diff revisado).
- Nao iniciar o lote seguinte sem concluir o checkpoint go/no-go do lote atual.

Checklist geral:

- [ ] Branch dedicada criada para a refatoracao.
- [ ] Build baseline validado antes de alteracoes estruturais.
- [x] Shared.Common mantido ate zerar referencias em src e tests.

Lote 1 - Estrutura inicial sem remocoes:

- [x] Criado projeto Shared.Kernel em src/Shared.
- [x] Criado projeto Shared.Web em src/Shared.
- [x] Projetos adicionados na solution sem remover Shared.Common.
- [x] Result movido/copiado para Shared.Kernel.
- [x] IEndpoint e EndpointExtensions movidos/copiados para Shared.Web.
- [x] Build da solution verde apos Lote 1.
- [x] Go/no-go aprovado para iniciar Lote 2.

Lote 2 - Migracao Catalogo:

- [x] Catalogo.Domain referenciando Shared.Kernel.
- [x] Catalogo.Application referenciando Shared.Kernel.
- [x] Usings de Result ajustados no Catalogo.
- [x] Build verde apos migracao do Catalogo.
- [x] Testes de Catalogo verdes.
- [x] Go/no-go aprovado para iniciar Lote 3.

Lote 3 - Migracao Pedidos:

- [x] Pedidos.Domain referenciando Shared.Kernel.
- [x] Pedidos.Common referenciando Shared.Kernel.
- [x] Pedidos.Endpoints referenciando Shared.Web (e Shared.Kernel quando aplicavel).
- [x] Pedidos.Host referenciando Shared.Web (e Shared.Kernel quando aplicavel).
- [x] Usings de Result e IEndpoint/EndpointExtensions ajustados em Pedidos.
- [x] Build verde apos migracao de Pedidos.
- [x] Testes de Pedidos verdes.
- [x] Go/no-go aprovado para iniciar Lote 4.

Lote 4 - Limpeza controlada de Shared.Common:

- [x] Nao ha ProjectReference para Shared.Common em src.
- [x] Nao ha ProjectReference para Shared.Common em tests.
- [x] Nao ha usings residuais de FacShopAPI.Shared.Common para tipos migrados.
- [x] Shared.Common removido da solution somente apos validacoes acima.
- [x] Build final verde.
- [x] Testes finais de Catalogo + Pedidos verdes.

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
