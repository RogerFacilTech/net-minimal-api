# Plano de Acao - Alinhamento dos Projetos de Teste por Contexto

Data: 2026-05-07
Status: Proposto
Escopo atual: tests/Catalogo.Tests, tests/Pedidos.Tests, tests/Pix.MockServer.Tests
Escopo alvo: src/Catalogo/Catalogo.Tests, src/Pedidos/Pedidos.Tests, samples/Pix.MockServer/Pix.MockServer.Tests

## Objetivo

Colocar cada projeto de teste dentro do contexto do seu proprio projeto de referencia, reduzindo acoplamento cruzado e tornando manutencao, build e execucao de testes mais previsiveis.

## Mapeamento de Referencia (alvo)

- Catalogo.Tests -> contexto Catalogo
    - projeto de referencia principal: src/Catalogo/Catalogo.Host/Catalogo.Host.csproj
    - localizacao fisica alvo: src/Catalogo/Catalogo.Tests/
- Pedidos.Tests -> contexto Pedidos
    - projeto de referencia principal: src/Pedidos/Pedidos.Host/Pedidos.Host.csproj
    - localizacao fisica alvo: src/Pedidos/Pedidos.Tests/
- Pix.MockServer.Tests -> contexto Pix Mock
    - projeto de referencia principal: samples/Pix.MockServer/Pix.MockServer.csproj
    - localizacao fisica alvo: samples/Pix.MockServer/Pix.MockServer.Tests/

## Regra Estrutural Obrigatoria

Cada projeto de testes deve ficar dentro da arvore de pastas do seu contexto de referencia, ao lado dos demais projetos do contexto (e nao consolidado em tests/).

## Estado Atual (resumo)

- Catalogo.Tests referencia varios projetos internos de Catalogo (Host, Domain, Application, Endpoints, Infrastructure, Common, Shared.Kernel).
- Pedidos.Tests referencia Pedidos.Host e Pedidos.Domain.
- Pix.MockServer.Tests referencia Pix.MockServer (sample) e ja esta contextualizado.

## Resultado Esperado

1. Cada projeto de teste passa a depender primariamente do seu host/projeto de entrada do contexto.
2. Referencias adicionais a projetos internos existem apenas quando justificadas por teste unitario de dominio/aplicacao.
3. Estrutura de pastas e naming deixam explicito qual contexto cada teste cobre.
4. Pipeline de testes por contexto roda de forma independente.

## Fase 0 - Preparacao e Baseline

Checklist:

- [ ] Criar branch de trabalho dedicada.
- [ ] Rodar baseline da solucao: dotnet build FacShopAPI.slnx
- [ ] Rodar baseline de testes por contexto:
    - dotnet test tests/Catalogo.Tests/Catalogo.Tests.csproj
    - dotnet test tests/Pedidos.Tests/Pedidos.Tests.csproj
    - dotnet test tests/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj
- [ ] Registrar contagem de testes e tempo por projeto para comparativo.
- [ ] Confirmar que os caminhos atuais em tests/ serao apenas origem de migracao.

Criterio de saida:

- Baseline verde ou, se houver falhas existentes, devidamente registradas antes da refatoracao.

## Fase 1 - Definir Fronteira de Dependencias por Contexto

Objetivo operacional:

- Formalizar quais referencias de projeto sao permitidas em cada projeto de testes.

Matriz de dependencia alvo:

- Catalogo.Tests
    - obrigatoria: Catalogo.Host
    - opcionais e justificadas: Catalogo.Domain (somente testes de dominio puros), Shared.Kernel (somente se necessario para tipos compartilhados)
    - evitar: referencia direta simultanea a todas as camadas sem necessidade de cobertura
- Pedidos.Tests
    - obrigatoria: Pedidos.Host
    - opcional e justificada: Pedidos.Domain (somente testes de aggregate/value objects)
    - evitar: referencias cruzadas com Catalogo.\*
- Pix.MockServer.Tests
    - obrigatoria: Pix.MockServer
    - evitar: referencias a Catalogo._ ou Pedidos._

Checklist:

- [ ] Aprovar matriz alvo acima antes das mudancas.
- [ ] Documentar excecoes por tipo de teste (unitario x integracao).

Criterio de saida:

- Fronteira aprovada e usada como regra para as fases seguintes.

## Fase 2 - Ajustar ProjectReference dos Testes

Objetivo operacional:

- Reduzir referencias ao minimo necessario em cada .csproj de teste.

Passos:

1. Catalogo.Tests

- [ ] Revisar testes unitarios para identificar quais realmente exigem Catalogo.Domain.
- [ ] Remover referencias redundantes em Catalogo.Tests.csproj (Application, Endpoints, Infrastructure, Common) quando nao forem necessarias.
- [ ] Manter Catalogo.Host como ancora principal para integracao.

2. Pedidos.Tests

- [ ] Validar se Pedidos.Domain e de fato necessario para todos os testes.
- [ ] Se possivel, manter apenas Pedidos.Host para integracao e deixar Domain apenas quando houver testes unitarios diretos.

3. Pix.MockServer.Tests

- [ ] Confirmar que ja segue o modelo e manter sem mudancas estruturais, salvo padronizacao de metadados.

Validacao por etapa:

- [ ] dotnet build de cada projeto apos ajuste.
- [ ] dotnet test de cada projeto apos ajuste.

Criterio de saida:

- Todos os projetos de teste compilam e executam com referencias minimizadas e sem perda de cobertura.

## Fase 3 - Organizar Estrutura Fisica por Contexto (obrigatorio)

Objetivo operacional:

- Tornar explicita a relacao teste <-> contexto tambem na estrutura de pastas.

Estrutura alvo obrigatoria:

- src/Catalogo/Catalogo.Tests/
- src/Pedidos/Pedidos.Tests/
- samples/Pix.MockServer/Pix.MockServer.Tests/

Passos:

- [ ] Mover projetos de teste para a arvore do proprio contexto (git mv):
    - tests/Catalogo.Tests -> src/Catalogo/Catalogo.Tests
    - tests/Pedidos.Tests -> src/Pedidos/Pedidos.Tests
    - tests/Pix.MockServer.Tests -> samples/Pix.MockServer/Pix.MockServer.Tests
- [ ] Atualizar caminhos no FacShopAPI.slnx.
- [ ] Atualizar pipelines/scripts locais que usam paths antigos.
- [ ] Garantir que fixtures e arquivos de apoio continuem sendo copiados corretamente.

Sequencia executavel sugerida:

1. Mover pastas fisicas (preservando historico):
    - git mv tests/Catalogo.Tests src/Catalogo/Catalogo.Tests
    - git mv tests/Pedidos.Tests src/Pedidos/Pedidos.Tests
    - git mv tests/Pix.MockServer.Tests samples/Pix.MockServer/Pix.MockServer.Tests

2. Atualizar a solution removendo caminhos antigos:
    - dotnet sln FacShopAPI.slnx remove tests/Catalogo.Tests/Catalogo.Tests.csproj
    - dotnet sln FacShopAPI.slnx remove tests/Pedidos.Tests/Pedidos.Tests.csproj
    - dotnet sln FacShopAPI.slnx remove tests/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj

3. Adicionar os novos caminhos nas pastas corretas da solution:
    - dotnet sln FacShopAPI.slnx add src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj --solution-folder Catalogo
    - dotnet sln FacShopAPI.slnx add src/Pedidos/Pedidos.Tests/Pedidos.Tests.csproj --solution-folder Pedidos
    - dotnet sln FacShopAPI.slnx add samples/Pix.MockServer/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj --solution-folder Samples

4. Ajustar referencias relativas de projeto nos .csproj de teste (se necessario):
    - Catalogo.Tests.csproj: revisar ProjectReference para src/Catalogo/_ e src/Shared/_ com os novos niveis relativos.
    - Pedidos.Tests.csproj: revisar ProjectReference para src/Pedidos/\* com os novos niveis relativos.
    - Pix.MockServer.Tests.csproj: revisar ProjectReference para samples/Pix.MockServer/Pix.MockServer.csproj.

5. Validar que a solution nao mantem entradas antigas em /Tests:
    - dotnet sln FacShopAPI.slnx list
    - conferir ausencia de tests/Catalogo.Tests/Catalogo.Tests.csproj
    - conferir ausencia de tests/Pedidos.Tests/Pedidos.Tests.csproj
    - conferir ausencia de tests/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj

6. Build e testes apos migracao:
    - dotnet build FacShopAPI.slnx
    - dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj
    - dotnet test src/Pedidos/Pedidos.Tests/Pedidos.Tests.csproj
    - dotnet test samples/Pix.MockServer/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj

Criterio de saida:

- Solucao compila e testes executam com novos caminhos, sem quebrar CI.
- Nenhum projeto de teste restante na raiz tests/.

## Fase 4 - Padronizar Convencoes de Teste por Contexto

Objetivo operacional:

- Uniformizar nomenclatura, categorias e factories para evitar mistura de responsabilidades.

Checklist:

- [ ] Namespaces iniciam com Catalogo.Tests, Pedidos.Tests ou Pix.MockServer.Tests conforme contexto.
- [ ] Factories de teste ficam dentro do proprio projeto de teste do contexto.
- [ ] Helpers de autenticacao e seed ficam encapsulados por contexto.
- [ ] Sem compartilhamento indevido de fixtures entre contextos diferentes.

Criterio de saida:

- Codigo de teste reflete claramente o bounded context dono da regra validada.

## Fase 5 - Execucao em Etapas e Gate de Qualidade

Ordem recomendada:

1. Catalogo.Tests (maior acoplamento atual)
2. Pedidos.Tests
3. Pix.MockServer.Tests (validacao final)

Gate por etapa (obrigatorio):

- [ ] Build do contexto verde.
- [ ] Testes do contexto verde.
- [ ] Sem novos acoplamentos cruzados no .csproj.
- [ ] Commit pequeno e reversivel.

Commits recomendados (granularidade):

1. commit 1: move Catalogo.Tests + ajuste slnx + validacao Catalogo.
2. commit 2: move Pedidos.Tests + ajuste slnx + validacao Pedidos.
3. commit 3: move Pix.MockServer.Tests + ajuste slnx + validacao Pix.
4. commit 4: atualizacao de CI/scripts e limpeza final.

Comandos de validacao final (apos mover os projetos):

- dotnet build FacShopAPI.slnx
- dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj
- dotnet test src/Pedidos/Pedidos.Tests/Pedidos.Tests.csproj
- dotnet test samples/Pix.MockServer/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj

## Riscos e Mitigacoes

1. Risco: remocao de referencia quebrar testes unitarios especificos de dominio.
   Mitigacao: remover referencias de forma incremental, validando a cada passo.

2. Risco: mudanca de estrutura fisica quebrar CI/script local.
   Mitigacao: aplicar Fase 3 com atualizacao sincronizada de slnx, CI e scripts no mesmo commit.

3. Risco: perda de rastreabilidade entre teste e contexto.
   Mitigacao: padronizar namespace, pasta e naming por contexto na Fase 4.

## Definicao de Conclusao

Este plano e considerado concluido quando:

- Cada projeto de testes esta claramente associado ao seu contexto de referencia.
- Cada projeto de testes esta fisicamente dentro da arvore do seu contexto de referencia.
- Dependencias entre projetos de teste e camadas internas estao minimizadas e justificadas.
- Build e testes por contexto passam de forma independente e tambem no consolidado da solucao.
