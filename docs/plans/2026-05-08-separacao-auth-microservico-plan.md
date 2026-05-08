# Plano Incremental: Separar Autenticacao em Microservico Dedicado

Data: 2026-05-08
Status: Concluido
Escopo: Catalogo + Pedidos + testes + documentacao

## Objetivo

Separar a emissao de JWT do microservico de Catalogo para um novo microservico de autenticacao (Auth), mantendo Catalogo e Pedidos apenas como resource servers (validacao de token).

## Motivacao

- Melhor aderencia ao modelo de microservicos real.
- Evita duplicacao de endpoint de login em servicos de dominio.
- Facilita evolucao futura (refresh token, usuarios, RBAC, IdP externo).
- Melhora o aprendizado arquitetural no projeto educacional.

## Nao Objetivos (nesta etapa)

- Implementar cadastro/gestao completa de usuarios.
- Implementar refresh token persistido em banco.
- Integrar com IdP externo (Keycloak/Entra/Auth0).

## Estado Atual (resumo)

- Login centralizado no Auth:
    - src/Auth/Auth.Endpoints/LoginEndpoint.cs
    - src/Auth/Auth.Host/Program.cs
- Catalogo e Pedidos ja validam JWT via AddJwtBearer.
- Catalogo nao expoe mais /api/v1/auth/login.
- Testes de Catalogo geram JWT localmente em AuthHelper.

## Estrategia de Migracao

Migracao em 5 fases, com compatibilidade temporaria para evitar quebra brusca.

---

## Fase 0 - Preparacao e baseline

### Entregas

- Usar a branch atual (já com push realizado) como baseline para a migração Auth.
- Confirmar baseline verde de build/testes.
- Congelar configuração JWT comum (Issuer/Audience/Key) em ambos hosts.

### Arquivos alvo

- src/Catalogo/Catalogo.Host/appsettings.json
- src/Pedidos/Pedidos.Host/appsettings.json
- docs/guias/COMO-CRIAR-NOVO-MICROSSERVICO.md (nota de arquitetura alvo)

### Criterios de aceite

- Build de Catalogo e Pedidos sem regressao.
- Configuracao JWT alinhada entre servicos.

### Risco

- Divergencia silenciosa de Issuer/Audience entre hosts.

---

## Fase 1 - Criar novo microservico Auth (MVP)

### Entregas

Criar estrutura minima:

- src/Auth/Auth.Host/
- src/Auth/Auth.Endpoints/ (opcional, se quiser manter padrao por projeto)

Implementar:

- POST /api/v1/auth/login
- Emissao de JWT com claims basicas (sub, email, role)
- Swagger para demonstracao
- Health endpoint

### Arquivos alvo (novos)

- src/Auth/Auth.Host/Auth.Host.csproj
- src/Auth/Auth.Host/Program.cs
- src/Auth/Auth.Host/appsettings.json
- src/Auth/Auth.Host/appsettings.Development.json
- src/Auth/Auth.Endpoints/Endpoints/Auth/LoginEndpoint.cs (se usar projeto separado)

### Criterios de aceite

- Auth.Host sobe localmente.
- Endpoint de login retorna token valido para Catalogo e Pedidos.
- Swagger do Auth acessivel.

### Risco

- Gerar token com claims/issuer incompativeis com validacao atual.

---

## Fase 2 - Integrar consumidores (Catalogo e Pedidos) sem remover legado

### Entregas

- Catalogo e Pedidos continuam validando JWT como hoje.
- Documentar que o emissor oficial agora e Auth.Host.
- Introduzir configuracao de URL do Auth para clientes/tests.

### Arquivos alvo

- src/Catalogo/Catalogo.Host/appsettings.json
- src/Pedidos/Pedidos.Host/appsettings.json
- docs/00-VISAO-GERAL.md
- docs/01-ARQUITETURA.md
- docs/guias/COMO-CRIAR-NOVO-MICROSSERVICO.md

### Criterios de aceite

- Requisicoes autenticadas com token emitido pelo Auth funcionam nos dois servicos.
- Nenhuma alteracao de contrato de endpoint de dominio.

### Risco

- Ambiguidade operacional se dois emissores (Catalogo e Auth) coexistirem sem sinalizacao.

---

## Fase 3 - Migrar testes para Auth e desativar dependencia de login no Catalogo

### Entregas

- Atualizar helper de testes para obter token do Auth.Host.
- Ajustar factories/infra de teste para endpoint de autenticacao central.
- Manter feature flag de compatibilidade temporaria no Catalogo (se necessario).

### Arquivos alvo

- src/Catalogo/Catalogo.Tests/Integration/AuthHelper.cs
- src/Catalogo/Catalogo.Tests/Integration/ApiFactory.cs
- src/Catalogo/Catalogo.Tests/Integration/RateLimitingApiFactory.cs
- (se aplicavel) novos testes de integracao em src/Auth/

### Criterios de aceite

- Suite de testes de Catalogo passa sem usar login interno do Catalogo.
- Fluxo de token em testes reproduz arquitetura alvo.

### Risco

- Testes ficarem interdependentes entre hosts sem orquestracao adequada.

---

## Fase 4 - Remocao do login do Catalogo

### Entregas

- Remover map de Auth endpoints do Catalogo Host.
- Remover endpoint de login do Catalogo.Endpoints.
- Limpar DTOs/codigo relacionado nao utilizado.

### Arquivos alvo

- src/Catalogo/Catalogo.Host/Program.cs
- src/Catalogo/Catalogo.Endpoints/Endpoints/Auth/AuthEndpoints.cs
- src/Catalogo/Catalogo.Endpoints/DTOs/ (somente se houver DTOs exclusivos do login)

### Criterios de aceite

- Catalogo nao expoe mais /api/v1/auth/login.
- Toda autenticacao da solution passa a ser emitida pelo Auth.Host.
- Catalogo e Pedidos continuam protegendo endpoints com RequireAuthorization.

### Risco

- Quebra de clientes legados que ainda chamam login no Catalogo.

### Mitigacao

- Janela de deprecacao com aviso em docs.
- Opcao de endpoint legado desativavel por config por tempo limitado.

---

## Fase 5 - Consolidacao didatica e ADR

### Entregas

- Criar ADR formalizando decisao de servico de autenticacao dedicado.
- Atualizar guias de onboarding para novo fluxo.
- Incluir diagrama simples: Auth (issuer) -> Catalogo/Pedidos (resource servers).

### Arquivos alvo

- docs/ADRs/ADR-00xx-auth-servico-dedicado.md
- docs/00-VISAO-GERAL.md
- docs/03-PEDIDOS.md
- docs/02-CATALOGO.md
- docs/guias/COMO-CRIAR-NOVO-MICROSSERVICO.md

### Criterios de aceite

- Novo desenvolvedor entende claramente: quem emite token e quem valida.
- Documentacao sem contradicoes sobre endpoint de login.

---

## Backlog Tecnico (pos-migracao)

- Rotacao de chave (kid/JWKS).
- Refresh token.
- Controle de roles/permissoes mais granular.
- Federacao com IdP externo.

## Ordem Recomendada de Implementacao

1. Fase 0
2. Fase 1
3. Fase 2
4. Fase 3
5. Fase 4
6. Fase 5

## Definition of Done (global)

- Auth.Host emite JWT.
- Catalogo e Pedidos apenas validam JWT.
- Login removido do Catalogo (ou legado explicitamente desativado por padrao).
- Testes e docs atualizados para o novo fluxo.
- Build dos hosts principais sem regressao.

## Status final de execucao

- Fase 0: concluida
- Fase 1: concluida
- Fase 2: concluida
- Fase 3: concluida
- Fase 4: concluida
- Fase 5: concluida

## Plano de Rollback

Se houver regressao critica:

1. Reabilitar temporariamente app.MapAuthEndpoints no Catalogo.
2. Reapontar AuthHelper para login do Catalogo.
3. Manter correcao em branch de migracao sem promover para main.

## Checklist de Execucao por PR (incremental)

- PR 1: Fase 0 + Fase 1 (Auth MVP)
- PR 2: Fase 2 (integracao de consumidores + docs iniciais)
- PR 3: Fase 3 (testes)
- PR 4: Fase 4 (remocao login Catalogo)
- PR 5: Fase 5 (ADR + consolidacao final)
