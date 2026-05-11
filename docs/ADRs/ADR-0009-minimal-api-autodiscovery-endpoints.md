---
status: accepted
date: 2024-01-20
deciders: [Marco Mendes]
---

# Minimal API com Auto-Discovery de Endpoints via Reflexão

## Contexto e Declaração do Problema

O projeto usa ASP.NET Core Minimal APIs (sem controllers) para maior simplicidade e performance. Com a arquitetura Vertical Slice em Pedidos, cada slice define seu próprio endpoint. Como registrar esses endpoints sem um `Program.cs` que cresce linearmente a cada nova funcionalidade adicionada?

## Opções Consideradas

- Registro manual de cada endpoint no `Program.cs`
- Controllers MVC tradicionais (atributo `[Route]`, `[HttpPost]`, etc.)
- Interface `IEndpoint` com auto-discovery por reflexão na inicialização

## Decisão

Opção escolhida: "Interface `IEndpoint` com auto-discovery por reflexão", porque permite que cada slice defina e registre seu próprio endpoint de forma autossuficiente, sem que o `Program.cs` precise conhecer os slices existentes — alinhado com o princípio Open/Closed.

A interface `IEndpoint` (`src/Shared/Web/IEndpoint.cs`) define um único método `MapEndpoints(IEndpointRouteBuilder)`. Na inicialização, `AddEndpointsFromAssembly()` reflete sobre o assembly e registra no DI todas as implementações de `IEndpoint`. Em seguida, `MapRegisteredEndpoints()` chama `MapEndpoints()` em cada instância registrada.

Para adicionar um novo endpoint em Pedidos: criar uma classe que implementa `IEndpoint` — sem tocar em `Program.cs`.

### Consequências

- Positivo, porque adicionar um novo endpoint não requer modificação do `Program.cs` — a descoberta é automática.
- Positivo, porque mantém a coesão do Vertical Slice: toda a definição do slice (Command, Handler, Validator, Endpoint) fica no mesmo diretório.
- Negativo, porque o comportamento "mágico" por reflexão pode ser opaco para desenvolvedores que não conhecem o mecanismo — um endpoint pode existir sem nenhuma referência explícita no ponto de entrada da aplicação.
- Negativo, porque erros de configuração em um `IEndpoint` podem causar falhas silenciosas na inicialização se não houver logging adequado do processo de discovery.

---

_Formato baseado no template [MADR — Markdown Architectural Decision Records](https://github.com/adr/madr/blob/develop/template/adr-template-minimal.md)._
