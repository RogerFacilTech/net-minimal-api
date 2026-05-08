# Pix — Mock Server e Cliente HTTP

Este módulo demonstra como estruturar chamadas de API externas em .NET com `HttpClientFactory`, modelar payloads JSON complexos e aplicar boas práticas de idempotência, correlação e tratamento de erro padronizado.

## Objetivo

Demonstrar como estruturar chamadas de API com `HttpClientFactory` e como modelar payloads JSON complexos de forma organizada, com validação, idempotência, correlação e tratamento padronizado de erro.

## Arquitetura da demo

```text
Pix.ClientDemo (Console)
  -> OAuth2 mock (/oauth/token)
  -> Pix.MockServer (/pix/v1/...)

Pix.MockServer
  - Contracts: payloads HTTP
  - Application: regras de negócio e validações
  - Infrastructure/InMemory: repositórios thread-safe
  - Security: Bearer + mTLS real (certificado de cliente no handshake TLS)
```

## Endpoints

- `POST /oauth/token`
- `POST /pix/v1/cobrancas`
- `GET /pix/v1/cobrancas/{txid}`
- `POST /pix/v1/cobrancas/{txid}/simular-liquidacao`
- `POST /pix/v1/devolucoes`
- `GET /pix/v1/devolucoes/{devolucaoId}`
- `GET /health`

## Exemplo completo de payload (cobrança)

```json
{
    "calendario": {
        "expiracao": 3600
    },
    "devedor": {
        "nome": "Cliente de Teste",
        "cpfCnpj": "12345678901",
        "endereco": {
            "logradouro": "Rua das Flores",
            "numero": "100",
            "cidade": "Sao Paulo",
            "uf": "SP",
            "cep": "01001000"
        }
    },
    "recebedor": {
        "nome": "Empresa Demo",
        "ispb": "12345678",
        "agencia": "0001",
        "conta": "123456",
        "tipoConta": "CACC"
    },
    "valor": {
        "original": 150.75,
        "abatimento": 0,
        "desconto": 5,
        "juros": 0,
        "multa": 0
    },
    "chavePix": "chave-pix-demo-123",
    "solicitacaoPagador": "Pagamento referente ao pedido #12345",
    "split": [
        {
            "favorecido": "Parceiro A",
            "documento": "98765432100",
            "valor": 30,
            "percentual": 20
        },
        {
            "favorecido": "Parceiro B",
            "documento": "11222333000181",
            "valor": 15,
            "percentual": 10
        }
    ],
    "infoAdicionais": [
        { "nome": "pedido", "valor": "12345" },
        { "nome": "canal", "valor": "mobile" }
    ],
    "metadata": {
        "origemSistema": "erp",
        "prioridade": "alta",
        "tags": ["pix", "educacional"]
    }
}
```

Exemplos completos versionados no repositório:

- [oauth-token-request.json](examples/pix-json/oauth-token-request.json)
- [oauth-token-response.json](examples/pix-json/oauth-token-response.json)
- [cobranca-create-request.json](examples/pix-json/cobranca-create-request.json)
- [cobranca-create-response.json](examples/pix-json/cobranca-create-response.json)
- [devolucao-create-request.json](examples/pix-json/devolucao-create-request.json)
- [devolucao-create-response.json](examples/pix-json/devolucao-create-response.json)
- [problem-validation-response.json](examples/pix-json/problem-validation-response.json)
- [problem-idempotency-conflict-response.json](examples/pix-json/problem-idempotency-conflict-response.json)

Guia complementar:

- [guias/JSON-COMPLEXO-E-BOAS-PRATICAS.md](guias/JSON-COMPLEXO-E-BOAS-PRATICAS.md)

## Anti-padrões comuns vs boa prática

- Anti-padrão: objeto JSON monolítico sem subestruturas semânticas.
- Boa prática: quebrar em blocos (`devedor`, `recebedor`, `valor`, `split`, `metadata`) e validar cada bloco.

- Anti-padrão: ausência de `Idempotency-Key` em `POST` financeiro.
- Boa prática: sempre enviar `Idempotency-Key` e rejeitar reaproveitamento com payload diferente.

- Anti-padrão: sem correlação entre chamadas.
- Boa prática: enviar `X-Correlation-Id` em todas as requisições.

- Anti-padrão: `HttpClient` criado manualmente por chamada.
- Boa prática: `HttpClientFactory` + cliente tipado + resiliência (`AddStandardResilienceHandler`).

## Como executar (2 terminais)

### Terminal 1: subir mock server

```bash
dotnet run --project samples/Pix/Pix.MockServer/Pix.MockServer.csproj
```

### Terminal 2: rodar cliente didático

```bash
dotnet run --project samples/Pix/Pix.ClientDemo/Pix.ClientDemo.csproj
```

### Executar testes da demo

```bash
dotnet test samples/Pix/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj
```

Persistência é em memória (objetivo didático). Segurança usa mTLS real + Bearer token. Em ambiente `Testing`, existe fallback de validação por header apenas para testes automatizados (`WebApplicationFactory`). Não há webhook nesta primeira versão.

---

## Pipeline de HttpClient — registro completo

```csharp
// samples/Pix/Pix.ClientDemo/Program.cs
builder.Services.AddSharedHttpInfrastructure(options =>
{
    options.PathContains = ["/pix/v1/cobrancas"];
});
// Registra CorrelationIdHandler, IdempotencyKeyHandler e RequestLoggingHandler
// (definidos em src/Shared/Http/)

builder.Services.AddHttpClient<PixProcessingClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<PixClientOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
})
.ConfigurePrimaryHttpMessageHandler(MutualTlsHttpHandlerFactory.Create) // mTLS — certificado de cliente
.AddHttpMessageHandler<CorrelationIdHandler>()   // handlers em cadeia (ordem importa)
.AddHttpMessageHandler<IdempotencyKeyHandler>()
.AddHttpMessageHandler<RequestLoggingHandler>()
.AddDefaultApiResiliencePipeline("pix", new ApiClientOptionsBase()); // Retry + Circuit Breaker + Timeout
```

A cadeia de handlers executa de fora para dentro na requisição e de dentro para fora na resposta:

```
Requisição:
  CorrelationIdHandler → IdempotencyKeyHandler → RequestLoggingHandler → mTLS handler → servidor

Resposta:
  servidor → mTLS handler → RequestLoggingHandler → IdempotencyKeyHandler → CorrelationIdHandler
```

### `AddStandardResilienceHandler` — o que inclui

O pacote `Microsoft.Extensions.Http.Resilience` configura automaticamente uma pipeline Polly com:

| Camada                | Comportamento padrão                                  |
| --------------------- | ----------------------------------------------------- |
| Timeout por tentativa | 10s por tentativa individual                          |
| Retry                 | 3 tentativas, backoff exponencial com jitter          |
| Circuit Breaker       | Abre após 50% de falhas em 30s; mantém aberto por 30s |
| Timeout total         | 30s para toda a operação (incluindo retentativas)     |

Para personalizar, troque por `AddResilienceHandler("nome", pipeline => ...)`.

---

## Fluxo OAuth2 — obtenção e uso de token

```csharp
// samples/Pix/Pix.ClientDemo/Client/AuthTokenProvider.cs (simplificado)
public async Task<string> ObterTokenAsync()
{
    var request = new HttpRequestMessage(HttpMethod.Post, "/oauth/token");
    request.Content = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("grant_type", "client_credentials"),
        new KeyValuePair<string, string>("client_id", _options.ClientId),
        new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
    });

    var response = await _httpClient.SendAsync(request);
    response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadFromJsonAsync<TokenResponse>();
    return json!.AccessToken;
}
```

### Idempotência na prática

O `IdempotencyKeyHandler` injeta um UUID v4 por requisição em todos os métodos não-idempotentes:

```csharp
// src/Shared/Http/IdempotencyKeyHandler.cs (simplificado)
protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request, CancellationToken cancellationToken)
{
    if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Delete)
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());

    return await base.SendAsync(request, cancellationToken);
}
```

O servidor mock armazena a resposta associada à chave. Um segundo POST com a mesma chave e payload idêntico retorna a resposta original (`200`). Um segundo POST com a mesma chave mas payload diferente retorna `409 Conflict`.

---

## Teste de integração — exemplo de cenário

```csharp
// samples/Pix/Pix.MockServer.Tests/PixMockServerTests.cs
[Fact]
public async Task Cobranca_MesmaChave_PayloadDivergente_Retorna409()
{
    var client = _factory.CreateClient();
    var key = Guid.NewGuid().ToString();

    // Primeira requisição — cria cobrança
    var res1 = await client.PostAsJsonAsync("/pix/v1/cobrancas",
        CobrancaValida(), CabecalhoComKey(key));
    res1.StatusCode.Should().Be(HttpStatusCode.Created);

    // Segunda requisição — mesma chave, valor diferente
    var res2 = await client.PostAsJsonAsync("/pix/v1/cobrancas",
        CobrancaComValorDiferente(), CabecalhoComKey(key));
    res2.StatusCode.Should().Be(HttpStatusCode.Conflict); // 409
}
```
