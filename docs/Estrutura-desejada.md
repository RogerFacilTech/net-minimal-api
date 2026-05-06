## Estrutura de Pastas

```text
src/
  Shared/
    Common/                      -> Contratos e utilitarios transversais
    Data/                        -> Persistencia compartilhada
  Catalogo/                      -> Código relacionado à API de catálogo de produtos
    Catalogo.Application/        -> Casos de uso do Catalogo
    Catalogo.Common/             -> Código comum na API de Catalogo
    Catalogo.Domain/             -> Dominio do Catalogo
    Catalogo.Endpoints/          -> Endpoints, DTOs, extensoes HTTP
    Catalogo.Host/               -> Host da API de catálogo de produtos
    Catalogo.Infrastructure/     -> EF Core, Dapper, DbSeeder
  Pedidos/                       -> Código relacionado à API de catálogo de produtos
    Pedidos.Application/         ->
    Pedidos.Common/              -> Código comum na API de Pedidos
    Pedidos.Domain/              ->
    Pedidos.Endpoints/           -> Endpoints, DTOs, extensoes HTTP
    Pedidos.Host/                -> Host da API de Pedidos
    Pedidos.Infrastructure/      ->
samples/
  Catalogo.HttpClientDemo/       -> Demo de resiliencia HTTP do Catalogo
  Pix/
    Pix.MockServer/              -> Servidor mock Pix (mTLS + OAuth2)
    Pix.ClientDemo/              -> Console app cliente Pix
tests/
```
