# Plano de Acao - Correcao do README.md

Data: 2026-05-08
Status: Proposto
Arquivo alvo: README.md (raiz do repositorio)

## Contexto

Analise do README revelou 5 areas fora de conformidade apos as mudancas arquiteturais recentes:
separacao do Auth como microservico dedicado (ADR-0016), reestruturacao dos sub-projetos do
Catalogo/Pedidos, e movimentacao de samples para a pasta samples/.

---

## Discrepancias Encontradas

### CRITICO

**1. Secao "Inicio Rapido"**

- Atual: `dotnet run` (comando unico, implica host unico)
- Correto: 3 hosts separados — Auth.Host, Catalogo.Host, Pedidos.Host
- Acao: substituir por bloco com 3 comandos `dotnet run --project <host>` e Swagger URLs corretas por servico
- Pendente: verificar portas nos launchSettings.json de cada Host antes de executar

Portas ja verificadas:

- Catalogo.Host: http://localhost:5000 (Swagger na raiz)
- Pedidos.Host: http://localhost:5001
- Auth.Host: verificar launchSettings (nao encontrado na busca inicial)

**2. Tabela "Bounded Contexts"**

- Atual: 3 linhas (Catalogo, Pedidos, Pix)
- Correto: 4 linhas — adicionar Auth
- Acao: incluir linha `Auth | JWT Emitter | /api/v1/auth/* | Emite e assina JWT; demais servicos apenas validam`

**3. Secao "Estrutura de Diretorios"**
Erros factuais multiplos — toda a arvore precisa ser reescrita:

| O que README diz                  | O que existe de fato                                                                                     |
| --------------------------------- | -------------------------------------------------------------------------------------------------------- |
| src/Catalogo/Catalogo.API/        | src/Catalogo/Catalogo.Endpoints/ + Catalogo.Host/ + Catalogo.Data/ + Catalogo.Common/                    |
| src/Catalogo/Catalogo.ClientDemo/ | samples/Catalogo.HttpClientDemo/                                                                         |
| src/Pedidos/CreatePedido/...      | src/Pedidos/Pedidos.Endpoints/CreatePedido/... + Domain, Application, Infrastructure, Host, Data, Common |
| src/Pix/                          | samples/Pix/                                                                                             |
| src/Shared/Common/ + Middleware/  | src/Shared/Data/, Http/, Kernel/, Web/                                                                   |
| (ausente)                         | src/Auth/Auth.Endpoints/, src/Auth/Auth.Host/                                                            |

Arvore correta a colocar no README:

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

### MENOR

**4. Contagem de ADRs na secao "Documentacao"**

- Atual: "15 ADRs no formato MADR 3.x"
- Correto: "16 ADRs" (ADR-0016-auth-servico-dedicado.md foi criado)
- Acao: alterar a string na ultima linha da tabela de Documentacao

**5. Contagem de testes**

- Atual: Catalogo.Tests=102, Pedidos.Tests=47, Pix.MockServer.Tests=7, Total=156
- Acao: rodar `dotnet test FacShopAPI.slnx` e verificar contagens antes de atualizar

---

## Arquivos a Modificar

- README.md (raiz do repositorio) — unico arquivo

## Passos de Execucao

1. Verificar launchSettings.json do Auth.Host para confirmar porta
2. Rodar `dotnet test` para conferir contagens atuais de testes
3. Editar secao "Inicio Rapido" — substituir `dotnet run` por 3 comandos separados com portas corretas
4. Editar tabela "Bounded Contexts" — adicionar linha do Auth
5. Reescrever secao "Estrutura de Diretorios" com arvore correta (ver arvore acima)
6. Alterar "15 ADRs" para "16 ADRs" na tabela de Documentacao
7. Atualizar contagens de testes se necessario

## Verificacao pos-execucao

- Confirmar que todos os diretorios da arvore existem fisicamente no workspace
- Conferir que nenhuma secao de endpoints foi alterada (estao corretas)
- Revisar leitura do README do inicio ao fim

## Escopo Deliberadamente Excluido

- Secao de Endpoints (Catalogo, Pedidos, Auth) — esta correta, nao alterar
- Secao de Documentacao (links dos docs/) — links estao corretos, apenas corrigir contagem de ADRs
- Nenhuma secao nova sera adicionada
