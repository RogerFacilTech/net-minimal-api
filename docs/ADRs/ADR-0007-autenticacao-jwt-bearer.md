---
status: accepted
date: 2024-03-10
deciders: [Marco Mendes]
---

# Autenticação JWT Bearer com Credenciais em Configuração

## Contexto e Declaração do Problema

A API precisa de autenticação para proteger endpoints de escrita (criação, atualização, remoção de produtos e criação de pedidos). O projeto é educacional e deve demonstrar o padrão de autenticação JWT sem depender de infraestrutura externa (servidor OAuth2, Azure AD, etc.). Como implementar autenticação funcional e demonstrável com a menor dependência externa possível?

## Opções Consideradas

- OAuth2 com provedor externo (Azure AD, Auth0, Keycloak)
- IdentityServer / Duende IdentityServer embutido
- JWT Bearer com usuário hardcoded no código-fonte
- JWT Bearer com credenciais em arquivo de configuração

## Decisão

Opção escolhida: "JWT Bearer com credenciais em arquivo de configuração", porque mantém a implementação autocontida para fins educacionais enquanto segue o padrão correto de externalizar credenciais do código-fonte.

O endpoint `POST /api/v1/auth/login` valida email e senha lidos de `appsettings.json` (`Auth:AdminEmail`, `Auth:AdminPassword`) e retorna um JWT assinado com HS256, expiração de 2 horas, contendo claims de `sub`, `email` e `role`.

**Simplificações intencionais para contexto educacional:**

- Usuário único (admin), sem user store ou banco de usuários
- Senha em texto claro na configuração (em produção: hash BCrypt/Argon2 no banco)
- Chave de assinatura JWT em `appsettings.json` (em produção: secrets manager ou variável de ambiente)
- Credenciais não devem estar hardcoded no código-fonte — devem vir sempre de configuração ou secrets

### Consequências

- Positivo, porque a API é autocontida — não requer infraestrutura externa para demonstrar autenticação JWT.
- Positivo, porque as credenciais ficam em configuração, não no código-fonte, seguindo o princípio de separação de configuração e código.
- Negativo, porque a chave JWT em `appsettings.json` é comprometida se o repositório for público — em produção, usar `dotnet user-secrets`, variáveis de ambiente ou Azure Key Vault.
- Negativo, porque não há gestão de usuários, refresh tokens ou revogação — funcionalidades necessárias em produção que estão fora do escopo educacional.

## Ver também

- [ADR-0016 — Auth como Serviço Dedicado](ADR-0016-auth-servico-dedicado.md): a emissão de JWT descrita aqui foi posteriormente extraída para um microsserviço dedicado (`Auth.Host`), desacoplando a responsabilidade de autenticação das APIs de domínio.

---

_Formato baseado no template [MADR — Markdown Architectural Decision Records](https://github.com/adr/madr/blob/develop/template/adr-template-minimal.md)._
