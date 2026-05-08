# Plano de Ação — Correção de Autenticação dos Endpoints

## Contexto

Foi identificado que os endpoints de escrita do contexto **Pedidos** (POST, PUT, PATCH, DELETE) estão públicos (`AllowAnonymous()`), contrariando as melhores práticas e as convenções do projeto. O contexto **Catálogo** já segue corretamente: leitura anônima, escrita protegida.

---

## Objetivo

Garantir que todos os endpoints de escrita de Pedidos exijam autenticação (`RequireAuthorization()`), alinhando com o padrão de segurança adotado no projeto.

---

## Passos do Plano de Ação

1. **Mapear todos os endpoints de Pedidos**
    - Revisar todos os arquivos em `src/Pedidos/Pedidos.Endpoints/`.
    - Identificar endpoints de escrita (POST, PUT, PATCH, DELETE) e leitura (GET).

2. **Corrigir endpoints de escrita**
    - Substituir `.AllowAnonymous()` por `.RequireAuthorization()` em todos os endpoints de escrita.
    - Manter `.AllowAnonymous()` apenas em endpoints de leitura, se desejado pelo negócio.

3. **Revisar endpoints de leitura**
    - Avaliar se endpoints GET de Pedidos devem ser públicos ou privados (por padrão, pedidos são privados).
    - Se necessário, aplicar `.RequireAuthorization()` também nos GET.

4. **Atualizar documentação**
    - Atualizar README e docs para refletir a nova política de autenticação dos endpoints de Pedidos.
    - Garantir que exemplos e tabelas estejam consistentes.

5. **Testar endpoints**
    - Rodar testes automatizados para garantir que endpoints protegidos retornam 401/403 sem token.
    - Adicionar/ajustar testes de autenticação se necessário.

6. **Comunicar mudança**
    - Registrar a alteração em changelog/ADR, se aplicável.
    - Comunicar a equipe sobre a mudança de política de autenticação.

---

## Observações

- O contexto Catálogo já está conforme o padrão.
- O endpoint de login (Auth) deve permanecer anônimo.
- Caso surjam novos endpoints, seguir sempre o padrão: escrita protegida, leitura anônima (exceto recursos sensíveis).

---

_Data: 08/05/2026_
_Autor: GitHub Copilot_
