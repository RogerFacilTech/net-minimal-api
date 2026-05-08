# EstratÃ©gia de Testes

## 1. Projetos de Teste

| Projeto                | Testes  | Escopo                            |
| ---------------------- | ------- | --------------------------------- |
| `FacShopAPI.Tests`    | 143     | CatÃ¡logo (integraÃ§Ã£o + unitÃ¡rios) |
| `Pix.MockServer.Tests` | 7       | IntegraÃ§Ã£o HTTP PIX               |
| **Total**              | **150** |                                   |

> `Pedidos.Tests` existe no repositÃ³rio mas tem uma dependÃªncia pendente de correÃ§Ã£o â€” nÃ£o estÃ¡ incluÃ­do na contagem acima.

---

## 2. DistribuiÃ§Ã£o por Tipo â€” FacShopAPI.Tests

| Tipo                  | Escopo                                               | Aprox. |
| --------------------- | ---------------------------------------------------- | ------ |
| IntegraÃ§Ã£o (CatÃ¡logo) | Endpoints HTTP completos via `HttpClient`            | ~80    |
| Rate limiting         | PolÃ­ticas de throttling via `RateLimitingApiFactory` | 3      |
| UnitÃ¡rios (domÃ­nio)   | Entidades, value objects, invariantes                | ~30    |
| Validators            | Regras FluentValidation                              | ~30    |

---

## 3. ApiFactory e Isolamento de Rate Limiting

Este Ã© o ponto de maior atenÃ§Ã£o ao escrever novos testes de integraÃ§Ã£o para o CatÃ¡logo.

### `ApiFactory` (base)

Factory base para todos os testes funcionais. Configura `Environment = "Testing"`, sobe o banco InMemory e executa o `DbSeeder`. Registra as trÃªs polÃ­ticas de rate limiting com limite `10000` para que nunca interfiram nos testes de comportamento funcional.

### `RateLimitingApiFactory`

Estende `ApiFactory` e sobrescreve o registro de `AddRateLimiting()`, aplicando limites baixos propositalmente:

| PolÃ­tica          | Limite       |
| ----------------- | ------------ |
| `leitura`         | 3 req/janela |
| `escrita`         | 3 req/janela |
| `criacao-produto` | 2 req/janela |

O objetivo Ã© permitir que os testes atinjam o limite `429` com poucas requisiÃ§Ãµes, sem depender de timing real.

### Por que o `Program.cs` nÃ£o registra rate limiting em Testing

Em `Environment = "Testing"`, a chamada `AddCatalogoRateLimiting()` Ã© omitida no `Program.cs`. Isso evita conflito de chave duplicada (`InvalidOperationException`) quando a factory tenta registrar suas prÃ³prias polÃ­ticas durante o `WebApplicationFactory.CreateHost()`.

### Isolamento entre classes de teste

Cada classe de teste usa `IClassFixture<T>` com sua prÃ³pria instÃ¢ncia de factory. Isso garante que o estado interno do rate limiter (contadores de janela) nÃ£o vaze entre classes de teste diferentes, evitando falhas intermitentes por ordem de execuÃ§Ã£o.

---

## 4. Executar Testes

```bash
# Todos os testes funcionais
dotnet test tests/FacShopAPI.Tests/FacShopAPI.Tests.csproj

# Apenas testes de rate limiting
dotnet test tests/FacShopAPI.Tests/ --filter "FullyQualifiedName~RateLimitingTests"

# Testes PIX
dotnet test samples/Pix/Pix.MockServer.Tests/
```

---

## 5. CenÃ¡rios CrÃ­ticos Cobertos

### CatÃ¡logo

- CRUD completo para todos os 5 recursos (Produto, Categoria, Variante, Atributo, MÃ­dia)
- PaginaÃ§Ã£o (tamanho de pÃ¡gina, cursor/offset)
- Soft delete: produto inativo retorna `404` em todos os endpoints, incluindo GET por ID
- `422 Unprocessable Entity` para payloads que violam validators FluentValidation
- `404 Not Found` para recursos inexistentes
- Rate limiting: sequÃªncia de requisiÃ§Ãµes que excede o limite retorna `429` com header `Retry-After`

### Pedidos

- Criar pedido, consultar por ID, listar
- Adicionar item a pedido existente
- Cancelar pedido
- Invariantes de domÃ­nio: nÃ£o Ã© permitido adicionar item a pedido cancelado, nem cancelar pedido jÃ¡ entregue

### PIX

- Fluxo OAuth2: obtenÃ§Ã£o de token e uso em requisiÃ§Ãµes subsequentes
- SeguranÃ§a mTLS: rejeiÃ§Ã£o de requisiÃ§Ãµes sem certificado cliente vÃ¡lido
- Idempotency key: mesma chave retorna a resposta cacheada
- Conflito `409`: payload divergente para a mesma chave de idempotÃªncia
- Fluxo de liquidaÃ§Ã£o: criaÃ§Ã£o de cobranÃ§a, webhook de liquidaÃ§Ã£o e consulta de status atualizado

---

## 6. Diretrizes para Novos Testes

| SituaÃ§Ã£o                        | Diretriz                                                                        |
| ------------------------------- | ------------------------------------------------------------------------------- |
| Novo endpoint                   | Cobrir: resposta `2xx` com sucesso, `4xx` de validaÃ§Ã£o, `404` quando aplicÃ¡vel  |
| Nova regra de domÃ­nio           | Escrever teste unitÃ¡rio no agregado **antes** do teste de integraÃ§Ã£o            |
| Endpoint de escrita no CatÃ¡logo | Obter token com `AuthHelper.ObterTokenAsync(client)` antes de chamar o endpoint |
| AsserÃ§Ã£o de rate limiting       | Usar `RateLimitingApiFactory`, nunca `ApiFactory`                               |
| Novos produtos criados em teste | IDs comeÃ§am a partir de 9 (DbSeeder reserva 1â€“8)                                |

---

## 7. Comandos de ExecuÃ§Ã£o Detalhados

```bash
# Executar toda a suÃ­te
dotnet test FacShopAPI.slnx -v minimal

# Por projeto
dotnet test tests/FacShopAPI.Tests/FacShopAPI.Tests.csproj -v minimal
dotnet test samples/Pix/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj -v minimal

# Por categoria â€” filtros de namespace
dotnet test tests/FacShopAPI.Tests/ --filter "FullyQualifiedName~Unit.Domain"
dotnet test tests/FacShopAPI.Tests/ --filter "FullyQualifiedName~Unit.Common"
dotnet test tests/FacShopAPI.Tests/ --filter "FullyQualifiedName~Services"
dotnet test tests/FacShopAPI.Tests/ --filter "FullyQualifiedName~Endpoints"
dotnet test tests/FacShopAPI.Tests/ --filter "FullyQualifiedName~Validators"
dotnet test tests/FacShopAPI.Tests/ --filter "FullyQualifiedName~Integration.Catalogo"
dotnet test tests/FacShopAPI.Tests/ --filter "FullyQualifiedName~Integration.Pedidos"
dotnet test tests/FacShopAPI.Tests/ --filter "FullyQualifiedName~RateLimitingTests"

# Somente testes de rate limiting
dotnet test tests/FacShopAPI.Tests/ \
  --filter "FullyQualifiedName~RateLimitingTests" -v detailed

# Com cobertura (requer dotnet-coverage ou coverlet)
dotnet test FacShopAPI.slnx --collect:"XPlat Code Coverage"
```

---

## 8. Exemplos de CÃ³digo de Teste

### Teste de integraÃ§Ã£o HTTP â€” endpoint do CatÃ¡logo

```csharp
// tests/FacShopAPI.Tests/Endpoints/ProdutoEndpointsTests.cs
public class ProdutoEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ProdutoEndpointsTests(ApiFactory factory) => _factory = factory;

    private async Task<HttpClient> CriarClienteAutenticadoAsync()
    {
        var client = _factory.CreateClient();
        var token = await AuthHelper.ObterTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task CriarProduto_ComDadosValidos_Retorna201()
    {
        var client = await CriarClienteAutenticadoAsync();
        var request = new CriarProdutoRequest
        {
            Nome = "Teclado MecÃ¢nico",
            Descricao = "Switch blue, retroiluminado",
            Preco = 349.90m,
            Estoque = 50,
            Categoria = "EletrÃ´nicos"
        };

        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var produto = await response.Content.ReadFromJsonAsync<ProdutoResponse>();
        produto!.Nome.Should().Be("Teclado MecÃ¢nico");
    }

    [Fact]
    public async Task CriarProduto_SemToken_Retorna401()
    {
        var client = _factory.CreateClient();      // sem autenticaÃ§Ã£o
        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos",
            new { nome = "Teste" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletarProduto_TornaInativo_ERetorna404NoGet()
    {
        var client = await CriarClienteAutenticadoAsync();
        // Soft delete â€” retorna 204
        var del = await client.DeleteAsync("/api/v1/catalogo/produtos/1");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Produto inativo deve retornar 404
        var get = await client.GetAsync("/api/v1/catalogo/produtos/1");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

### Teste unitÃ¡rio de domÃ­nio â€” sem dependÃªncias de infraestrutura

```csharp
// tests/FacShopAPI.Tests/Unit/Domain/ProdutoTests.cs
public class ProdutoTests
{
    [Fact]
    public void Criar_ComDadosValidos_RetornaProduto()
    {
        var result = Produto.Criar("Notebook", "DescriÃ§Ã£o completa", 1000m, "EletrÃ´nicos", 5, "a@b.com");
        result.IsSuccess.Should().BeTrue();
        result.Value!.Nome.Should().Be("Notebook");
    }

    [Fact]
    public void Criar_NomeCurto_RetornaFalha()
    {
        var result = Produto.Criar("AB", "Desc", 100m, "Livros", 1, "a@b.com");
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("3");       // mensagem menciona mÃ­nimo de 3 chars
    }

    [Fact]
    public void Criar_PrecoZero_RetornaFalha()
    {
        var result = Produto.Criar("Notebook", "Desc", 0m, "Livros", 1, "a@b.com");
        result.IsSuccess.Should().BeFalse();
    }
}
```

### Teste de rate limiting â€” usando `RateLimitingApiFactory`

```csharp
// tests/FacShopAPI.Tests/Integration/RateLimitingTests.cs
public class RateLimitingTests : IClassFixture<RateLimitingApiFactory>
{
    private readonly RateLimitingApiFactory _factory;
    public RateLimitingTests(RateLimitingApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ExcedeLimiteLeitura_Retorna429()
    {
        var client = _factory.CreateClient();

        // leitura limit = 3 no RateLimitingApiFactory
        for (var i = 0; i < 3; i++)
            await client.GetAsync("/api/v1/catalogo/produtos");

        // 4Âª requisiÃ§Ã£o deve ser rejeitada
        var response = await client.GetAsync("/api/v1/catalogo/produtos");
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.Contains("Retry-After").Should().BeTrue();
    }
}
```

### Teste de domÃ­nio Pedido â€” invariante de agregado

```csharp
// tests/FacShopAPI.Tests/Unit/Domain/PedidoTests.cs
public class PedidoTests
{
    [Fact]
    public void AddItem_PedidoCancelado_RetornaFalha()
    {
        var pedido = Pedido.Create("Cliente Teste").Value!;
        pedido.Cancel();                             // pedido agora estÃ¡ Cancelado

        var produto = new Produto { Estoque = 10 };
        var result = pedido.AddItem(produto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Pedido nÃ£o estÃ¡ aberto");
    }
}
```

---

## 9. DistribuiÃ§Ã£o de Testes por Arquivo

| Arquivo                                           | Tipo                       | Aprox.     |
| ------------------------------------------------- | -------------------------- | ---------- |
| `Unit/Domain/ProdutoTests.cs`                     | UnitÃ¡rio â€” domÃ­nio         | ~15        |
| `Unit/Domain/CategoriaTests.cs`                   | UnitÃ¡rio â€” domÃ­nio         | ~10        |
| `Unit/Domain/PedidoTests.cs`                      | UnitÃ¡rio â€” domÃ­nio         | ~8         |
| `Unit/Common/ResultTests.cs`                      | UnitÃ¡rio â€” tipos comuns    | ~5         |
| `Services/ProdutoServiceTests.cs`                 | UnitÃ¡rio â€” serviÃ§o         | ~15        |
| `Endpoints/ProdutoEndpointsTests.cs`              | IntegraÃ§Ã£o HTTP            | ~25        |
| `Integration/Catalogo/CategoriaEndpointsTests.cs` | IntegraÃ§Ã£o HTTP            | ~20        |
| `Integration/Pedidos/*.cs` (5 arquivos)           | IntegraÃ§Ã£o HTTP            | ~40        |
| `Integration/RateLimitingTests.cs`                | IntegraÃ§Ã£o rate limiting   | 3          |
| `Validators/*.cs`                                 | ValidaÃ§Ã£o FluentValidation | ~24        |
| `Pix.MockServer.Tests/*.cs`                       | IntegraÃ§Ã£o HTTP (PIX)      | 7          |
| **Total**                                         |                            | **~150+7** |
