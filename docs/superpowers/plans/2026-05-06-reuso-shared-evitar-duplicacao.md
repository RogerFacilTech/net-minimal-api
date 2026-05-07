# Plano de Acao - Reuso de Shared para Evitar Duplicacao

Data: 2026-05-06
Status: Concluido
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

### Fase 3 - Separacao de contextos mantendo banco compartilhado (medio prazo)

Decisao arquitetural desta fase:

- Manter o mesmo banco fisico por enquanto.
- Separar ownership de contexto e ownership de migrations por modulo (Catalogo e Pedidos).
- Remover o acoplamento de Shared.Data com entidades e projetos especificos de dominio.

Objetivo da fase:

- Cada modulo aplica e evolui suas proprias migrations, mesmo com string de conexao compartilhada.

Diretrizes tecnicas:

1. Um DbContext por modulo:

- CatalogoDbContext para entidades de Catalogo.
- PedidosDbContext para entidades de Pedidos.

2. Migrations separadas por modulo:

- Assembly/pasta de migrations de Catalogo isolada.
- Assembly/pasta de migrations de Pedidos isolada.

3. Historico de migration isolado por contexto:

- Usar tabela de historico distinta por contexto para evitar colisao de \_\_EFMigrationsHistory.

4. Banco fisico compartilhado, ownership logico separado:

- Mesmo Data Source, mas fronteiras de mapeamento e evolucao controladas por contexto.

#### Sub-plano detalhado da Fase 3

Pre-condicoes:

- Build baseline verde antes da extracao.
- Testes de Catalogo e Pedidos verdes.
- Branch dedicada para a fase.

Lote 1 - Estrutura de contextos separados (sem remover AppDbContext ainda):

1. Criar projeto/area de infraestrutura de dados de Catalogo (ex.: Catalogo.Data).
2. Criar projeto/area de infraestrutura de dados de Pedidos (ex.: Pedidos.Data).
3. Introduzir CatalogoDbContext com apenas DbSets e mapeamentos de Catalogo.
4. Introduzir PedidosDbContext com apenas DbSets e mapeamentos de Pedidos.
5. Manter AppDbContext temporariamente para compatibilidade.

Criterio de saida:

- Solution compila com os 3 contextos coexistindo temporariamente.

Lote 2 - Migracao de injecao e repositorios de Catalogo:

1. Migrar DI de Catalogo.Host para usar CatalogoDbContext.
2. Ajustar repositorios/servicos de Catalogo para depender de CatalogoDbContext/abstracao equivalente.
3. Garantir que Pedidos nao seja carregado no pipeline de Catalogo.
4. Build + testes de Catalogo.

Criterio de saida:

- Catalogo funcional sem dependencia de AppDbContext.

Lote 3 - Migracao de injecao e repositorios de Pedidos:

1. Migrar DI de Pedidos.Host para usar PedidosDbContext.
2. Ajustar repositorios/handlers de Pedidos para depender de PedidosDbContext.
3. Garantir que Catalogo nao seja carregado no pipeline de Pedidos.
4. Build + testes de Pedidos.

Criterio de saida:

- Pedidos funcional sem dependencia de AppDbContext.

Lote 4 - Separacao de migrations por modulo:

1. Criar configuracao de migrations para CatalogoDbContext (assembly/pasta propria).
2. Criar configuracao de migrations para PedidosDbContext (assembly/pasta propria).
3. Configurar tabela de historico de migration distinta por contexto.
4. Definir estrategia de bootstrap:

- Catalogo.Host aplica apenas migrations de Catalogo.
- Pedidos.Host aplica apenas migrations de Pedidos (ou pipeline externo por servico).

5. Rodar validacao em banco novo e banco ja existente.

Criterio de saida:

- Cada host evolui apenas seu proprio schema logico.

Lote 5 - Descomissionamento de Shared.Data acoplado:

1. Remover referencias de Shared.Data para projetos de dominio/aplicacao especificos.
2. Remover AppDbContext quando nao houver mais consumidores.
3. Manter em Shared apenas utilitarios neutros (se houver necessidade real).
4. Build final + testes Catalogo e Pedidos.

Criterio de saida:

- Nao existe mais contexto unificado acoplado entre Catalogo e Pedidos.

#### Estrategia de migrations (detalhe operacional)

1. Nao recriar historico do zero em ambiente existente.
2. Criar migration baseline por contexto refletindo estado atual.
3. Validar script idempotente por contexto antes de aplicar em ambiente compartilhado.
4. Definir ordem de rollout:

- Primeiro scripts de Catalogo.
- Depois scripts de Pedidos.

5. Em caso de conflito de objeto no banco compartilhado, bloquear rollout e corrigir naming/mapeamento antes de seguir.

#### Riscos especificos e mitigacoes

1. Conflito de nomes de tabelas/indices entre contextos.

- Mitigacao: padronizar naming por contexto e revisar scripts antes de aplicar.

2. Divergencia entre snapshot atual e baseline novo.

- Mitigacao: homologar em copia de banco real e revisar diff SQL gerado.

3. Um host aplicar migration de outro contexto por engano.

- Mitigacao: separar explicitamente startup de migration por host e por contexto.

4. Quebra de testes de integracao por mudanca de contexto.

- Mitigacao: migracao em lotes com build/teste ao final de cada lote.

#### Checklist operacional da Fase 3

Checklist geral:

- [x] Branch dedicada da Fase 3 criada.
- [x] Build baseline verde registrado.
- [x] Testes baseline de Catalogo e Pedidos verdes registrados.

Lote 1:

- [x] CatalogoDbContext criado.
- [x] PedidosDbContext criado.
- [x] AppDbContext mantido apenas para compatibilidade temporaria.
- [x] Build verde.

Lote 2:

- [x] DI de Catalogo migrada para CatalogoDbContext.
- [x] Repositorios de Catalogo migrados.
- [x] Build verde.
- [x] Testes Catalogo verdes.

Lote 3:

- [x] DI de Pedidos migrada para PedidosDbContext.
- [x] Repositorios/handlers de Pedidos migrados.
- [x] Build verde.
- [x] Testes Pedidos verdes.

Lote 4:

- [x] Migrations de Catalogo separadas.
- [x] Migrations de Pedidos separadas.
- [x] Tabela de historico de migration separada por contexto.
- [x] Validacao em banco novo concluida.
- [x] Validacao em banco existente concluida.

Lote 5:

- [x] Shared.Data desacoplado de projetos especificos.
- [x] AppDbContext removido (quando sem consumidores).
- [x] Build final verde.
- [x] Testes finais Catalogo e Pedidos verdes.

Entregavel da fase:

- Contextos segregados por modulo, com banco compartilhado e ownership de migrations independente por servico.

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
