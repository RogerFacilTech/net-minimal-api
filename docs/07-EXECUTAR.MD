# EXECUTAR

Comandos para executar os projetos executaveis e os projetos de teste.

## Executaveis

Rodar o host principal do Catalogo:

```bash
dotnet run --project src/Catalogo/Catalogo.API/Catalogo.API.csproj
```

Rodar o host de Auth (emissor JWT):

```bash
dotnet run --project src/Auth/Auth.Host/Auth.Host.csproj
```

Rodar o host principal de Pedidos:

```bash
dotnet run --project src/Pedidos/Pedidos.API/Pedidos.API.csproj
```

Rodar o mock server Pix:

```bash
dotnet run --project samples/Pix/Pix.MockServer/Pix.MockServer.csproj
```

Rodar o cliente demo do Catalogo:

```bash
dotnet run --project samples/Catalogo.HttpClientDemo/Catalogo.HttpClientDemo.csproj
dotnet run --project samples/Catalogo.HttpClientDemo/Catalogo.HttpClientDemo.csproj "http://localhost:5000"
```

Rodar o cliente demo do Pix:

```bash
dotnet run --project samples/Pix/Pix.ClientDemo/Pix.ClientDemo.csproj
```

## Testes

Executar testes de Catalogo:

```bash
dotnet test src/Catalogo/Catalogo.Tests/Catalogo.Tests.csproj
```

Executar testes de Pedidos:

```bash
dotnet test src/Pedidos/Pedidos.Tests/Pedidos.Tests.csproj
```

Executar testes de Pix MockServer:

```bash
dotnet test samples/Pix/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj
```

Executar todos os testes da solucao:

```bash
dotnet test FacShop.slnx
```
