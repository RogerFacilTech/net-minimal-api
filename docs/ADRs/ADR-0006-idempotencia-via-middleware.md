---
status: accepted
date: 2024-03-20
deciders: [Marco Mendes]
---

# Idempotência via Middleware com Idempotency-Key

## Contexto e Problema

Em operações de escrita (POST, PUT, PATCH), clientes podem reenviar a mesma requisição por timeout, instabilidade de rede ou retry automático. Sem proteção, isso resulta em duplicação de recursos (ex: dois pedidos criados a partir do mesmo clique). Como garantir que requisições repetidas com o mesmo payload retornem o mesmo resultado sem efeitos colaterais?

## Opções Consideradas

- Idempotência na lógica de negócio de cada handler
- Idempotência via middleware centralizado com header `Idempotency-Key`
- API Gateway com deduplicação por hash de payload
- Sem idempotência explícita (aceitar duplicidade)

## Decisão

Opção escolhida: "Idempotência via middleware centralizado com header `Idempotency-Key`", porque implementar uma única vez no middleware garante proteção uniforme para todos os endpoints de escrita sem que cada handler precise reimplementar a lógica.

O middleware intercepta requisições POST, PUT e PATCH. Se o header `Idempotency-Key` estiver presente e a chave já existir no cache, retorna a resposta armazenada sem executar o handler. Apenas respostas 2xx são cacheadas. O TTL é de 24 horas.

**Limitação conhecida:** A implementação usa `IMemoryCache`, que é volátil — não sobrevive a restarts do servidor. Em um ambiente com múltiplas instâncias ou alta disponibilidade, um cliente que retenta após um restart pode processar a requisição duas vezes. Em produção, o cache deveria ser substituído por Redis ou uma tabela de idempotência persistida em banco de dados.

### Consequências

- Positivo, porque a proteção contra duplicidade é aplicada automaticamente a todos os endpoints de escrita sem mudança nos handlers.
- Positivo, porque o contrato é simples: o cliente envia um `Idempotency-Key` UUID e pode retentar com segurança.
- Negativo, porque o cache em memória não é durável — restarts do servidor invalidam todas as chaves armazenadas.
- Negativo, porque em deployment multi-instância (load balancer), cada instância tem seu próprio cache, quebrando a garantia de idempotência.

---

### Ver também

- [Overview de Idempotência](../08-IDEMPOTENCIA.md) — resumo de todos os mecanismos de idempotência do projeto (servidor, cliente HTTP e Pix MockServer)

---

_Formato baseado no template [MADR — Markdown Architectural Decision Records](https://github.com/adr/madr/blob/develop/template/adr-template-minimal.md)._
