# Auth.Host

Microserviço de autenticação JWT para a solution.

- Vertical Slice Architecture
- Endpoint principal: POST /api/v1/auth/login
- Configuração de usuário/admin em appsettings.json
- Emite JWT compatível com Catalogo e Pedidos
- Health check: GET /health

## Como rodar

```bash
dotnet run --project src/Auth/Auth.Host/Auth.Host.csproj
```

Swagger disponível em http://localhost:5000 (ou porta configurada)

## Como configurar login e senha

O endpoint `POST /api/v1/auth/login` valida as credenciais usando estas chaves de configuração:

- `Auth:AdminEmail`
- `Auth:AdminPassword`

Hoje elas são lidas em `LoginEndpoint` via `IConfiguration`.

### Opção 1: appsettings.json (mais simples para desenvolvimento)

Arquivo: `src/Auth/Auth.Host/appsettings.json`

```json
{
    "Auth": {
        "AdminEmail": "admin@example.com",
        "AdminPassword": "senha123"
    }
}
```

### Opção 2: variável de ambiente (recomendado fora de dev)

No .NET, `:` vira `__` em variável de ambiente:

- `Auth__AdminEmail`
- `Auth__AdminPassword`

Exemplo PowerShell:

```powershell
$env:Auth__AdminEmail = "admin@example.com"
$env:Auth__AdminPassword = "senha123"
dotnet run --project src/Auth/Auth.Host/Auth.Host.csproj
```

## Como testar no Swagger

1. Rode o Auth.Host em Development.
2. Abra o Swagger na URL mostrada no console (ex.: `http://localhost:5020/swagger`).
3. Execute `POST /api/v1/auth/login` com:

```json
{
    "email": "admin@example.com",
    "senha": "senha123"
}
```

4. Se as credenciais baterem com a configuração, a API retorna `200` com JWT.
