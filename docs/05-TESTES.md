# Estratégia de Testes

## 1. Projetos de Teste

| Projeto                | Testes  | Escopo                            |
| ---------------------- | ------- | --------------------------------- |
| `ProdutosAPI.Tests`    | 143     | Catálogo (integração + unitários) |
| `Pix.MockServer.Tests` | 7       | Integração HTTP PIX               |
| **Total**              | **150** |                                   |

> `Pedidos.Tests` existe no repositório mas tem uma dependência pendente de correção — não está incluído na contagem acima.

---

## 2. Distribuição por Tipo — ProdutosAPI.Tests

| Tipo                  | Escopo                                               | Aprox. |
| --------------------- | ---------------------------------------------------- | ------ |
| Integração (Catálogo) | Endpoints HTTP completos via `HttpClient`            | ~80    |
| Rate limiting         | Políticas de throttling via `RateLimitingApiFactory` | 3      |
| Unitários (domínio)   | Entidades, value objects, invariantes                | ~30    |
| Validators            | Regras FluentValidation                              | ~30    |

---

## 3. ApiFactory e Isolamento de Rate Limiting

Este é o ponto de maior atenção ao escrever novos testes de integração para o Catálogo.

### `ApiFactory` (base)

Factory base para todos os testes funcionais. Configura `Environment = "Testing"`, sobe o banco InMemory e executa o `DbSeeder`. Registra as três políticas de rate limiting com limite `10000` para que nunca interfiram nos testes de comportamento funcional.

### `RateLimitingApiFactory`

Estende `ApiFactory` e sobrescreve o registro de `AddRateLimiting()`, aplicando limites baixos propositalmente:

| Política          | Limite       |
| ----------------- | ------------ |
| `leitura`         | 3 req/janela |
| `escrita`         | 3 req/janela |
| `criacao-produto` | 2 req/janela |

O objetivo é permitir que os testes atinjam o limite `429` com poucas requisições, sem depender de timing real.

### Por que o `Program.cs` não registra rate limiting em Testing

Em `Environment = "Testing"`, a chamada `AddCatalogoRateLimiting()` é omitida no `Program.cs`. Isso evita conflito de chave duplicada (`InvalidOperationException`) quando a factory tenta registrar suas próprias políticas durante o `WebApplicationFactory.CreateHost()`.

### Isolamento entre classes de teste

Cada classe de teste usa `IClassFixture<T>` com sua própria instância de factory. Isso garante que o estado interno do rate limiter (contadores de janela) não vaze entre classes de teste diferentes, evitando falhas intermitentes por ordem de execução.

---

## 4. Executar Testes

```bash
# Todos os testes funcionais
dotnet test tests/ProdutosAPI.Tests/ProdutosAPI.Tests.csproj

# Apenas testes de rate limiting
dotnet test tests/ProdutosAPI.Tests/ --filter "FullyQualifiedName~RateLimitingTests"

# Testes PIX
dotnet test tests/Pix.MockServer.Tests/
```

---

## 5. Cenários Críticos Cobertos

### Catálogo

- CRUD completo para todos os 5 recursos (Produto, Categoria, Variante, Atributo, Mídia)
- Paginação (tamanho de página, cursor/offset)
- Soft delete: produto inativo retorna `404` em todos os endpoints, incluindo GET por ID
- `422 Unprocessable Entity` para payloads que violam validators FluentValidation
- `404 Not Found` para recursos inexistentes
- Rate limiting: sequência de requisições que excede o limite retorna `429` com header `Retry-After`

### Pedidos

- Criar pedido, consultar por ID, listar
- Adicionar item a pedido existente
- Cancelar pedido
- Invariantes de domínio: não é permitido adicionar item a pedido cancelado, nem cancelar pedido já entregue

### PIX

- Fluxo OAuth2: obtenção de token e uso em requisições subsequentes
- Segurança mTLS: rejeição de requisições sem certificado cliente válido
- Idempotency key: mesma chave retorna a resposta cacheada
- Conflito `409`: payload divergente para a mesma chave de idempotência
- Fluxo de liquidação: criação de cobrança, webhook de liquidação e consulta de status atualizado

---

## 6. Diretrizes para Novos Testes

| Situação                        | Diretriz                                                                        |
| ------------------------------- | ------------------------------------------------------------------------------- |
| Novo endpoint                   | Cobrir: resposta `2xx` com sucesso, `4xx` de validação, `404` quando aplicável  |
| Nova regra de domínio           | Escrever teste unitário no agregado **antes** do teste de integração            |
| Endpoint de escrita no Catálogo | Obter token com `AuthHelper.ObterTokenAsync(client)` antes de chamar o endpoint |
| Asserção de rate limiting       | Usar `RateLimitingApiFactory`, nunca `ApiFactory`                               |
| Novos produtos criados em teste | IDs começam a partir de 9 (DbSeeder reserva 1–8)                                |

---

## 7. Comandos de Execução Detalhados

```bash
# Executar toda a suíte
dotnet test FacShopAPI.slnx -v minimal

# Por projeto
dotnet test tests/ProdutosAPI.Tests/ProdutosAPI.Tests.csproj -v minimal
dotnet test tests/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj -v minimal

# Por categoria — filtros de namespace
dotnet test tests/ProdutosAPI.Tests/ --filter "FullyQualifiedName~Unit.Domain"
dotnet test tests/ProdutosAPI.Tests/ --filter "FullyQualifiedName~Unit.Common"
dotnet test tests/ProdutosAPI.Tests/ --filter "FullyQualifiedName~Services"
dotnet test tests/ProdutosAPI.Tests/ --filter "FullyQualifiedName~Endpoints"
dotnet test tests/ProdutosAPI.Tests/ --filter "FullyQualifiedName~Validators"
dotnet test tests/ProdutosAPI.Tests/ --filter "FullyQualifiedName~Integration.Catalogo"
dotnet test tests/ProdutosAPI.Tests/ --filter "FullyQualifiedName~Integration.Pedidos"
dotnet test tests/ProdutosAPI.Tests/ --filter "FullyQualifiedName~RateLimitingTests"

# Somente testes de rate limiting
dotnet test tests/ProdutosAPI.Tests/ \
  --filter "FullyQualifiedName~RateLimitingTests" -v detailed

# Com cobertura (requer dotnet-coverage ou coverlet)
dotnet test FacShopAPI.slnx --collect:"XPlat Code Coverage"
```

---

## 8. Exemplos de Código de Teste

### Teste de integração HTTP — endpoint do Catálogo

```csharp
// tests/ProdutosAPI.Tests/Endpoints/ProdutoEndpointsTests.cs
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
            Nome = "Teclado Mecânico",
            Descricao = "Switch blue, retroiluminado",
            Preco = 349.90m,
            Estoque = 50,
            Categoria = "Eletrônicos"
        };

        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var produto = await response.Content.ReadFromJsonAsync<ProdutoResponse>();
        produto!.Nome.Should().Be("Teclado Mecânico");
    }

    [Fact]
    public async Task CriarProduto_SemToken_Retorna401()
    {
        var client = _factory.CreateClient();      // sem autenticação
        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos",
            new { nome = "Teste" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletarProduto_TornaInativo_ERetorna404NoGet()
    {
        var client = await CriarClienteAutenticadoAsync();
        // Soft delete — retorna 204
        var del = await client.DeleteAsync("/api/v1/catalogo/produtos/1");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Produto inativo deve retornar 404
        var get = await client.GetAsync("/api/v1/catalogo/produtos/1");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

### Teste unitário de domínio — sem dependências de infraestrutura

```csharp
// tests/ProdutosAPI.Tests/Unit/Domain/ProdutoTests.cs
public class ProdutoTests
{
    [Fact]
    public void Criar_ComDadosValidos_RetornaProduto()
    {
        var result = Produto.Criar("Notebook", "Descrição completa", 1000m, "Eletrônicos", 5, "a@b.com");
        result.IsSuccess.Should().BeTrue();
        result.Value!.Nome.Should().Be("Notebook");
    }

    [Fact]
    public void Criar_NomeCurto_RetornaFalha()
    {
        var result = Produto.Criar("AB", "Desc", 100m, "Livros", 1, "a@b.com");
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("3");       // mensagem menciona mínimo de 3 chars
    }

    [Fact]
    public void Criar_PrecoZero_RetornaFalha()
    {
        var result = Produto.Criar("Notebook", "Desc", 0m, "Livros", 1, "a@b.com");
        result.IsSuccess.Should().BeFalse();
    }
}
```

### Teste de rate limiting — usando `RateLimitingApiFactory`

```csharp
// tests/ProdutosAPI.Tests/Integration/RateLimitingTests.cs
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

        // 4ª requisição deve ser rejeitada
        var response = await client.GetAsync("/api/v1/catalogo/produtos");
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.Contains("Retry-After").Should().BeTrue();
    }
}
```

### Teste de domínio Pedido — invariante de agregado

```csharp
// tests/ProdutosAPI.Tests/Unit/Domain/PedidoTests.cs
public class PedidoTests
{
    [Fact]
    public void AddItem_PedidoCancelado_RetornaFalha()
    {
        var pedido = Pedido.Create("Cliente Teste").Value!;
        pedido.Cancel();                             // pedido agora está Cancelado

        var produto = new Produto { Estoque = 10 };
        var result = pedido.AddItem(produto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Pedido não está aberto");
    }
}
```

---

## 9. Distribuição de Testes por Arquivo

| Arquivo                                           | Tipo                       | Aprox.     |
| ------------------------------------------------- | -------------------------- | ---------- |
| `Unit/Domain/ProdutoTests.cs`                     | Unitário — domínio         | ~15        |
| `Unit/Domain/CategoriaTests.cs`                   | Unitário — domínio         | ~10        |
| `Unit/Domain/PedidoTests.cs`                      | Unitário — domínio         | ~8         |
| `Unit/Common/ResultTests.cs`                      | Unitário — tipos comuns    | ~5         |
| `Services/ProdutoServiceTests.cs`                 | Unitário — serviço         | ~15        |
| `Endpoints/ProdutoEndpointsTests.cs`              | Integração HTTP            | ~25        |
| `Integration/Catalogo/CategoriaEndpointsTests.cs` | Integração HTTP            | ~20        |
| `Integration/Pedidos/*.cs` (5 arquivos)           | Integração HTTP            | ~40        |
| `Integration/RateLimitingTests.cs`                | Integração rate limiting   | 3          |
| `Validators/*.cs`                                 | Validação FluentValidation | ~24        |
| `Pix.MockServer.Tests/*.cs`                       | Integração HTTP (PIX)      | 7          |
| **Total**                                         |                            | **~150+7** |
