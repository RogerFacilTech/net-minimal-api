# Estratégia de Testes

## 1. Projetos de Teste

| Projeto                | Testes (aprox.) | Escopo                                             |
| ---------------------- | --------------- | -------------------------------------------------- |
| `Catalogo.Tests`       | 116             | Catálogo (integração HTTP + unitários + validação) |
| `Pedidos.Tests`        | 47              | Pedidos (endpoints + integração + domínio)         |
| `Pix.MockServer.Tests` | 7               | Integração HTTP PIX (OAuth2, mTLS, idempotência)   |
| **Total**              | **170**         |                                                    |

> Contagem estimada por atributos `[Fact]` e `[Theory]` na branch atual.

---

## 2. Distribuição por Tipo — `Catalogo.Tests`

| Tipo                  | Escopo                                               | Aprox. |
| --------------------- | ---------------------------------------------------- | ------ |
| Integração (Catálogo) | Endpoints HTTP completos via `HttpClient`            | ~55    |
| Rate limiting         | Políticas de throttling via `RateLimitingApiFactory` | 3      |
| Unitários (domínio)   | Entidades, value objects, invariantes                | ~35    |
| Validators            | Regras FluentValidation                              | ~23    |

---

## 3. ApiFactory e Isolamento de Rate Limiting (Catálogo)

Este é o ponto de maior atenção ao escrever novos testes de integração para o Catálogo.

### `ApiFactory` (base)

Factory base para testes funcionais de `Catalogo.Tests`. Configura `Environment = "Testing"`, sobe o banco InMemory e executa o `DbSeeder`. Registra as três políticas de rate limiting com limite `10000` para não interferir em testes funcionais.

### `RateLimitingApiFactory`

Estende `ApiFactory` e sobrescreve o registro de `AddRateLimiting()`, aplicando limites baixos propositalmente:

| Política          | Limite       |
| ----------------- | ------------ |
| `leitura`         | 3 req/janela |
| `escrita`         | 3 req/janela |
| `criacao-produto` | 2 req/janela |

O objetivo é permitir que os testes atinjam `429` com poucas requisições, sem depender de timing real.

### Por que o `Program.cs` não registra rate limiting em Testing

Em `Environment = "Testing"`, a chamada `AddCatalogoRateLimiting()` é omitida no `Program.cs`. Isso evita conflito de chave duplicada (`InvalidOperationException`) quando a factory registra suas próprias políticas durante o `WebApplicationFactory.CreateHost()`.

### Isolamento entre classes de teste

Cada classe de teste usa `IClassFixture<T>` com sua própria instância de factory. Isso evita vazamento de estado interno do rate limiter entre classes de teste e reduz falhas intermitentes por ordem de execução.

---

## 4. Executar Testes

```bash
# Catálogo
dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj -v minimal

# Pedidos
dotnet test src/Pedidos/Pedidos.Tests/Pedidos.Tests.csproj -v minimal

# PIX
dotnet test samples/Pix/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj -v minimal
```

---

## 5. Cenários Críticos Cobertos

### Catálogo

- CRUD completo para os 5 recursos (Produto, Categoria, Variante, Atributo, Mídia)
- Paginação (tamanho de página, cursor/offset)
- Soft delete: produto inativo retorna `404` em todos os endpoints, incluindo GET por ID
- `422 Unprocessable Entity` para payloads inválidos nos validators FluentValidation
- `404 Not Found` para recursos inexistentes
- Rate limiting: sequência de requisições que excede o limite retorna `429` com header `Retry-After`

### Pedidos

- Criar pedido, consultar por ID, listar
- Adicionar item a pedido existente
- Cancelar pedido
- Invariantes de domínio: não permitir adicionar item a pedido cancelado, nem cancelar pedido já entregue

### PIX

- Fluxo OAuth2: obtenção de token e uso em requisições subsequentes
- Segurança mTLS: rejeição de requisições sem certificado cliente válido
- Idempotency key: mesma chave retorna resposta cacheada
- Conflito `409`: payload divergente para a mesma chave de idempotência
- Fluxo de liquidação: criação de cobrança, webhook de liquidação e consulta de status

---

## 6. Diretrizes para Novos Testes

| Situação                        | Diretriz                                                                        |
| ------------------------------- | ------------------------------------------------------------------------------- |
| Novo endpoint                   | Cobrir: resposta `2xx` de sucesso, `4xx` de validação e `404` quando aplicável  |
| Nova regra de domínio           | Escrever teste unitário no agregado **antes** do teste de integração            |
| Endpoint de escrita no Catálogo | Obter token com `AuthHelper.ObterTokenAsync(client)` antes de chamar o endpoint |
| Asserção de rate limiting       | Usar `RateLimitingApiFactory`, nunca `ApiFactory`                               |
| Novos produtos em teste         | IDs começam em 9 (`DbSeeder` reserva 1–8)                                       |

### Modo E2E opcional com Auth.Host real

Por padrão, `AuthHelper` gera JWT localmente para manter os testes isolados.
Para validar emissão real de token no microserviço Auth, defina:

- `AUTH_BASE_URL` (ex.: `http://localhost:5020`)
- `AUTH_ADMIN_EMAIL` (opcional, default `admin@example.com`)
- `AUTH_ADMIN_PASSWORD` (opcional, default `senha123`)

Com `AUTH_BASE_URL` definido, `AuthHelper.ObterTokenAsync(client)` chama `POST /api/v1/auth/login` no Auth.Host.

---

## 7. Comandos de Execução Detalhados

```bash
# Executar toda a suíte da solução
dotnet test FacShop.slnx -v minimal

# Por projeto
dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj -v minimal
dotnet test src/Pedidos/Pedidos.Tests/Pedidos.Tests.csproj -v minimal
dotnet test samples/Pix/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj -v minimal

# Catálogo por categoria (filtro de namespace)
dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj --filter "FullyQualifiedName~Unit.Domain"
dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj --filter "FullyQualifiedName~Unit.Common"
dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj --filter "FullyQualifiedName~Services"
dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj --filter "FullyQualifiedName~Endpoints"
dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj --filter "FullyQualifiedName~Validators"
dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj --filter "FullyQualifiedName~Integration.Catalogo"
dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj --filter "FullyQualifiedName~RateLimitingTests"

# Pedidos por categoria (filtro de namespace)
dotnet test src/Pedidos/Pedidos.Tests/Pedidos.Tests.csproj --filter "FullyQualifiedName~Unit.Domain"
dotnet test src/Pedidos/Pedidos.Tests/Pedidos.Tests.csproj --filter "FullyQualifiedName~Endpoints"
dotnet test src/Pedidos/Pedidos.Tests/Pedidos.Tests.csproj --filter "FullyQualifiedName~Integration"
dotnet test src/Pedidos/Pedidos.Tests/Pedidos.Tests.csproj --filter "FullyQualifiedName~Validators"

# Somente rate limiting (Catálogo)
dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj --filter "FullyQualifiedName~RateLimitingTests" -v detailed

# Com cobertura
dotnet test FacShop.slnx --collect:"XPlat Code Coverage"
```

---

## 8. Exemplos de Código de Teste

### Teste de integração HTTP — endpoint do Catálogo

```csharp
// src/Catalogo/Catalogo.Tests/Endpoints/ProdutoEndpointsTests.cs
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
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos", new { nome = "Teste" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

### Teste unitário de domínio — sem infraestrutura

```csharp
// src/Catalogo/Catalogo.Tests/Unit/Domain/ProdutoTests.cs
public class ProdutoTests
{
    [Fact]
    public void Criar_ComDadosValidos_RetornaProduto()
    {
        var result = Produto.Criar("Notebook", "Descrição completa", 1000m, "Eletrônicos", 5, "a@b.com");
        result.IsSuccess.Should().BeTrue();
        result.Value!.Nome.Should().Be("Notebook");
    }
}
```

### Teste de rate limiting — usando `RateLimitingApiFactory`

```csharp
// src/Catalogo/Catalogo.Tests/Integration/RateLimitingTests.cs
public class RateLimitingTests : IClassFixture<RateLimitingApiFactory>
{
    private readonly RateLimitingApiFactory _factory;
    public RateLimitingTests(RateLimitingApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ExcedeLimiteLeitura_Retorna429()
    {
        var client = _factory.CreateClient();

        for (var i = 0; i < 3; i++)
            await client.GetAsync("/api/v1/catalogo/produtos");

        var response = await client.GetAsync("/api/v1/catalogo/produtos");
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.Contains("Retry-After").Should().BeTrue();
    }
}
```

### Teste de domínio Pedido — invariante de agregado

```csharp
// src/Pedidos/Pedidos.Tests/Unit/Domain/PedidoTests.cs
public class PedidoTests
{
    [Fact]
    public void AdicionarItem_PedidoCancelado_RetornaFalha()
    {
        var pedido = Pedido.Criar();
        pedido.Cancelar("motivo teste");

        var produto = ProdutoTestBuilder.Criar();
        var result = pedido.AdicionarItem(produto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("rascunho");
    }
}
```

---

## 9. Distribuição de Testes por Arquivo

| Arquivo                                                                       | Tipo                       |
| ----------------------------------------------------------------------------- | -------------------------- |
| `src/Catalogo/Catalogo.Tests/Unit/Domain/ProdutoTests.cs`                     | Unitário — domínio         |
| `src/Catalogo/Catalogo.Tests/Unit/Domain/CategoriaTests.cs`                   | Unitário — domínio         |
| `src/Catalogo/Catalogo.Tests/Unit/Domain/VarianteTests.cs`                    | Unitário — domínio         |
| `src/Catalogo/Catalogo.Tests/Unit/Common/ResultTests.cs`                      | Unitário — tipos comuns    |
| `src/Catalogo/Catalogo.Tests/Services/ProdutoServiceTests.cs`                 | Unitário — serviço         |
| `src/Catalogo/Catalogo.Tests/Endpoints/ProdutoEndpointsTests.cs`              | Integração HTTP            |
| `src/Catalogo/Catalogo.Tests/Integration/Catalogo/CategoriaEndpointsTests.cs` | Integração HTTP            |
| `src/Catalogo/Catalogo.Tests/Integration/RateLimitingTests.cs`                | Integração rate limiting   |
| `src/Catalogo/Catalogo.Tests/Validators/ProdutoValidatorTests.cs`             | Validação FluentValidation |
| `src/Pedidos/Pedidos.Tests/Endpoints/*.cs`                                    | Integração HTTP            |
| `src/Pedidos/Pedidos.Tests/Integration/*.cs`                                  | Integração HTTP            |
| `src/Pedidos/Pedidos.Tests/Unit/Domain/*.cs`                                  | Unitário — domínio         |
| `samples/Pix/Pix.MockServer.Tests/*.cs`                                       | Integração HTTP (PIX)      |
