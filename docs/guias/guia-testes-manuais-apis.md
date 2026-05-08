# Guia de Testes Manuais — Endpoints da Solution

Este guia cobre todos os endpoints dos contextos Auth, Catálogo, Pedidos e Pix, com exemplos de payloads e instruções para testes via Postman ou curl.

---

## 1. Auth

### 1.1. Login

- **POST** `/api/v1/auth/login`
- **Body:**

```json
{
    "email": "admin@example.com",
    "senha": "senha123"
}
```

- **Resposta:** 200 OK com `{ token, expiresIn }` ou 401 Unauthorized
- **Uso:** Obtenha o token JWT para endpoints protegidos

### 1.2. Health

- **GET** `/health`
- **Uso:** Verifica se o serviço está online

---

## 2. Catálogo

### 2.1. Produtos

- **GET** `/api/v1/catalogo/produtos` — Lista produtos (query: `page`, `pageSize`, `categoria`, `search`)
- **GET** `/api/v1/catalogo/produtos/{id}` — Detalhe do produto
- **POST** `/api/v1/catalogo/produtos` — Cria produto (protegido)

```json
{
    "nome": "Produto Teste",
    "descricao": "Descrição...",
    "preco": 99.9,
    "categoriaId": 1,
    "estoque": 10
}
```

- **PUT** `/api/v1/catalogo/produtos/{id}` — Atualiza produto (protegido)
- **PATCH** `/api/v1/catalogo/produtos/{id}` — Atualiza parcialmente (protegido)
- **DELETE** `/api/v1/catalogo/produtos/{id}` — Soft delete (protegido)

### 2.2. Categorias

- **GET** `/api/v1/catalogo/categorias` — Lista categorias
- **GET** `/api/v1/catalogo/categorias/{id}` — Detalhe
- **POST** `/api/v1/catalogo/categorias` — Cria categoria (protegido)

```json
{
    "nome": "Eletrônicos",
    "categoriaPaiId": null
}
```

- **PUT** `/api/v1/catalogo/categorias/{id}` — Renomeia (protegido)
- **DELETE** `/api/v1/catalogo/categorias/{id}` — Desativa (protegido)

### 2.3. Variantes

- **GET** `/api/v1/catalogo/variantes?produtoId={id}` — Lista variantes de um produto
- **GET** `/api/v1/catalogo/variantes/{id}` — Detalhe
- **POST** `/api/v1/catalogo/variantes` — Cria variante (protegido)

```json
{
    "produtoId": 1,
    "sku": "SKU-001",
    "descricao": "Azul, P",
    "precoAdicional": 10.0,
    "estoque": 5
}
```

- **PUT** `/api/v1/catalogo/variantes/{id}` — Atualiza preço (protegido)
- **PATCH** `/api/v1/catalogo/variantes/{id}/estoque` — Atualiza estoque (protegido)
- **DELETE** `/api/v1/catalogo/variantes/{id}` — Desativa (protegido)

### 2.4. Atributos

- **GET** `/api/v1/catalogo/atributos?produtoId={id}`
- **POST** `/api/v1/catalogo/atributos` (protegido)

```json
{
    "produtoId": 1,
    "chave": "Cor",
    "valor": "Azul"
}
```

- **PUT** `/api/v1/catalogo/atributos/{id}` (protegido)
- **DELETE** `/api/v1/catalogo/atributos/{id}` (protegido)

### 2.5. Mídias

- **GET** `/api/v1/catalogo/midias?produtoId={id}`
- **POST** `/api/v1/catalogo/midias` (protegido)

```json
{
    "produtoId": 1,
    "url": "https://img.com/foto.jpg",
    "tipo": "Imagem",
    "ordem": 1
}
```

- **PATCH** `/api/v1/catalogo/midias/{id}/ordem` (protegido)
- **DELETE** `/api/v1/catalogo/midias/{id}` (protegido)

---

## 3. Pedidos

### 3.1. Listar pedidos

- **GET** `/api/v1/pedidos?page=1&pageSize=10` — Lista paginada

### 3.2. Obter pedido

- **GET** `/api/v1/pedidos/{id}`

### 3.3. Criar pedido

- **POST** `/api/v1/pedidos` (protegido)

```json
{
    "itens": [{ "produtoId": 1, "quantidade": 2 }]
}
```

### 3.4. Adicionar item

- **POST** `/api/v1/pedidos/{id}/itens` (protegido)

```json
{
    "produtoId": 2,
    "quantidade": 1
}
```

### 3.5. Cancelar pedido

- **POST** `/api/v1/pedidos/{id}/cancelar` (protegido)

```json
{
    "motivo": "Cliente desistiu"
}
```

---

## 4. Pix (MockServer)

### 4.1. Obter token OAuth2

- **POST** `/oauth/token`

```json
{
    "client_id": "pix-demo-client",
    "client_secret": "pix-demo-secret",
    "grant_type": "client_credentials"
}
```

- **Resposta:** `{ "access_token": "...", "token_type": "Bearer", ... }`

### 4.2. Criar cobrança

- **POST** `/pix/v1/cobrancas` (requer Bearer token + mTLS)

```json
{
    "calendario": { "expiracao": 3600 },
    "devedor": {
        "nome": "Cliente Exemplo",
        "cpf": "12345678901",
        "endereco": {
            "logradouro": "Rua A",
            "numero": "100",
            "cidade": "Sao Paulo",
            "uf": "SP",
            "cep": "01001000"
        }
    },
    "recebedor": {
        "nome": "Empresa",
        "agencia": "12345678",
        "conta": "0001",
        "banco": "99999",
        "tipoConta": "CACC"
    },
    "valor": { "original": 100.0 },
    "chave": "chave-pix-demo",
    "solicitacaoPagador": "Pedido #123"
}
```

### 4.3. Simular liquidação

- **POST** `/pix/v1/cobrancas/{txid}/simular-liquidacao` (token + mTLS)

### 4.4. Criar devolução

- **POST** `/pix/v1/devolucoes` (token + mTLS)

```json
{
    "txid": "...",
    "e2eid": "...",
    "valor": 10.0,
    "natureza": "ORIGINAL",
    "descricao": "Teste devolucao",
    "infoAdicionais": {}
}
```

### 4.5. Consultar devolução

- **GET** `/pix/v1/devolucoes/{devolucaoId}` (token + mTLS)

### 4.6. Health

- **GET** `/health`

---

## Observações

- Endpoints protegidos exigem header `Authorization: Bearer {token}`.
- No Pix, além do token, é necessário mTLS (certificado cliente).
- Teste respostas de erro enviando dados inválidos ou omitindo autenticação.

---

_Data: 08/05/2026_
_Autor: GitHub Copilot_
