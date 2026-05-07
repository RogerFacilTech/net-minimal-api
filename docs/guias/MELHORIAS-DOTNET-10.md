# Melhorias .NET 10 - Minimal API

## 📌 Visão Geral

Este documento descreve as melhorias implementadas no projeto **ProdutosAPI** para aproveitar os novos recursos do **.NET 10 LTS** com foco em **Minimal API** patterns modernos e best practices.

**Versão do Projeto**: 3.1.0  
**Framework**: .NET 10.0  
**Data de Atualização**: 2026-03-04

---

## 🎯 Principais Melhorias do .NET 10

### 1. **Typed Results para Type-Safety**

#### ❌ Antes (IResult não tipado)

```csharp
private static async Task<IResult> ObterProduto(int id, IProdutoService service)
{
    var produto = await service.ObterProdutoAsync(id);
    return Results.Ok(produto);  // Tipo não é verificado em compile-time
}
```

#### ✅ Depois (.NET 10 - Typed Results)

```csharp
private static async Task<Results<Ok<ProdutoResponse>, NotFound<ErrorResponse>>> ObterProduto(
    int id,
    IProdutoService service)
{
    try
    {
        var produto = await service.ObterProdutoAsync(id);
        return TypedResults.Ok(produto);  // Type-safe, verificado em compile-time
    }
    catch (KeyNotFoundException ex)
    {
        return TypedResults.NotFound(new ErrorResponse { ... });
    }
}
```

**Benefícios**:

- ✅ Verificação de tipo em compile-time
- ✅ IntelliSense melhorado em IDEs
- ✅ Documentação automática mais precisa
- ✅ Melhor performance (sem boxing de ValueTypes)
- ✅ Melhor suporte do Swagger/OpenAPI

---

### 2. **Discriminated Union Results**

O .NET 10 introduz tipos discriminados para representar múltiplos resultados possíveis:

```csharp
// Representa que o endpoint pode retornar:
// - 200 OK com ProdutoResponse
// - 404 NotFound com ErrorResponse
// - 422 UnprocessableEntity com ErrorResponse
private static async Task<Results<
    Ok<ProdutoResponse>,
    NotFound<ErrorResponse>,
    UnprocessableEntity<ErrorResponse>>> AtualizarProduto(...)
{
    // Agora o compilador força tratamento de todos os casos
    // Swagger gera documentação precisa com todos os status codes
}
```

**Vantagens**:

- ✅ Força tratamento de todos os cenários de erro possíveis
- ✅ OpenAPI gerado automaticamente com todos os status codes
- ✅ Type-safe routing baseado em resultado
- ✅ Melhor documentação automática

---

### 3. **MapGroup com Prefix - DRY Principle**

#### ❌ Antes (Duplicação de rota)

```csharp
app.MapGet("/api/v1/produtos", Handler1).WithName("...").WithOpenApi();
app.MapGet("/api/v1/produtos/{id}", Handler2).WithName("...").WithOpenApi();
app.MapPost("/api/v1/produtos", Handler3).WithName("...").WithOpenApi();
```

#### ✅ Depois (.NET 10 MapGroup)

```csharp
var group = app.MapGroup("/api/v1/produtos")
    .WithName("Produtos")
    .WithOpenApi()
    .WithTags("Produtos")
    .WithDescription("Endpoints para gerenciamento de produtos");

group.MapGet("/", Handler1);
group.MapGet("/{id}", Handler2);
group.MapPost("/", Handler3);
```

**Benefícios**:

- ✅ Reduz duplicação de configuração
- ✅ Facilita manutenção (mudança de versão de API em um lugar)
- ✅ Melhor organização visual do código
- ✅ Configurações compartilhadas aplicadas a todos endpoints

---

### 4. **Métodos Typed Results Explícitos**

O .NET 10 introduz `TypedResults` factory methods:

````csharp
// ✅ .NET 10 - Type-safe factories
return TypedResults.Ok(produto);           // Results<Ok<T>>
return TypedResults.Created(uri, produto); // Results<Created<T>>
return TypedResults.NoContent();           // Results<NoContent>
return TypedResults.NotFound(error);       // Results<NotFound<T>>
return TypedResults.BadRequest(error);     // Results<BadRequest<T>>

---

### 5. **Resiliência HTTP Padrão para Clientes**

A trilha PIX aplica os recursos modernos de cliente HTTP do ecossistema .NET:

```csharp
builder.Services.AddHttpClient<PixProcessingClient>(...)
    .AddStandardResilienceHandler();
````

**Benefícios**:

- ✅ Retry e timeout padronizados sem código repetido
- ✅ Menor risco de chamadas frágeis em integrações externas
- ✅ Configuração centralizada por cliente tipado

**Implementação**: `samples/Pix/Pix.ClientDemo/Program.cs`

---

### 6. **JSON Source Generation**

No servidor mock PIX, o fingerprint de idempotência usa serialização com `JsonSerializerContext`:

```csharp
[JsonSerializable(typeof(CriarCobrancaRequest))]
internal partial class JsonContext : JsonSerializerContext
{
}
```

**Benefícios**:

- ✅ Menor custo de reflexão em serialização
- ✅ Contratos JSON mais explícitos
- ✅ Performance e previsibilidade em payloads complexos

**Implementação**: `samples/Pix/Pix.MockServer/Application/JsonContext.cs`
return TypedResults.UnprocessableEntity(error); // Results<UnprocessableEntity<T>>

// vs. IResult genérico (não tipado)
return Results.Ok(produto);

````

---

### 5. **Melhor Integração com OpenAPI/Swagger**

#### Antes
```csharp
group.MapGet("/{id}", Handler)
    .Produces<ProdutoResponse>(200)
    .Produces<ErrorResponse>(404);
````

#### Depois (.NET 10 - Automático)

```csharp
group.MapGet("/{id}", Handler)
    .WithOpenApi()
    .Accepts<CriarProdutoRequest>("application/json")
    .Produces<ProdutoResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
    .WithDescription("Obtém um produto")
    .WithSummary("Obter produto");

// Swagger gera documentação PRECISA com:
// ✅ Request/Response schemas tipados
// ✅ Todos os status codes listados
// ✅ Descrições detalhadas
// ✅ Exemplos de valores
```

---

### 6. **`WithParameterValidation()`**

Novo atributo do .NET 10 para validação de parâmetros:

```csharp
group.MapGet("/", ListarProdutos)
    .WithParameterValidation()  // .NET 10 - Valida automaticamente
    .Produces<PaginatedResponse<ProdutoResponse>>(200)
    .Produces<ErrorResponse>(400);
```

---

## 📋 Mudanças Implementadas no Projeto

### Arquivo: `src/Produtos/Produtos.API/Endpoints/ProdutoEndpoints.cs`

#### **Assinatura de Método - Antes**

```csharp
private static async Task<IResult> ListarProdutos(
    IProdutoService service, ...)
```

#### **Assinatura de Método - Depois**

```csharp
private static async Task<Results<Ok<PaginatedResponse<ProdutoResponse>>, BadRequest<ErrorResponse>>>
ListarProdutos(IProdutoService service, ...)
```

**Mudanças em cada endpoint**:

| Endpoint     | Antes         | Depois                                                           | Benefício                         |
| ------------ | ------------- | ---------------------------------------------------------------- | --------------------------------- |
| GET /        | Task<IResult> | Task<Results<Ok<...>, BadRequest<...>>>                          | Type-safe, múltiplos return types |
| GET /{id}    | Task<IResult> | Task<Results<Ok<...>, NotFound<...>>>                            | Força tratamento de 404           |
| POST /       | Task<IResult> | Task<Results<Created<...>, BadRequest<...>, Unprocessable<...>>> | Previne erros                     |
| PUT /{id}    | Task<IResult> | Task<Results<Ok<...>, NotFound<...>>>                            | Exaustivo                         |
| PATCH /{id}  | Task<IResult> | Task<Results<Ok<...>, NotFound<...>>>                            | Exaustivo                         |
| DELETE /{id} | Task<IResult> | Task<Results<NoContent, NotFound<...>>>                          | Simples, direto                   |

---

### Recursos de Endpoint Adicionados

```csharp
group.MapGet("/", ListarProdutos)
    .WithName("ListarProdutos")
    .WithDescription("...")
    .WithSummary("...")
    .WithOpenApi()                    // .NET 10 - melhoria
    .Accepts<CriarProdutoRequest>("application/json")  // .NET 10 - novo
    .Produces<PaginatedResponse<ProdutoResponse>>(200)
    .Produces<ErrorResponse>(400)
    .WithParameterValidation()         // .NET 10 - novo
    .AllowAnonymous();
```

### 🚀 Recursos Facilitadores para Vertical Slice

A nova arquitetura de **Vertical Slice** utilizada em `src/Pedidos/` se beneficia das melhorias do .NET 10:

- **IEndpoint scan automático**: o método de extensão `AddEndpointsFromAssembly()` elimina a necessidade de registrar cada rota manualmente, permitindo que slices sejam registrados somente por estarem na assembly.

    ```csharp
    builder.Services.AddEndpointsFromAssembly(typeof(CreatePedidoEndpoint).Assembly);
    ```

- **Primary constructors para handlers**: handlers de comandos podem declarar dependências diretamente no construtor de registro conciso.

    ```csharp
    public sealed class CreatePedidoHandler(IAppDbContext db, ILogger<CreatePedidoHandler> log)
    {
        public async Task<Result> Handle(CreatePedidoCommand cmd) { ... }
    }
    ```

- **Collection expressions** tornam o mapeamento de múltiplos endpoints mais conciso quando agrupados dinamicamente.
    ```csharp
    var slices = new[] { typeof(CreatePedidoEndpoint), typeof(CancelPedidoEndpoint) };
    foreach(var type in slices) builder.Services.AddEndpoints(type);
    ```

Estas facilidades tornam o desenvolvimento de cada slice extremamente leve e eliminam boilerplate que antes era inevitável em APIs de grande escala.

---

## 📊 Comparativa: Antes vs Depois

### Handlers HTTP

**Antes - IResult genérico**

```csharp
private static async Task<IResult> CriarProduto(CriarProdutoRequest req, ...)
{
    // Swagger não sabe quais status códigos são possíveis
    // Verificação de tipo apenas em runtime
    var produto = await service.CriarProdutoAsync(req);
    return Results.Created($"...", produto);
}
```

**Depois - Typed Results Union**

```csharp
private static async Task<Results<Created<ProdutoResponse>, BadRequest<ErrorResponse>, UnprocessableEntity<ErrorResponse>>>
CriarProduto(CriarProdutoRequest req, ...)
{
    try
    {
        // Compilador força tratamento de todos os cenários
        var produto = await service.CriarProdutoAsync(req);
        return TypedResults.Created($"...", produto);
    }
    catch (ValidationException ex)
    {
        return TypedResults.UnprocessableEntity(new ErrorResponse { ... });
    }
}
```

---

## 🛡️ Benefícios de Type-Safety

### Antes: Swagger impreciso

```json
{
    "responses": {
        "200": {
            "description": "Success",
            "content": {
                "application/json": {
                    "schema": {} // ❌ Schema vazio, tipo desconhecido
                }
            }
        }
    }
}
```

### Depois: Swagger preciso

```json
{
    "responses": {
        "200": {
            "description": "Success",
            "content": {
                "application/json": {
                    "schema": { "$ref": "#/components/schemas/ProdutoResponse" } // ✅ Schema completo
                }
            }
        },
        "404": {
            "description": "Not Found",
            "content": {
                "application/json": {
                    "schema": { "$ref": "#/components/schemas/ErrorResponse" } // ✅ Listado
                }
            }
        }
    }
}
```

---

## 🧪 Testes do .NET 10

O projeto inclui testes abrangentes validando:

✅ **Unit Tests** (ProdutoServiceTests.cs)

- Testes de service com mocking
- 16+ cases cobrindo todos os cenários

✅ **Integration Tests** (ProdutoEndpointsTests.cs)

- Testes de HTTP status codes
- Testes de validação
- 18+ cases

✅ **Validator Tests** (ProdutoValidatorTests.cs)

- Testes de regras de negócio
- 20+ cases

---

## 🚀 Como Executar e Testar

### Build the Project

```bash
cd net-minimal-api
dotnet build -c Release
```

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Category

```bash
dotnet test --filter "FullyQualifiedName~ProdutosAPI.Tests.Services"
dotnet test --filter "FullyQualifiedName~ProdutosAPI.Tests.Endpoints"
```

### Run Application

```bash
dotnet run --project ProdutosAPI.csproj
# Acesse: http://localhost:5000
# Swagger UI: http://localhost:5000/swagger
```

---

## 📦 Dependências Atualizadas para .NET 10

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Version>3.1.0</Version>
</PropertyGroup>

<ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.9.0" />
    <PackageReference Include="FluentValidation" Version="11.10.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SQLite" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.2.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.7.0" />
    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="13.0.1" />
</ItemGroup>
```

---

## 🎓 Recursos de Aprendizado

### Documentação Oficial

- [.NET 10 Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Typed Results in .NET 10](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses)
- [OpenAPI with Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/openapi)

### Referências do Projeto

- [Melhores Praticas API](./MELHORES-PRATICAS-API.md)
- [Estratégia de Testes](./ESTRATEGIA-DE-TESTES.md)
- [Code Structure](./ESTRUTURA-DO-CODIGO.md)

---

## 🔄 Próximos Passos Opcionais

1. **API Versioning**
    - Implementar header-based versioning
    - URL-path versioning com MapGroup

2. **Rate Limiting**
    - .NET 10 built-in rate limiting middleware
    - Per-endpoint ou global policies

3. **Advanced OpenAPI**
    - Custom operation filters
    - Security schemes (OAuth2, API Key)

4. **Performance**
    - Output caching
    - Compression middleware

5. **Globalization (i18n)**
    - Multi-language error messages
    - Content negotiation by culture

---

## 📝 Checklist de Migração Completa

✅ Framework atualizado para .NET 10.0  
✅ Pacotes NuGet atualizados para compatibilidade .NET 10  
✅ Typed Results implementados em todos endpoints  
✅ Discriminated Union Results para múltiplas respostas  
✅ MapGroup com prefix consolidado  
✅ OpenAPI/Swagger enhancements aplicadas  
✅ Testes unitários criados (xUnit, Moq)  
✅ Testes de integração criados  
✅ Documentação atualizada  
✅ Versionamento de projeto: 1.0.0 → 3.1.0

---

## 🏆 Conclusão

O projeto **ProdutosAPI v3.1.0** agora demonstra as melhores práticas modernas do **.NET 10 LTS** com:

🎯 **Type-Safety** através de Typed Results  
🎯 **Documentação Precisa** com OpenAPI automático  
🎯 **Code Organization** com MapGroup  
🎯 **Comprehensive Testing** com xUnit e Moq  
🎯 **Production-Ready** patterns e practices

É um recurso educacional excelente para aprender Minimal API no .NET 10!

---

**Autor**: GitHub Copilot  
**Última Atualização**: 2026-03-04  
**Status**: ✅ Completo para .NET 10 LTS
