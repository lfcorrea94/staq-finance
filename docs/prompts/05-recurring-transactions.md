# Prompt 05 — Recorrência (create/list) + endpoint `run` com idempotência

Você é um assistente de programação. Os prompts anteriores já foram implementados:
- **Prompt 01 (Foundation):** solution estruturada, EF Core + PostgreSQL, entidades `Tenant` e `TenantUser`, `TenantResolutionMiddleware`, policy `MustBelongToTenant`, JWT configurado, endpoints `/api/health` e `/api/workspaces/{workspaceSlug}/_ping`.
- **Prompt 02 (Auth):** `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`, `GET /api/me` — com criação automática de workspace e vínculo `TenantUser`.
- **Prompt 03 (Accounts e Categories):** `POST` e `GET` para `/api/workspaces/{slug}/accounts` e `/api/workspaces/{slug}/categories`, com isolamento por tenant via EF Core Global Query Filters.
- **Prompt 04 (Transactions):** `POST` e `GET` para `/api/workspaces/{slug}/transactions`, com filtros por período/conta/categoria/tipo, isolamento por tenant via Global Query Filters, enum `TransactionType` (`Income = 1`, `Expense = 2`), valores em centavos inteiros (`AmountCents: long`).

A solution segue a estrutura:
```
src/
  Api/                                                          ← Host ASP.NET Core (Controllers, middleware, DI)
  BuildingBlocks/                                               ← Result<T>, erros, abstrações compartilhadas
  Modules/
    Identity/{Domain,Application,Infrastructure}
    Tenancy/{Domain,Application,Infrastructure}
    Accounts/{Domain,Application,Infrastructure}
    Categories/{Domain,Application,Infrastructure}
    Transactions/{Domain,Application,Infrastructure}
    RecurringTransactions/{Domain,Application,Infrastructure}   ← NOVO
tests/
  StaqFinance.Api.IntegrationTests/
```

Contexto do MVP (reforço):
- Controllers com Clean Architecture por módulo (Domain / Application / Infrastructure).
- Isolamento multiusuário: toda entidade tenant-scoped possui `TenantId (Guid)` e usa **EF Core Global Query Filters**.
- O `TenantId` corrente é obtido via `ICurrentTenant` (preenchido pelo `TenantResolutionMiddleware`).
- Valores monetários em **centavos inteiros** (`AmountCents: long`). O sinal (entrada/saída) vem do `Type`; o valor permanece positivo.
- `Type`: `1 = Income`, `2 = Expense` — reutilize o enum `TransactionType` já existente no módulo `Transactions.Domain`.
- Validações com o padrão já adotado no projeto (FluentValidation ou DataAnnotations — verifique o que existe).

---

## Tarefa (quinta entrega: RecurringTransactions)

### 1. Analise o estado atual do repositório

Antes de escrever qualquer código:
- Leia os projetos existentes em `src/Modules/Transactions` para entender o padrão adotado (estrutura de pastas, nomenclatura, DI, configurações EF, Global Query Filter).
- Leia `src/Api/Persistence/AppDbContext.cs` para entender como os `DbSet`, Global Query Filters e configurações de entidade estão registrados.
- Leia `src/Api/Program.cs` para entender como os módulos são registrados no DI.
- Leia `tests/StaqFinance.Api.IntegrationTests/WorkspaceTestBase.cs` e `IntegrationTestCollection.cs` para manter o mesmo padrão de testes.

Liste as decisões de design que você vai tomar (nomes de classes, estrutura de pastas, FK behavior, etc.) **antes de começar a escrever código**.

---

### 2. Modelo de domínio

#### Entidade `RecurringTransaction`

| Campo           | Tipo                     | Restrições                                           |
|-----------------|--------------------------|------------------------------------------------------|
| `Id`            | `Guid`                   | PK, gerado pela aplicação                            |
| `TenantId`      | `Guid`                   | FK implícita (Global Query Filter), index            |
| `AccountId`     | `Guid`                   | FK → `Accounts.Id`, obrigatório                      |
| `CategoryId`    | `Guid?`                  | FK → `Categories.Id`, **nullable**                   |
| `Description`   | `string`                 | Obrigatório, máx 255 chars                           |
| `Type`          | `TransactionType`        | Enum: `Income = 1`, `Expense = 2` (reutilizar)       |
| `AmountCents`   | `long`                   | Obrigatório, > 0                                     |
| `StartDate`     | `DateOnly`               | Obrigatório                                          |
| `EndDate`       | `DateOnly?`              | Opcional — nulo = sem fim                            |
| `Frequency`     | `RecurringFrequency`     | Enum: `Monthly = 1`, `Weekly = 2`                    |
| `Interval`      | `int`                    | Obrigatório, >= 1 (a cada N períodos)                |
| `NextRunOn`     | `DateOnly`               | Próximo dia a materializar, index                    |
| `IsActive`      | `bool`                   | `true` por padrão                                    |
| `CreatedAt`     | `DateTime`               | UTC, definido na criação                             |

#### Entidade `RecurringTransactionRun` (idempotência)

| Campo                    | Tipo      | Restrições                                              |
|--------------------------|-----------|---------------------------------------------------------|
| `Id`                     | `Guid`    | PK, gerado pela aplicação                               |
| `TenantId`               | `Guid`    | Index                                                   |
| `RecurringTransactionId` | `Guid`    | FK → `RecurringTransactions.Id`                         |
| `RunDate`                | `DateOnly`| Data materializada                                      |
| `GeneratedTransactionId` | `Guid`    | FK → `Transactions.Id` (lançamento gerado)              |

- Unique constraint: `(RecurringTransactionId, RunDate)` — impede duplicação na materialização.

#### Enum `RecurringFrequency`

```csharp
public enum RecurringFrequency
{
    Monthly = 1,
    Weekly  = 2
}
```

Crie este enum no projeto `RecurringTransactions.Domain`.

#### Factory `RecurringTransaction.Create`

```csharp
public static RecurringTransaction Create(
    Guid tenantId,
    Guid accountId,
    Guid? categoryId,
    string description,
    TransactionType type,
    long amountCents,
    DateOnly startDate,
    DateOnly? endDate,
    RecurringFrequency frequency,
    int interval)
```

- `NextRunOn = startDate` na criação.
- `IsActive = true` na criação.

#### Método de avanço do `NextRunOn`

Adicione à entidade um método interno para calcular o próximo run:

```csharp
public DateOnly AdvanceNextRunOn()
{
    NextRunOn = Frequency switch
    {
        RecurringFrequency.Monthly => NextRunOn.AddMonths(Interval),
        RecurringFrequency.Weekly  => NextRunOn.AddDays(7 * Interval),
        _ => throw new InvalidOperationException("Frequência desconhecida.")
    };
    return NextRunOn;
}
```

#### Regras de integridade

- `AccountId` deve pertencer ao mesmo tenant. Se não existir, retorne `Error.NotFound("Account.NotFound", "Conta não encontrada.")`.
- `CategoryId`, quando informado, deve pertencer ao mesmo tenant. Se não existir, retorne `Error.NotFound("Category.NotFound", "Categoria não encontrada.")`.
- `AmountCents` deve ser > 0.
- `EndDate`, quando informado, deve ser >= `StartDate`.
- `Interval` deve ser >= 1.

---

### 3. Endpoints a implementar

#### `POST /api/workspaces/{workspaceSlug}/recurring-transactions`

- **Autenticação/autorização:** `[Authorize(Policy = "MustBelongToTenant")]`
- **Request body:**

```json
{
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "categoryId": "7ba85f64-5717-4562-b3fc-2c963f66afa7",
  "description": "Aluguel",
  "type": 2,
  "amountCents": 150000,
  "startDate": "2025-08-01",
  "endDate": "2026-07-01",
  "frequency": 1,
  "interval": 1
}
```

- **Validações (antes de chegar ao handler):**
  - `accountId`: obrigatório, não pode ser `Guid.Empty`.
  - `description`: obrigatório, máx 255 chars.
  - `type`: obrigatório, deve ser `1` ou `2`.
  - `amountCents`: obrigatório, deve ser > 0.
  - `startDate`: obrigatório.
  - `endDate`: quando informado, deve ser >= `startDate`.
  - `frequency`: obrigatório, deve ser `1` ou `2`.
  - `interval`: obrigatório, deve ser >= 1.

- **Response `201 Created`:**

```json
{
  "id": "...",
  "accountId": "...",
  "categoryId": "...",
  "description": "Aluguel",
  "type": 2,
  "amountCents": 150000,
  "startDate": "2025-08-01",
  "endDate": "2026-07-01",
  "frequency": 1,
  "interval": 1,
  "nextRunOn": "2025-08-01",
  "isActive": true,
  "createdAt": "2025-07-10T18:00:00Z"
}
```

- **Erros possíveis:**

| Status | Código de erro          | Quando                                    |
|--------|-------------------------|-------------------------------------------|
| `400`  | (validação)             | Body inválido                             |
| `400`  | `EndDate.BeforeStart`   | `endDate` < `startDate`                   |
| `401`  | —                       | Não autenticado                           |
| `403`  | —                       | Usuário não é membro do workspace         |
| `404`  | `Account.NotFound`      | `accountId` não existe no tenant          |
| `404`  | `Category.NotFound`     | `categoryId` não existe no tenant         |

---

#### `GET /api/workspaces/{workspaceSlug}/recurring-transactions`

- **Autenticação/autorização:** `[Authorize(Policy = "MustBelongToTenant")]`
- **Query string (todos opcionais):**

| Parâmetro  | Tipo   | Descrição                              |
|------------|--------|----------------------------------------|
| `isActive` | `bool` | Filtrar por ativas (`true`) ou inativas (`false`) |

- **Ordenação padrão:** `NextRunOn ASC`, depois `CreatedAt ASC`.
- **Response `200 OK`:**

```json
[
  {
    "id": "...",
    "accountId": "...",
    "categoryId": null,
    "description": "Aluguel",
    "type": 2,
    "amountCents": 150000,
    "startDate": "2025-08-01",
    "endDate": null,
    "frequency": 1,
    "interval": 1,
    "nextRunOn": "2025-08-01",
    "isActive": true,
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

#### `POST /api/workspaces/{workspaceSlug}/recurring-transactions/run?until=YYYY-MM-DD`

Este endpoint materializa as recorrências ativas em lançamentos concretos na tabela `Transactions`.

- **Autenticação/autorização:** `[Authorize(Policy = "MustBelongToTenant")]`
- **Query string:**

| Parâmetro | Tipo       | Obrigatório | Descrição                                           |
|-----------|------------|-------------|-----------------------------------------------------|
| `until`   | `DateOnly` | Sim         | Data limite (inclusiva) para materialização         |

- **Comportamento (executado dentro de uma única transação de banco):**
  1. Busca todas as `RecurringTransaction` ativas (`IsActive = true`) do tenant.
  2. Para cada uma, enquanto `NextRunOn <= until` **e** (`EndDate` é nulo **ou** `NextRunOn <= EndDate`):
     a. Verifica se já existe um `RecurringTransactionRun` com `(RecurringTransactionId, RunDate = NextRunOn)` — se existir, **pula** (idempotência).
     b. Cria uma `Transaction` com os dados da recorrência e `Date = NextRunOn`.
     c. Cria um `RecurringTransactionRun` com `RunDate = NextRunOn` e `GeneratedTransactionId`.
     d. Chama `AdvanceNextRunOn()` na recorrência.
  3. Se `EndDate` foi atingido (ou seja, o novo `NextRunOn > EndDate`), define `IsActive = false`.
  4. Salva todas as mudanças em uma única chamada a `SaveChangesAsync()`.

- **Response `200 OK`:**

```json
{
  "processedRules": 3,
  "generatedTransactions": 5
}
```

- **Erros possíveis:**

| Status | Código de erro           | Quando                                  |
|--------|--------------------------|-----------------------------------------|
| `400`  | `Until.Required`         | Parâmetro `until` não informado         |
| `401`  | —                        | Não autenticado                         |
| `403`  | —                        | Usuário não é membro do workspace       |

---

### 4. Estrutura de arquivos esperada (seguindo o padrão dos módulos anteriores)

```
src/Modules/RecurringTransactions/
  Domain/
    Entities/RecurringTransaction.cs
    Entities/RecurringTransactionRun.cs
    Enums/RecurringFrequency.cs
  Application/
    Commands/CreateRecurringTransactionCommand.cs      ← record + interface ICreateRecurringTransactionCommandHandler
    Commands/RunRecurringTransactionsCommand.cs        ← record + interface IRunRecurringTransactionsCommandHandler
    Queries/ListRecurringTransactionsQuery.cs          ← record + interface IListRecurringTransactionsQueryHandler
    DTOs/RecurringTransactionResponse.cs
    DTOs/RunRecurringTransactionsResponse.cs
    DTOs/ListRecurringTransactionsFilter.cs
    DI/RecurringTransactionsApplicationModule.cs
  Infrastructure/
    Handlers/CreateRecurringTransactionCommandHandler.cs
    Handlers/RunRecurringTransactionsCommandHandler.cs
    Handlers/ListRecurringTransactionsQueryHandler.cs
    Persistence/Configurations/RecurringTransactionConfiguration.cs
    Persistence/Configurations/RecurringTransactionRunConfiguration.cs
    DI/RecurringTransactionsInfrastructureModule.cs

src/Api/
  Controllers/RecurringTransactionsController.cs
```

---

### 5. Configuração EF Core

#### `RecurringTransactions`

- Tabela: `RecurringTransactions`
- `Id`: `ValueGeneratedNever()`
- `TenantId`: required, index
- `AccountId`: required, FK, `OnDelete(DeleteBehavior.Restrict)`
- `CategoryId`: nullable, FK, `OnDelete(DeleteBehavior.SetNull)`
- `Description`: required, max length 255
- `Type`: required, `HasConversion<int>()`
- `AmountCents`: required
- `StartDate`: required, column type `date`
- `EndDate`: nullable, column type `date`
- `Frequency`: required, `HasConversion<int>()`
- `Interval`: required
- `NextRunOn`: required, column type `date`, index
- `IsActive`: required
- `CreatedAt`: required

**Global Query Filter** (seguindo o padrão de `Transaction`):
```csharp
modelBuilder.Entity<RecurringTransaction>().HasQueryFilter(r =>
    !_currentTenant.IsResolved || r.TenantId == _currentTenant.TenantId);
```

#### `RecurringTransactionRuns`

- Tabela: `RecurringTransactionRuns`
- `Id`: `ValueGeneratedNever()`
- `TenantId`: required, index
- `RecurringTransactionId`: required, FK → `RecurringTransactions.Id`, `OnDelete(DeleteBehavior.Cascade)`
- `RunDate`: required, column type `date`
- `GeneratedTransactionId`: required, FK → `Transactions.Id`, `OnDelete(DeleteBehavior.Restrict)`
- Unique constraint: `(RecurringTransactionId, RunDate)`

> **Atenção:** `RecurringTransactionRun` **não deve ter Global Query Filter** — o handler precisa verificar runs existentes sem restrição de tenant para garantir idempotência mesmo em caso de reprocessamento.  
> Alternativamente, aplique o Global Query Filter também aqui, pois o TenantId está armazenado na entidade e o handler sempre opera no contexto do tenant correto.  
> Escolha a abordagem e documente a decisão.

Adicione `DbSet<RecurringTransaction> RecurringTransactions` e `DbSet<RecurringTransactionRun> RecurringTransactionRuns` ao `AppDbContext`.

Registre `RecurringTransactionConfiguration` e `RecurringTransactionRunConfiguration` em `AppDbContext.OnModelCreating`.

---

### 6. Registro no DI e no `Program.cs`

- Crie `RecurringTransactionsApplicationModule` e `RecurringTransactionsInfrastructureModule` com métodos de extensão `AddRecurringTransactionsModule()`.
- Registre os dois em `Program.cs` seguindo o mesmo padrão dos módulos anteriores.
- Adicione as referências de projeto necessárias em `src/Api/StaqFinance.Api.csproj`.
- Os projetos Infrastructure precisam de `<FrameworkReference Include="Microsoft.AspNetCore.App" />` e referência ao pacote `Microsoft.EntityFrameworkCore.Relational`.
- O módulo `RecurringTransactions.Infrastructure` precisa de referência ao projeto `Transactions.Domain` (para criar entidades `Transaction` durante o `run`).

---

### 7. Migration

Após implementar e buildar com sucesso, gere a migration:

```bash
dotnet ef migrations add AddRecurringTransactions --project src/Api/StaqFinance.Api.csproj
```

**Não aplique a migration agora** — o usuário fará isso manualmente.

---

### 8. Testes de integração

Crie `tests/StaqFinance.Api.IntegrationTests/RecurringTransactions/RecurringTransactionsTests.cs` com os seguintes cenários:

| #  | Cenário                                                                                  | Esperado              |
|----|------------------------------------------------------------------------------------------|-----------------------|
| 1  | `POST` sem token                                                                         | `401`                 |
| 2  | `POST` com token de outro tenant                                                         | `403`                 |
| 3  | `POST` com `accountId` inexistente                                                       | `404`                 |
| 4  | `POST` com `categoryId` inexistente                                                      | `404`                 |
| 5  | `POST` com `amountCents = 0`                                                             | `400`                 |
| 6  | `POST` com `interval = 0`                                                                | `400`                 |
| 7  | `POST` com `endDate` anterior a `startDate`                                              | `400`                 |
| 8  | `POST` válido sem categoria e sem endDate                                                 | `201` + body correto  |
| 9  | `POST` válido com categoria e endDate                                                    | `201` + body correto  |
| 10 | `GET` sem token                                                                          | `401`                 |
| 11 | `GET` autenticado com lista vazia                                                        | `200 []`              |
| 12 | `GET` retorna apenas recorrências do tenant corrente                                     | `200` isolado         |
| 13 | `GET` com filtro `isActive=false` retorna apenas inativas                                | `200` filtrado        |
| 14 | `POST /run` sem token                                                                    | `401`                 |
| 15 | `POST /run` sem parâmetro `until`                                                        | `400`                 |
| 16 | `POST /run` com `until` antes do `startDate` — nenhum lançamento gerado                 | `200` com `generatedTransactions = 0` |
| 17 | `POST /run` gera lançamentos corretamente para recorrência mensal                        | `200` + `Transactions` criadas no banco |
| 18 | `POST /run` é idempotente — segunda chamada com mesmo `until` não duplica lançamentos    | `200` com `generatedTransactions = 0` na segunda chamada |
| 19 | `POST /run` respeita `endDate` — não gera além do fim                                    | `200` + recorrência marcada `isActive = false` |
| 20 | `POST /run` gera múltiplos lançamentos quando `until` cobre vários períodos             | `200` + quantidade correta |

Use o mesmo `[Collection("Integration")]` e herde de `WorkspaceTestBase` para o setup de autenticação.

Nos helpers de setup dos testes, reutilize os métodos `CreateAccountAsync` e `CreateCategoryAsync` já existentes em `WorkspaceTestBase` (ou em helpers locais criados no Prompt 04). Adicione um método `CreateRecurringTransactionAsync(string token, string slug, object body)` para facilitar o setup.

---

### 9. Build e testes

Ao finalizar:
1. Execute `dotnet build` e corrija quaisquer erros de compilação.
2. Execute `dotnet test tests/StaqFinance.Api.IntegrationTests/StaqFinance.Api.IntegrationTests.csproj` e garanta que **todos** os testes passem (incluindo os dos prompts anteriores).
