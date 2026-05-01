# Prompt 03 — Accounts e Categories (create/list) com isolamento por tenant

Você é um assistente de programação. Os prompts anteriores já foram implementados:
- **Prompt 01 (Foundation):** solution estruturada, EF Core + PostgreSQL, entidades `Tenant` e `TenantUser`, `TenantResolutionMiddleware`, policy `MustBelongToTenant`, JWT configurado, endpoints `/api/health` e `/api/workspaces/{workspaceSlug}/_ping`.
- **Prompt 02 (Auth):** `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`, `GET /api/me` — com criação automática de workspace e vínculo `TenantUser`.

A solution segue a estrutura:
```
src/
  Api/                                          ← Host ASP.NET Core (Controllers, middleware, DI)
  BuildingBlocks/                               ← Result<T>, erros, abstrações compartilhadas
  Modules/
    Identity/{Domain,Application,Infrastructure}
    Tenancy/{Domain,Application,Infrastructure}
tests/
  StaqFinance.Api.IntegrationTests/
```

Contexto do MVP (reforço):
- Controllers com Clean Architecture por módulo (Domain / Application / Infrastructure).
- Isolamento multiusuário: toda entidade tenant-scoped possui `TenantId (Guid)` e usa **EF Core Global Query Filters** para garantir isolamento.
- O `TenantId` corrente é obtido via `ICurrentTenant` (preenchido pelo `TenantResolutionMiddleware`).
- Valores monetários em centavos (`AmountCents: long`), moeda única `BRL` — mas `Accounts` e `Categories` **não** possuem valores, apenas `Transactions`.
- Validações com o padrão já adotado no projeto (FluentValidation ou DataAnnotations — verifique o que existe).

---

## Tarefa (terceira entrega: Accounts e Categories)

### 1. Analise o estado atual do repositório

Antes de criar qualquer arquivo, leia o que já existe em `src/Modules/`, `src/Api/Controllers/` e `src/Api/` para entender convenções adotadas.

---

### 2. Crie o módulo `Accounts`

**Estrutura esperada:**
```
src/Modules/Accounts/
  Domain/
    Account.cs
  Application/
    Commands/
      CreateAccountCommand.cs
      CreateAccountCommandHandler.cs
    Queries/
      ListAccountsQuery.cs
      ListAccountsQueryHandler.cs
    DTOs/
      AccountResponse.cs
  Infrastructure/
    AccountRepository.cs          ← (ou via DbContext diretamente no handler, decida e justifique)
    AccountsModuleExtensions.cs   ← registro de DI do módulo
```

**Entidade `Account`** (conforme blueprint):
| Campo       | Tipo     | Regras                              |
|-------------|----------|-------------------------------------|
| `Id`        | `Guid`   | PK, gerado na criação               |
| `TenantId`  | `Guid`   | FK, index, não nulo                 |
| `Name`      | `string` | não nulo, 1–100 chars               |
| `CreatedAt` | `datetime UTC` | definido na criação           |

**Endpoints:**

#### `POST /api/workspaces/{workspaceSlug}/accounts`
- Requer `Authorization: Bearer {token}` + policy `MustBelongToTenant`.
- Body:
  ```json
  { "name": "string" }
  ```
- Validações:
  - `name`: obrigatório, 1–100 chars.
  - Não permitir duplicata de nome dentro do mesmo tenant (retornar `409 Conflict`).
- Resposta `201 Created`:
  ```json
  {
    "id": "guid",
    "name": "string",
    "createdAt": "datetime"
  }
  ```
- Erros esperados:
  - `400` — validação.
  - `401` / `403` — autenticação/autorização.
  - `409` — conta com mesmo nome já existe no workspace.

#### `GET /api/workspaces/{workspaceSlug}/accounts`
- Requer `Authorization: Bearer {token}` + policy `MustBelongToTenant`.
- Retorna todas as contas do workspace (sem paginação no MVP).
- Resposta `200 OK`:
  ```json
  [
    { "id": "guid", "name": "string", "createdAt": "datetime" }
  ]
  ```

---

### 3. Crie o módulo `Categories`

**Estrutura esperada:**
```
src/Modules/Categories/
  Domain/
    Category.cs
  Application/
    Commands/
      CreateCategoryCommand.cs
      CreateCategoryCommandHandler.cs
    Queries/
      ListCategoriesQuery.cs
      ListCategoriesQueryHandler.cs
    DTOs/
      CategoryResponse.cs
  Infrastructure/
    CategoryRepository.cs         ← (ou via DbContext diretamente, mesma decisão de Accounts)
    CategoriesModuleExtensions.cs
```

**Entidade `Category`** (conforme blueprint):
| Campo       | Tipo     | Regras                              |
|-------------|----------|-------------------------------------|
| `Id`        | `Guid`   | PK, gerado na criação               |
| `TenantId`  | `Guid`   | FK, index, não nulo                 |
| `Name`      | `string` | não nulo, 1–100 chars               |
| `CreatedAt` | `datetime UTC` | definido na criação           |

**Endpoints:**

#### `POST /api/workspaces/{workspaceSlug}/categories`
- Requer `Authorization: Bearer {token}` + policy `MustBelongToTenant`.
- Body:
  ```json
  { "name": "string" }
  ```
- Validações:
  - `name`: obrigatório, 1–100 chars.
  - Não permitir duplicata de nome dentro do mesmo tenant (retornar `409 Conflict`).
- Resposta `201 Created`:
  ```json
  {
    "id": "guid",
    "name": "string",
    "createdAt": "datetime"
  }
  ```
- Erros esperados:
  - `400` — validação.
  - `401` / `403` — autenticação/autorização.
  - `409` — categoria com mesmo nome já existe no workspace.

#### `GET /api/workspaces/{workspaceSlug}/categories`
- Requer `Authorization: Bearer {token}` + policy `MustBelongToTenant`.
- Retorna todas as categorias do workspace (sem paginação no MVP).
- Resposta `200 OK`:
  ```json
  [
    { "id": "guid", "name": "string", "createdAt": "datetime" }
  ]
  ```

---

### 4. Configure EF Core para os novos módulos

- Adicione `DbSet<Account>` e `DbSet<Category>` ao `AppDbContext` existente (ou explique se a abordagem for diferente).
- Configure via `IEntityTypeConfiguration<T>`:
  - Índice em `TenantId` para ambas as entidades.
  - **Global Query Filter** filtrando por `TenantId` corrente (usando `ICurrentTenant`).
- Gere e aplique uma nova migration.

---

### 5. Registre os módulos na DI

- Chame `AddAccountsModule(...)` e `AddCategoriesModule(...)` (ou equivalente) em `Program.cs`.
- Os `Controllers` de `Accounts` e `Categories` devem estar em `src/Api/Controllers/`.

---

### 6. Escreva testes de integração

Siga o padrão já adotado em `tests/StaqFinance.Api.IntegrationTests/`.

Cenários mínimos para **cada módulo** (Accounts e Categories):

| Cenário | Resultado esperado |
|---|---|
| `POST` sem token | `401` |
| `POST` com token de outro workspace | `403` |
| `POST` com workspace inexistente | `404` |
| `POST` com `name` vazio | `400` |
| `POST` válido | `201` com body correto |
| `POST` com nome duplicado no mesmo tenant | `409` |
| `GET` sem token | `401` |
| `GET` válido (lista vazia) | `200` com `[]` |
| `GET` válido após criar itens | `200` com itens do tenant (sem vazar dados de outros tenants) |

---

## Regras importantes

- **NÃO** implemente `Transactions` ou `RecurringTransactions` ainda — isso é o Prompt 04 e 05.
- Garanta que o **Global Query Filter** está ativo: um tenant jamais deve enxergar dados de outro.
- Use `ICurrentTenant.TenantId` para escrita (criar entidade) e o filtro global para leitura.
- Siga rigorosamente as convenções de nomes, estrutura de pastas e padrões de DI já estabelecidos nos módulos `Identity` e `Tenancy`.
- Antes de criar qualquer arquivo, liste as decisões que você vai tomar (repositório vs handler direto, escopo dos projetos `.csproj`, etc.) e espere confirmação.
