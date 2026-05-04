# EXECUTAR

Comandos para executar os projetos executaveis e os projetos de teste.

## Executaveis

Rodar o host principal da API:

```bash
dotnet run --project src/Host/Host.csproj
```

Rodar o mock server Pix:

```bash
dotnet run --project src/Pix/Pix.MockServer/Pix.MockServer.csproj
```

Rodar o cliente demo do Catalogo:

```bash
dotnet run --project samples/Catalogo.HttpClientDemo/Catalogo.HttpClientDemo.csproj
dotnet run --project samples/Catalogo.HttpClientDemo/Catalogo.HttpClientDemo.csproj "http://localhost:5000"
```

Rodar o cliente demo do Pix:

```bash
dotnet run --project src/Pix/Pix.ClientDemo/Pix.ClientDemo.csproj
```

## Testes

Executar testes de ProdutosAPI:

```bash
dotnet test tests/ProdutosAPI.Tests/ProdutosAPI.Tests.csproj
```

Executar testes de Pedidos:

```bash
dotnet test tests/Pedidos.Tests/Pedidos.Tests.csproj
```

Executar testes de Pix MockServer:

```bash
dotnet test tests/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj
```

Executar todos os testes da solucao:

```bash
dotnet test ProdutosAPI.slnx
```
