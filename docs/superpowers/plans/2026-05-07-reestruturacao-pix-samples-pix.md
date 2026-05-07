# 2026-05-07 - Plano de acao em fases: reestruturacao Pix para samples/Pix

## Objetivo

Reestruturar os projetos Pix para uma arvore unica em `samples/Pix`, alinhando organizacao por contexto com o padrao ja adotado em Catalogo (`src/Catalogo/...`) e Pedidos (`src/Pedidos/...`).

Projetos que devem ficar dentro de `samples/Pix`:

- `Pix.MockServer`
- `Pix.ClientDemo`
- `Pix.MockServer.Tests`

## Estado atual (inventario)

- `samples/Pix.MockServer/`
- `samples/Pix.ClientDemo/`
- `samples/Pix.MockServer/Pix.MockServer.Tests/`

## Estrutura alvo

- `samples/Pix/Pix.MockServer/`
- `samples/Pix/Pix.ClientDemo/`
- `samples/Pix/Pix.MockServer.Tests/`

## Premissas

- Preservar historico de git via `git mv`.
- Nao alterar comportamento funcional dos projetos; foco em estrutura e referencias.
- Executar validacao tecnica ao fim de cada fase para reduzir risco acumulado.

---

## Fase 0 - Baseline e seguranca de mudanca

Objetivo operacional:

- Garantir ponto de partida reproduzivel antes da migracao.

Checklist:

- [ ] Registrar baseline de build:
    - `dotnet build samples/Pix.MockServer/Pix.MockServer.csproj`
    - `dotnet build samples/Pix.ClientDemo/Pix.ClientDemo.csproj`
- [ ] Registrar baseline de testes:
    - `dotnet test samples/Pix.MockServer/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj`
- [ ] Registrar estado da solution:
    - `dotnet sln FacShopAPI.slnx list`

Criterio de saida:

- Baseline documentado e sem regressao previa nao mapeada.

---

## Fase 1 - Movimentacao fisica das pastas

Objetivo operacional:

- Mover os projetos para `samples/Pix/` mantendo historico.

Passos executaveis:

1. Criar pasta agregadora:
    - `mkdir -p samples/Pix`
2. Mover MockServer:
    - `git mv samples/Pix.MockServer samples/Pix/Pix.MockServer`
3. Mover ClientDemo:
    - `git mv samples/Pix.ClientDemo samples/Pix/Pix.ClientDemo`
4. Extrair projeto de testes para ficar irmao:
    - `git mv samples/Pix/Pix.MockServer/Pix.MockServer.Tests samples/Pix/Pix.MockServer.Tests`

Observacao:

- A extracao de `Pix.MockServer.Tests` para pasta irma evita acoplamento estrutural do projeto de testes dentro do projeto principal.

Criterio de saida:

- As tres pastas existem sob `samples/Pix/` e nao restam pastas antigas na raiz de `samples/`.

---

## Fase 2 - Ajuste de referencias de projeto e solution

Objetivo operacional:

- Corrigir caminhos apos o movimento fisico.

Checklist:

- [ ] Atualizar `ProjectReference` em `samples/Pix/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj` para apontar para `../Pix.MockServer/Pix.MockServer.csproj`.
- [ ] Atualizar `FacShopAPI.slnx` removendo caminhos antigos e adicionando os novos:
    - `dotnet sln FacShopAPI.slnx remove samples/Pix.MockServer/Pix.MockServer.csproj`
    - `dotnet sln FacShopAPI.slnx remove samples/Pix.ClientDemo/Pix.ClientDemo.csproj`
    - `dotnet sln FacShopAPI.slnx remove samples/Pix.MockServer/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj`
    - `dotnet sln FacShopAPI.slnx add samples/Pix/Pix.MockServer/Pix.MockServer.csproj --solution-folder Samples`
    - `dotnet sln FacShopAPI.slnx add samples/Pix/Pix.ClientDemo/Pix.ClientDemo.csproj --solution-folder Samples`
    - `dotnet sln FacShopAPI.slnx add samples/Pix/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj --solution-folder Samples`
- [ ] Revisar scripts e tasks locais que ainda usem `samples/Pix.MockServer` ou `samples/Pix.ClientDemo`.

Criterio de saida:

- Solution lista apenas caminhos novos em `samples/Pix/...`.

---

## Fase 3 - Atualizacao de documentacao e comandos de execucao

Objetivo operacional:

- Eliminar referencias obsoletas para evitar onboarding inconsistente.

Arquivos candidatos para revisao:

- `README.md`
- `docs/00-VISAO-GERAL.md`
- `docs/01-ARQUITETURA.md`
- `docs/04-PIX.md`
- `docs/05-TESTES.md`
- `docs/ESTRUTURA.MD`

Checklist:

- [ ] Substituir caminhos antigos (`src/Pix/...` e `samples/Pix.MockServer/...`) por `samples/Pix/...`.
- [ ] Revisar comandos de `dotnet run` e `dotnet test` para os novos caminhos.
- [ ] Revisar qualquer diagrama textual de estrutura para refletir a nova arvore.

Criterio de saida:

- Nenhuma referencia funcional a caminhos antigos nos documentos de execucao principal.

---

## Fase 4 - Validacao tecnica pos-migracao

Objetivo operacional:

- Comprovar que a reestruturacao nao quebrou build/testes.

Checklist:

- [ ] Build por projeto:
    - `dotnet build samples/Pix/Pix.MockServer/Pix.MockServer.csproj`
    - `dotnet build samples/Pix/Pix.ClientDemo/Pix.ClientDemo.csproj`
- [ ] Testes:
    - `dotnet test samples/Pix/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj`
- [ ] Build da solution:
    - `dotnet build FacShopAPI.slnx`
- [ ] Listagem final da solution:
    - `dotnet sln FacShopAPI.slnx list`

Criterio de saida:

- Build verde e testes Pix verdes com caminhos novos.

---

## Fase 5 - Fechamento, governanca e rollback

Objetivo operacional:

- Encerrar a migracao com rastreabilidade e plano de contingencia.

Checklist:

- [ ] Registrar no PR as decisoes de caminho e motivacao arquitetural.
- [ ] Confirmar que CI usa os novos caminhos (pipeline/build scripts).
- [ ] Definir rollback rapido (caso necessario):
    - Reverter commit da migracao completa (unico commit recomendado para facilitar rollback).

Criterio de saida:

- Mudanca pronta para merge com baixo risco operacional.

---

## Ordem recomendada de execucao

1. Fase 0
2. Fase 1
3. Fase 2
4. Fase 3
5. Fase 4
6. Fase 5

## Riscos e mitigacoes

- Risco: quebra de `ProjectReference` por alteracao de nivel de pasta.
    - Mitigacao: validar `.csproj` logo na Fase 2 e rodar build imediatamente.
- Risco: comandos de documentacao defasados apos o move.
    - Mitigacao: Fase 3 obrigatoria antes do fechamento.
- Risco: CI continuar apontando para caminhos antigos.
    - Mitigacao: checklist de pipeline na Fase 5.
