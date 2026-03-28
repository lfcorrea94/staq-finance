# StaqFinance API — Documentação

> API de controle de gastos pessoais com suporte a múltiplos workspaces (multi-tenant).
>
> **Base URL (desenvolvimento):**
> - HTTP: `http://localhost:5176`
> - HTTPS: `https://localhost:7275`
>
> **Swagger UI:** `{baseUrl}/swagger`

---

## Sumário

- [Visão Geral da Arquitetura](#visão-geral-da-arquitetura)
- [Autenticação](#autenticação)
- [Headers Globais](#headers-globais)
- [Multi-tenancy (Workspaces)](#multi-tenancy-workspaces)
- [Endpoints](#endpoints)
  - [Health](#health)
  - [Auth](#auth)
  - [Me](#me)
  - [Workspace](#workspace)
- [Códigos de Resposta Padrão](#códigos-de-resposta-padrão)
- [Modelos de Dados](#modelos-de-dados)

---

## Visão Geral da Arquitetura

```
StaqFinance.Api                      ← Projeto principal (controllers, middleware, DI)
├── Modules/Identity/Domain          ← Entidades ApplicationUser, RefreshToken
├── Modules/Identity/Application     ← Commands, Queries, DTOs, interfaces de serviço
├── Modules/Identity/Infrastructure  ← JWT, SlugService, TokenService, handlers
├── Modules/Tenancy/Domain           ← Entidades Tenant, TenantUser + contratos
├── Modules/Tenancy/Application      ← Interface ICurrentTenant
└── Modules/Tenancy/Infrastructure   ← TenantRepository, CurrentTenant
```

**Stack:**
- Runtime: .NET 10 / ASP.NET Core
- Banco de dados: PostgreSQL via Entity Framework Core
- Autenticação: JWT Bearer (ASP.NET Core Identity)
- Logs: Serilog (Console + structured logging)

---

## Autenticação

A API utiliza **JWT Bearer Token**. Todos os endpoints protegidos exigem o header:

```
Authorization: Bearer {seu_token_jwt}
```

### Parâmetros JWT (configurados em `appsettings.json`)

| Parâmetro            | Valor padrão                 | Descrição                            |
|----------------------|------------------------------|--------------------------------------|
| `Jwt:Issuer`         | `staqfinance-api`            | Emissor do token                     |
| `Jwt:Audience`       | `staqfinance-client`         | Audiência esperada do token          |
| `Jwt:ExpiresInMinutes` | `60`                       | Tempo de expiração do access token   |
| `Jwt:RefreshExpiresInDays` | `7`                   | Tempo de expiração do refresh token  |
| `Jwt:Key`            | *(mínimo 32 caracteres)*     | Chave secreta de assinatura HMAC     |

> ⚠️ **Importante:** A `Jwt:Key` padrão **deve ser substituída** antes de ir para produção.

---

## Headers Globais

### `X-Correlation-Id` *(opcional)*

Presente em **todas** as requisições e respostas. Utilizado para rastreabilidade de logs.

| Direção  | Comportamento                                                                 |
|----------|-------------------------------------------------------------------------------|
| Request  | Se fornecido, o valor é propagado pela request e nos logs                     |
| Response | Sempre retornado. Se não enviado na request, um novo UUID é gerado automaticamente |

**Exemplo:**
```
X-Correlation-Id: 3fa85f64-5717-4562-b3fc-2c963f66afa6
```

---

## Multi-tenancy (Workspaces)

Todos os endpoints de negócio são **prefixados com o slug do workspace** na rota:

```
/api/workspaces/{workspaceSlug}/...
```

### Fluxo de resolução do tenant

1. O middleware `TenantResolutionMiddleware` extrai `{workspaceSlug}` da rota.
2. Consulta o banco de dados por um `Tenant` com aquele slug.
3. Se não existir → retorna `404 Not Found`.
4. Se existir → popula `ICurrentTenant` com `TenantId` e `WorkspaceSlug` para uso na request.
5. A policy `MustBelongToTenant` verifica se o usuário autenticado (via JWT) é membro daquele tenant.
   - Se não for membro → retorna `403 Forbidden`.

### Modelo de associação

Um usuário pode pertencer a múltiplos workspaces. Cada associação possui uma `Role`:

| Role    | Descrição              |
|---------|------------------------|
| `Owner` | Dono do workspace (padrão ao criar) |

---

## Endpoints

### Health

#### `GET /api/health`

Verifica se a API está no ar. **Não requer autenticação.**

**Request:**
```http
GET /api/health
```

**Response `200 OK`:**
```json
{
  "status": "healthy",
  "timestamp": "2026-03-21T12:00:00Z"
}
```

---

#### `GET /api/health/db`

Verifica a conectividade com o banco de dados via EF Core. **Não requer autenticação.**

**Request:**
```http
GET /api/health/db
```

**Response `200 OK`** *(banco acessível)*:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "AppDbContext": {
      "status": "Healthy",
      "duration": "00:00:00.0100000"
    }
  }
}
```

**Response `503 Service Unavailable`** *(banco inacessível)*:
```json
{
  "status": "Unhealthy",
  "totalDuration": "...",
  "entries": {
    "AppDbContext": {
      "status": "Unhealthy",
      "description": "..."
    }
  }
}
```

---

### Auth

> Endpoints públicos — **não requerem** `Authorization` header.

---

#### `POST /api/auth/register`

Cria um novo usuário, gera um slug para o workspace e cria o vínculo `TenantUser` com role `Owner`. Tudo em uma única transação atômica.

**Request:**
```http
POST /api/auth/register
Content-Type: application/json
```
```json
{
  "email": "user@example.com",
  "password": "Abc12345",
  "displayName": "João Silva",
  "workspaceName": "Meu Orçamento"
}
```

| Campo           | Tipo     | Validação                                                           |
|-----------------|----------|---------------------------------------------------------------------|
| `email`         | `string` | Obrigatório, formato de e-mail válido, máx 254 chars               |
| `password`      | `string` | Obrigatório, mín 8 chars, ao menos 1 maiúscula, 1 minúscula, 1 número |
| `displayName`   | `string` | Obrigatório, 1–100 chars                                           |
| `workspaceName` | `string` | Obrigatório, 2–80 chars (slug gerado deve ter 3–40 chars)          |

**Response `201 Created`:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "displayName": "João Silva",
  "workspace": {
    "name": "Meu Orçamento",
    "slug": "meu-orcamento",
    "currency": "BRL"
  }
}
```

**Respostas de erro:**

| Status | Quando ocorre                          |
|--------|----------------------------------------|
| `400`  | Campos inválidos ou slug inválido      |
| `409`  | E-mail já cadastrado                   |

---

#### `POST /api/auth/login`

Autentica o usuário e retorna um access token JWT e um refresh token.

**Request:**
```http
POST /api/auth/login
Content-Type: application/json
```
```json
{
  "email": "user@example.com",
  "password": "Abc12345"
}
```

**Response `200 OK`:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

| Campo          | Tipo     | Descrição                                  |
|----------------|----------|--------------------------------------------|
| `accessToken`  | `string` | JWT Bearer token                           |
| `expiresIn`    | `int`    | Validade do access token em segundos       |
| `refreshToken` | `string` | Token opaco para rotação (7 dias)          |

**Respostas de erro:**

| Status | Quando ocorre                                   |
|--------|-------------------------------------------------|
| `400`  | Body inválido                                   |
| `401`  | Credenciais incorretas (mensagem genérica)      |

---

#### `POST /api/auth/refresh`

Valida o refresh token, o revoga (rotação obrigatória) e emite um novo par de tokens.

**Request:**
```http
POST /api/auth/refresh
Content-Type: application/json
```
```json
{
  "token": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

**Response `200 OK`:** mesma estrutura de `/login`.

**Respostas de erro:**

| Status | Quando ocorre                                      |
|--------|----------------------------------------------------|
| `400`  | Body inválido                                      |
| `401`  | Refresh token inválido, expirado ou já utilizado   |

---

### Me

> Requer `Authorization: Bearer {token}`. **Não** passa pelo `TenantResolutionMiddleware`.

---

#### `GET /api/me`

Retorna os dados do usuário autenticado e seu workspace único.

**Request:**
```http
GET /api/me
Authorization: Bearer {accessToken}
```

**Response `200 OK`:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "displayName": "João Silva",
  "createdAt": "2026-03-21T12:00:00Z",
  "workspace": {
    "name": "Meu Orçamento",
    "slug": "meu-orcamento",
    "currency": "BRL"
  }
}
```

**Respostas de erro:**

| Status | Quando ocorre                              |
|--------|--------------------------------------------|
| `401`  | Token ausente ou inválido                  |
| `404`  | Usuário ou workspace não encontrado no banco |

---

### Workspace

> Todos os endpoints abaixo exigem:
> - Header `Authorization: Bearer {token}`
> - Que o usuário autenticado seja membro do workspace informado na rota.

---

#### `GET /api/workspaces/{workspaceSlug}/_ping`

Valida que o token JWT é válido **e** que o usuário pertence ao workspace informado. Útil para testar autenticação e autorização end-to-end.

**Request:**
```http
GET /api/workspaces/meu-workspace/_ping
Authorization: Bearer {token}
```

| Parâmetro de rota | Tipo     | Obrigatório | Descrição                         |
|-------------------|----------|-------------|-----------------------------------|
| `workspaceSlug`   | `string` | ✅           | Slug único que identifica o workspace |

**Response `200 OK`:**
```json
{
  "status": "ok",
  "workspaceSlug": "meu-workspace",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp": "2026-03-21T12:00:00Z"
}
```

| Campo           | Tipo       | Descrição                          |
|-----------------|------------|------------------------------------|
| `status`        | `string`   | Sempre `"ok"` quando bem-sucedido  |
| `workspaceSlug` | `string`   | Slug do workspace resolvido        |
| `tenantId`      | `guid`     | ID interno do tenant no banco      |
| `timestamp`     | `datetime` | Momento UTC da resposta            |

**Respostas de erro:**

| Status | Quando ocorre                                                  |
|--------|----------------------------------------------------------------|
| `401`  | Token ausente, inválido ou expirado                           |
| `403`  | Token válido, mas o usuário não é membro do workspace         |
| `404`  | O `workspaceSlug` informado não existe no banco de dados      |

---

## Códigos de Resposta Padrão

| Status | Significado                                                             |
|--------|-------------------------------------------------------------------------|
| `200`  | Sucesso                                                                 |
| `401`  | Não autenticado — token ausente ou inválido                            |
| `403`  | Não autorizado — usuário autenticado não tem acesso ao recurso         |
| `404`  | Recurso não encontrado (ex: workspace com o slug informado não existe) |
| `503`  | Serviço indisponível (ex: banco de dados inacessível)                  |

**Formato do erro `404` de workspace:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Workspace not found.",
  "status": 404,
  "detail": "Workspace 'meu-workspace' does not exist."
}
```

---

## Modelos de Dados

### `ApplicationUser`

Herda de `IdentityUser<Guid>`. Representa um usuário da aplicação.

| Campo         | Tipo       | Descrição                          |
|---------------|------------|------------------------------------|
| `Id`          | `Guid`     | Identificador único                |
| `Email`       | `string`   | E-mail único (obrigatório)         |
| `UserName`    | `string`   | Username (gerido pelo Identity)    |
| `DisplayName` | `string`   | Nome de exibição do usuário        |
| `CreatedAt`   | `datetime` | Data de criação (UTC)              |

### `Tenant` (Workspace)

| Campo       | Tipo       | Descrição                            |
|-------------|------------|--------------------------------------|
| `Id`        | `Guid`     | Identificador único                  |
| `Name`      | `string`   | Nome do workspace                    |
| `Slug`      | `string`   | Identificador de URL (único)         |
| `Currency`  | `string`   | Moeda padrão (ex: `"BRL"`)          |
| `CreatedAt` | `datetime` | Data de criação (UTC)                |

### `TenantUser` (Associação Usuário ↔ Workspace)

| Campo      | Tipo       | Descrição                            |
|------------|------------|--------------------------------------|
| `TenantId` | `Guid`     | Referência ao Tenant                 |
| `UserId`   | `Guid`     | Referência ao ApplicationUser        |
| `Role`     | `string`   | Papel no workspace (padrão: `Owner`) |
| `JoinedAt` | `datetime` | Data de entrada no workspace (UTC)   |

### `RefreshToken`

Armazenado na tabela `RefreshTokens`. Cada token é de uso único (rotação obrigatória).

| Campo       | Tipo       | Descrição                                         |
|-------------|------------|---------------------------------------------------|
| `Id`        | `Guid`     | Identificador único                               |
| `UserId`    | `Guid`     | Referência ao `ApplicationUser`                   |
| `Token`     | `string`   | Valor opaco (Base64 URL-safe, 64 bytes aleatórios)|
| `ExpiresAt` | `datetime` | Data de expiração (UTC, padrão: 7 dias)           |
| `IsRevoked` | `bool`     | Indica se o token foi revogado                    |
| `CreatedAt` | `datetime` | Data de criação (UTC)                             |
