# Prompt 04 — Transactions (create/list com filtros) com isolamento por tenant

Você é um assistente de programação. Os prompts anteriores já foram implementados:
- **Prompt 01 (Foundation):** solution estruturada, EF Core + PostgreSQL, entidades `Tenant` e `TenantUser`, `TenantResolutionMiddleware`, policy `MustBelongToTenant`, JWT configurado, endpoints `/api/health` e `/api/workspaces/{workspaceSlug}/_ping`.
- **Prompt 02 (Auth):** `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`, `GET /api/me` — com criação automática de workspace e vínculo `TenantUser`.
- **Prompt 03 (Accounts e Categories):** `POST` e `GET` para `/api/workspaces/{slug}/accounts` e `/api/workspaces/{slug}/categories`, com isolamento por tenant via EF Core Global Query Filters.

A solution segue a estrutura:
```
src/
  Api/                                          ← Host ASP.NET Core (Controllers, middleware, DI)
  BuildingBlocks/                               ← Result<T>, erros, abstrações compartilhadas
  Modules/
    Identity/{Domain,Application,Infrastructure}
    Tenancy/{Domain,Application,Infrastructure}
    Accounts/{Domain,Application,Infrastructure}
    Categories/{Domain,Application,Infrastructure}
    Transactions/{Domain,Application,Infrastructure}   ← NOVO
tests/
  StaqFinance.Api.IntegrationTests/
```

Contexto do MVP (reforço):
- Controllers com Clean Architecture por módulo (Domain / Application / Infrastructure).
- Isolamento multiusuário: toda entidade tenant-scoped possui `TenantId (Guid)` e usa **EF Core Global Query Filters**.
- O `TenantId` corrente é obtido via `ICurrentTenant` (preenchido pelo `TenantResolutionMiddleware`).
- Valores monetários em **centavos inteiros** (`AmountCents: long`). O sinal (entrada/saída) vem do `Type`; o valor permanece positivo.
- `Type`: `1 = Income`, `2 = Expense` (smallint no banco, enum no domínio).
- `CategoryId` é **opcional** (permite lançamentos "Sem categoria").
- Validações com o padrão já adotado no projeto (FluentValidation ou DataAnnotations — verifique o que existe).

---

## Tarefa (quarta entrega: Transactions)

### 1. Analise o estado atual do repositório

Antes de escrever qualquer código:
- Leia os projetos existentes em `src/Modules/Accounts` e `src/Modules/Categories` para entender o padrão adotado (estrutura de pastas, nomenclatura, DI, configurações EF).
- Leia `src/Api/Persistence/AppDbContext.cs` para entender como os `DbSet` e os Global Query Filters estão registrados.
- Leia `src/Api/Program.cs` para entender como os módulos são registrados no DI.
- Leia `tests/StaqFinance.Api.IntegrationTests/WorkspaceTestBase.cs` e `IntegrationTestCollection.cs` para manter o mesmo padrão de testes.

Liste as decisões de design que você vai tomar (nomes de classes, estrutura de pastas, FK behavior, etc.) **antes de começar a escrever código**.

---

### 2. Modelo de domínio

#### Entidade `Transaction`

| Campo          | Tipo              | Restrições                                      |
|----------------|-------------------|-------------------------------------------------|
| `Id`           | `Guid`            | PK, gerado pela aplicação                       |
| `TenantId`     | `Guid`            | FK implícita (Global Query Filter), index       |
| `AccountId`    | `Guid`            | FK → `Accounts.Id`, obrigatório                 |
| `CategoryId`   | `Guid?`           | FK → `Categories.Id`, **nullable**              |
| `Date`         | `DateOnly`        | Obrigatório, index                              |
| `Description`  | `string`          | Obrigatório, máx 255 chars                      |
| `Type`         | `TransactionType` | Enum: `Income = 1`, `Expense = 2`               |
| `AmountCents`  | `long`            | Obrigatório, > 0                                |
| `CreatedAt`    | `DateTime`        | UTC, definido na criação                        |

- Crie o enum `TransactionType` no projeto Domain.
- Crie o factory `Transaction.Create(Guid tenantId, Guid accountId, Guid? categoryId, DateOnly date, string description, TransactionType type, long amountCents)`.
- Não adicione lógica de recorrência aqui (isso é o próximo prompt).

#### Regras de integridade

- `AccountId` deve pertencer ao mesmo tenant (`TenantId`). Valide antes de persistir — se não existir, retorne `Error.NotFound("Account.NotFound", "Conta não encontrada.")`.
- `CategoryId`, quando informado, deve pertencer ao mesmo tenant. Se não existir, retorne `Error.NotFound("Category.NotFound", "Categoria não encontrada.")`.
- `AmountCents` deve ser > 0.

---

### 3. Endpoints a implementar

#### `POST /api/workspaces/{workspaceSlug}/transactions`

- **Autenticação/autorização:** `[Authorize(Policy = "MustBelongToTenant")]`
- **Request body:**

```json
{
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "categoryId": "7ba85f64-5717-4562-b3fc-2c963f66afa7",  // opcional
  "date": "2025-07-10",
  "description": "Supermercado",
  "type": 2,
  "amountCents": 15000
}
```

- **Validações (antes de chegar ao handler):**
  - `accountId`: obrigatório, não pode ser `Guid.Empty`.
  - `date`: obrigatório.
  - `description`: obrigatório, máx 255 chars.
  - `type`: obrigatório, deve ser `1` ou `2`.
  - `amountCents`: obrigatório, deve ser > 0.

- **Response `201 Created`:**

```json
{
  "id": "...",
  "accountId": "...",
  "categoryId": "...",
  "date": "2025-07-10",
  "description": "Supermercado",
  "type": 2,
  "amountCents": 15000,
  "createdAt": "2025-07-10T18:00:00Z"
}
```

- **Erros possíveis:**

| Status | Código de erro              | Quando                                |
|--------|-----------------------------|---------------------------------------|
| `400`  | (validação)                 | Body inválido                         |
| `401`  | —                           | Não autenticado                       |
| `403`  | —                           | Usuário não é membro do workspace     |
| `404`  | `Account.NotFound`          | `accountId` não existe no tenant      |
| `404`  | `Category.NotFound`         | `categoryId` não existe no tenant     |

---

#### `GET /api/workspaces/{workspaceSlug}/transactions`

- **Autenticação/autorização:** `[Authorize(Policy = "MustBelongToTenant")]`
- **Query string (todos opcionais):**

| Parâmetro    | Tipo       | Descrição                                  |
|--------------|------------|--------------------------------------------|
| `from`       | `DateOnly` | Filtro de data inicial (inclusivo)         |
| `to`         | `DateOnly` | Filtro de data final (inclusivo)           |
| `accountId`  | `Guid`     | Filtrar por conta                          |
| `categoryId` | `Guid`     | Filtrar por categoria                      |
| `type`       | `int`      | Filtrar por tipo (`1` = Income, `2` = Expense) |

- **Ordenação padrão:** `Date DESC`, depois `CreatedAt DESC`.
- **Response `200 OK`:**

```json
[
  {
    "id": "...",
    "accountId": "...",
    "categoryId": null,
    "date": "2025-07-10",
    "description": "Supermercado",
    "type": 2,
    "amountCents": 15000,
    "createdAt": "2025-07-10T18:00:00Z"
  }
]
```

- **Erros possíveis:**

| Status | Quando                                  |
|--------|-----------------------------------------|
| `401`  | Não autenticado                         |
| `403`  | Usuário não é membro do workspace       |

---

### 4. Estrutura de arquivos esperada (seguindo o padrão dos módulos anteriores)

```
src/Modules/Transactions/
  Domain/
    Entities/Transaction.cs
    Enums/TransactionType.cs
  Application/
    Commands/CreateTransactionCommand.cs   ← record + interface ICreateTransactionCommandHandler
    Queries/ListTransactionsQuery.cs       ← record + interface IListTransactionsQueryHandler
    DTOs/TransactionResponse.cs
    DTOs/ListTransactionsFilter.cs
    DI/TransactionsApplicationModule.cs
  Infrastructure/
    Handlers/CreateTransactionCommandHandler.cs
    Handlers/ListTransactionsQueryHandler.cs
    Persistence/Configurations/TransactionConfiguration.cs
    DI/TransactionsInfrastructureModule.cs

src/Api/
  Controllers/TransactionsController.cs
```

---

### 5. Configuração EF Core

- Tabela: `Transactions`
- `Id`: `ValueGeneratedNever()`
- `TenantId`: required, index
- `AccountId`: required, FK, `OnDelete(DeleteBehavior.Restrict)` — não excluir transações em cascata
- `CategoryId`: nullable, FK, `OnDelete(DeleteBehavior.SetNull)`
- `Date`: required, column type `date`, index
- `Description`: required, max length 255
- `Type`: required, `HasConversion<int>()`
- `AmountCents`: required
- `CreatedAt`: required

Registre `TransactionConfiguration` em `AppDbContext.OnModelCreating` e adicione o **Global Query Filter** para `Transaction` seguindo o mesmo padrão de `Account` e `Category`:
```csharp
modelBuilder.Entity<Transaction>().HasQueryFilter(t =>
    !_currentTenant.IsResolved || t.TenantId == _currentTenant.TenantId);
```

Adicione `DbSet<Transaction> Transactions` ao `AppDbContext`.

---

### 6. Registro no DI e no `Program.cs`

- Crie `TransactionsApplicationModule` e `TransactionsInfrastructureModule` com métodos de extensão `AddTransactionsModule()`.
- Registre os dois em `Program.cs` seguindo o mesmo padrão dos módulos anteriores.
- Adicione as referências de projeto necessárias em `src/Api/StaqFinance.Api.csproj`.
- Os projetos Infrastructure precisam de `<FrameworkReference Include="Microsoft.AspNetCore.App" />` e referência ao pacote `Microsoft.EntityFrameworkCore.Relational`.

---

### 7. Migration

Após implementar e buildar com sucesso, gere a migration:

```bash
dotnet ef migrations add AddTransactions --project src/Api/StaqFinance.Api.csproj
```

**Não aplique a migration agora** — o usuário fará isso manualmente.

---

### 8. Testes de integração

Crie `tests/StaqFinance.Api.IntegrationTests/Transactions/TransactionsTests.cs` com os seguintes cenários:

| # | Cenário                                                      | Esperado           |
|---|--------------------------------------------------------------|--------------------|
| 1 | `POST` sem token                                             | `401`              |
| 2 | `POST` com token de outro tenant                             | `403`              |
| 3 | `POST` com `accountId` inexistente                           | `404`              |
| 4 | `POST` com `categoryId` inexistente                          | `404`              |
| 5 | `POST` com `amountCents = 0`                                 | `400`              |
| 6 | `POST` válido sem categoria                                   | `201` + body correto |
| 7 | `POST` válido com categoria                                   | `201` + body correto |
| 8 | `GET` sem token                                              | `401`              |
| 9 | `GET` autenticado com lista vazia                             | `200 []`           |
| 10 | `GET` retorna apenas transações do tenant corrente           | `200` isolado      |
| 11 | `GET` com filtro `type=2` retorna apenas Expenses            | `200` filtrado     |
| 12 | `GET` com filtro `accountId` retorna apenas da conta filtrada | `200` filtrado    |
| 13 | `GET` com filtro `from` e `to` respeita o intervalo de datas | `200` filtrado    |

Use o mesmo `[Collection("Integration")]` e herde de `WorkspaceTestBase` para o setup de autenticação.

No helper `WorkspaceTestBase` (ou em um helper local), adicione um método `CreateAccountAsync(string token, string slug, string name)` e `CreateCategoryAsync(...)` para facilitar o setup dos testes de `Transactions`.

---

### 9. Build e testes

Ao finalizar:
1. Execute `dotnet build` e corrija quaisquer erros de compilação.
2. Execute `dotnet test tests/StaqFinance.Api.IntegrationTests/StaqFinance.Api.IntegrationTests.csproj` e garanta que todos os testes passem (incluindo os de Accounts e Categories).
