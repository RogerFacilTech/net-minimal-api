# Melhorias .NET 10 - Minimal API

## ðŸ“Œ VisÃ£o Geral

Este documento descreve as melhorias implementadas no projeto **FacShopAPI** para aproveitar os novos recursos do **.NET 10 LTS** com foco em **Minimal API** patterns modernos e best practices.

**VersÃ£o do Projeto**: 3.1.0  
**Framework**: .NET 10.0  
**Data de AtualizaÃ§Ã£o**: 2026-03-04

---

## ðŸŽ¯ Principais Melhorias do .NET 10

### 1. **Typed Results para Type-Safety**

#### âŒ Antes (IResult nÃ£o tipado)

```csharp
private static async Task<IResult> ObterProduto(int id, IProdutoService service)
{
    var produto = await service.ObterProdutoAsync(id);
    return Results.Ok(produto);  // Tipo nÃ£o Ã© verificado em compile-time
}
```

#### âœ… Depois (.NET 10 - Typed Results)

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

**BenefÃ­cios**:

- âœ… VerificaÃ§Ã£o de tipo em compile-time
- âœ… IntelliSense melhorado em IDEs
- âœ… DocumentaÃ§Ã£o automÃ¡tica mais precisa
- âœ… Melhor performance (sem boxing de ValueTypes)
- âœ… Melhor suporte do Swagger/OpenAPI

---

### 2. **Discriminated Union Results**

O .NET 10 introduz tipos discriminados para representar mÃºltiplos resultados possÃ­veis:

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
    // Agora o compilador forÃ§a tratamento de todos os casos
    // Swagger gera documentaÃ§Ã£o precisa com todos os status codes
}
```

**Vantagens**:

- âœ… ForÃ§a tratamento de todos os cenÃ¡rios de erro possÃ­veis
- âœ… OpenAPI gerado automaticamente com todos os status codes
- âœ… Type-safe routing baseado em resultado
- âœ… Melhor documentaÃ§Ã£o automÃ¡tica

---

### 3. **MapGroup com Prefix - DRY Principle**

#### âŒ Antes (DuplicaÃ§Ã£o de rota)

```csharp
app.MapGet("/api/v1/produtos", Handler1).WithName("...").WithOpenApi();
app.MapGet("/api/v1/produtos/{id}", Handler2).WithName("...").WithOpenApi();
app.MapPost("/api/v1/produtos", Handler3).WithName("...").WithOpenApi();
```

#### âœ… Depois (.NET 10 MapGroup)

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

**BenefÃ­cios**:

- âœ… Reduz duplicaÃ§Ã£o de configuraÃ§Ã£o
- âœ… Facilita manutenÃ§Ã£o (mudanÃ§a de versÃ£o de API em um lugar)
- âœ… Melhor organizaÃ§Ã£o visual do cÃ³digo
- âœ… ConfiguraÃ§Ãµes compartilhadas aplicadas a todos endpoints

---

### 4. **MÃ©todos Typed Results ExplÃ­citos**

O .NET 10 introduz `TypedResults` factory methods:

````csharp
// âœ… .NET 10 - Type-safe factories
return TypedResults.Ok(produto);           // Results<Ok<T>>
return TypedResults.Created(uri, produto); // Results<Created<T>>
return TypedResults.NoContent();           // Results<NoContent>
return TypedResults.NotFound(error);       // Results<NotFound<T>>
return TypedResults.BadRequest(error);     // Results<BadRequest<T>>

---

### 5. **ResiliÃªncia HTTP PadrÃ£o para Clientes**

A trilha PIX aplica os recursos modernos de cliente HTTP do ecossistema .NET:

```csharp
builder.Services.AddHttpClient<PixProcessingClient>(...)
    .AddStandardResilienceHandler();
````

**BenefÃ­cios**:

- âœ… Retry e timeout padronizados sem cÃ³digo repetido
- âœ… Menor risco de chamadas frÃ¡geis em integraÃ§Ãµes externas
- âœ… ConfiguraÃ§Ã£o centralizada por cliente tipado

**ImplementaÃ§Ã£o**: `samples/Pix/Pix.ClientDemo/Program.cs`

---

### 6. **JSON Source Generation**

No servidor mock PIX, o fingerprint de idempotÃªncia usa serializaÃ§Ã£o com `JsonSerializerContext`:

```csharp
[JsonSerializable(typeof(CriarCobrancaRequest))]
internal partial class JsonContext : JsonSerializerContext
{
}
```

**BenefÃ­cios**:

- âœ… Menor custo de reflexÃ£o em serializaÃ§Ã£o
- âœ… Contratos JSON mais explÃ­citos
- âœ… Performance e previsibilidade em payloads complexos

**ImplementaÃ§Ã£o**: `samples/Pix/Pix.MockServer/Application/JsonContext.cs`
return TypedResults.UnprocessableEntity(error); // Results<UnprocessableEntity<T>>

// vs. IResult genÃ©rico (nÃ£o tipado)
return Results.Ok(produto);

````

---

### 5. **Melhor IntegraÃ§Ã£o com OpenAPI/Swagger**

#### Antes
```csharp
group.MapGet("/{id}", Handler)
    .Produces<ProdutoResponse>(200)
    .Produces<ErrorResponse>(404);
````

#### Depois (.NET 10 - AutomÃ¡tico)

```csharp
group.MapGet("/{id}", Handler)
    .WithOpenApi()
    .Accepts<CriarProdutoRequest>("application/json")
    .Produces<ProdutoResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
    .WithDescription("ObtÃ©m um produto")
    .WithSummary("Obter produto");

// Swagger gera documentaÃ§Ã£o PRECISA com:
// âœ… Request/Response schemas tipados
// âœ… Todos os status codes listados
// âœ… DescriÃ§Ãµes detalhadas
// âœ… Exemplos de valores
```

---

### 6. **`WithParameterValidation()`**

Novo atributo do .NET 10 para validaÃ§Ã£o de parÃ¢metros:

```csharp
group.MapGet("/", ListarProdutos)
    .WithParameterValidation()  // .NET 10 - Valida automaticamente
    .Produces<PaginatedResponse<ProdutoResponse>>(200)
    .Produces<ErrorResponse>(400);
```

---

## ðŸ“‹ MudanÃ§as Implementadas no Projeto

### Arquivo: `src/Produtos/Produtos.API/Endpoints/ProdutoEndpoints.cs`

#### **Assinatura de MÃ©todo - Antes**

```csharp
private static async Task<IResult> ListarProdutos(
    IProdutoService service, ...)
```

#### **Assinatura de MÃ©todo - Depois**

```csharp
private static async Task<Results<Ok<PaginatedResponse<ProdutoResponse>>, BadRequest<ErrorResponse>>>
ListarProdutos(IProdutoService service, ...)
```

**MudanÃ§as em cada endpoint**:

| Endpoint     | Antes         | Depois                                                           | BenefÃ­cio                         |
| ------------ | ------------- | ---------------------------------------------------------------- | --------------------------------- |
| GET /        | Task<IResult> | Task<Results<Ok<...>, BadRequest<...>>>                          | Type-safe, mÃºltiplos return types |
| GET /{id}    | Task<IResult> | Task<Results<Ok<...>, NotFound<...>>>                            | ForÃ§a tratamento de 404           |
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

### ðŸš€ Recursos Facilitadores para Vertical Slice

A nova arquitetura de **Vertical Slice** utilizada em `src/Pedidos/` se beneficia das melhorias do .NET 10:

- **IEndpoint scan automÃ¡tico**: o mÃ©todo de extensÃ£o `AddEndpointsFromAssembly()` elimina a necessidade de registrar cada rota manualmente, permitindo que slices sejam registrados somente por estarem na assembly.

    ```csharp
    builder.Services.AddEndpointsFromAssembly(typeof(CreatePedidoEndpoint).Assembly);
    ```

- **Primary constructors para handlers**: handlers de comandos podem declarar dependÃªncias diretamente no construtor de registro conciso.

    ```csharp
    public sealed class CreatePedidoHandler(IAppDbContext db, ILogger<CreatePedidoHandler> log)
    {
        public async Task<Result> Handle(CreatePedidoCommand cmd) { ... }
    }
    ```

- **Collection expressions** tornam o mapeamento de mÃºltiplos endpoints mais conciso quando agrupados dinamicamente.
    ```csharp
    var slices = new[] { typeof(CreatePedidoEndpoint), typeof(CancelPedidoEndpoint) };
    foreach(var type in slices) builder.Services.AddEndpoints(type);
    ```

Estas facilidades tornam o desenvolvimento de cada slice extremamente leve e eliminam boilerplate que antes era inevitÃ¡vel em APIs de grande escala.

---

## ðŸ“Š Comparativa: Antes vs Depois

### Handlers HTTP

**Antes - IResult genÃ©rico**

```csharp
private static async Task<IResult> CriarProduto(CriarProdutoRequest req, ...)
{
    // Swagger nÃ£o sabe quais status cÃ³digos sÃ£o possÃ­veis
    // VerificaÃ§Ã£o de tipo apenas em runtime
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
        // Compilador forÃ§a tratamento de todos os cenÃ¡rios
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

## ðŸ›¡ï¸ BenefÃ­cios de Type-Safety

### Antes: Swagger impreciso

```json
{
    "responses": {
        "200": {
            "description": "Success",
            "content": {
                "application/json": {
                    "schema": {} // âŒ Schema vazio, tipo desconhecido
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
                    "schema": { "$ref": "#/components/schemas/ProdutoResponse" } // âœ… Schema completo
                }
            }
        },
        "404": {
            "description": "Not Found",
            "content": {
                "application/json": {
                    "schema": { "$ref": "#/components/schemas/ErrorResponse" } // âœ… Listado
                }
            }
        }
    }
}
```

---

## ðŸ§ª Testes do .NET 10

O projeto inclui testes abrangentes validando:

âœ… **Unit Tests** (ProdutoServiceTests.cs)

- Testes de service com mocking
- 16+ cases cobrindo todos os cenÃ¡rios

âœ… **Integration Tests** (ProdutoEndpointsTests.cs)

- Testes de HTTP status codes
- Testes de validaÃ§Ã£o
- 18+ cases

âœ… **Validator Tests** (ProdutoValidatorTests.cs)

- Testes de regras de negÃ³cio
- 20+ cases

---

## ðŸš€ Como Executar e Testar

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
dotnet test --filter "FullyQualifiedName~FacShopAPI.Tests.Services"
dotnet test --filter "FullyQualifiedName~FacShopAPI.Tests.Endpoints"
```

### Run Application

```bash
dotnet run --project FacShopAPI.csproj
# Acesse: http://localhost:5000
# Swagger UI: http://localhost:5000/swagger
```

---

## ðŸ“¦ DependÃªncias Atualizadas para .NET 10

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

## ðŸŽ“ Recursos de Aprendizado

### DocumentaÃ§Ã£o Oficial

- [.NET 10 Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Typed Results in .NET 10](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses)
- [OpenAPI with Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/openapi)

### ReferÃªncias do Projeto

- [Melhores Praticas API](./MELHORES-PRATICAS-API.md)
- [EstratÃ©gia de Testes](./ESTRATEGIA-DE-TESTES.md)
- [Code Structure](./ESTRUTURA-DO-CODIGO.md)

---

## ðŸ”„ PrÃ³ximos Passos Opcionais

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

## ðŸ“ Checklist de MigraÃ§Ã£o Completa

âœ… Framework atualizado para .NET 10.0  
âœ… Pacotes NuGet atualizados para compatibilidade .NET 10  
âœ… Typed Results implementados em todos endpoints  
âœ… Discriminated Union Results para mÃºltiplas respostas  
âœ… MapGroup com prefix consolidado  
âœ… OpenAPI/Swagger enhancements aplicadas  
âœ… Testes unitÃ¡rios criados (xUnit, Moq)  
âœ… Testes de integraÃ§Ã£o criados  
âœ… DocumentaÃ§Ã£o atualizada  
âœ… Versionamento de projeto: 1.0.0 â†’ 3.1.0

---

## ðŸ† ConclusÃ£o

O projeto **FacShopAPI v3.1.0** agora demonstra as melhores prÃ¡ticas modernas do **.NET 10 LTS** com:

ðŸŽ¯ **Type-Safety** atravÃ©s de Typed Results  
ðŸŽ¯ **DocumentaÃ§Ã£o Precisa** com OpenAPI automÃ¡tico  
ðŸŽ¯ **Code Organization** com MapGroup  
ðŸŽ¯ **Comprehensive Testing** com xUnit e Moq  
ðŸŽ¯ **Production-Ready** patterns e practices

Ã‰ um recurso educacional excelente para aprender Minimal API no .NET 10!

---

**Autor**: GitHub Copilot  
**Ãšltima AtualizaÃ§Ã£o**: 2026-03-04  
**Status**: âœ… Completo para .NET 10 LTS
