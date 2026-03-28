# Blueprint do MVP — Controle de Gastos (SaaS) com Recorrência

Este documento consolida um desenho **pronto para implementar** do MVP combinado: **lançamento simples (Opção A)** + **recorrência**, com **workspace (tenant) identificado por slug na rota**, **moeda única no MVP**, e **isolamento multiusuário preparado para SaaS**.

> Observações
> - Regra do MVP: cada usuário possui **exatamente 1 workspace**.
> - Preparado para evolução: mantenha `TenantId` (GUID/UUID) como chave primária interna; `Slug` é o identificador público.

---

## 1. Arquitetura (recomendada)

- **Estilo:** Monólito Modular + Clean Architecture
- **API:** ASP.NET Core Web API usando **Controllers**
- **Frontend:** React em repositório separado
- **Tenancy:** escopo por workspace, resolvido por **slug na rota**

### 1.1. Estrutura da solution (sugestão)

- `src/Api` — Host do ASP.NET Core (Controllers, autenticação, DI, middleware)
- `src/BuildingBlocks` — Componentes compartilhados (Result, erros, domain events, abstrações)
- `src/Modules/Identity/*`
- `src/Modules/Tenancy/*` (Workspaces)
- `src/Modules/Accounts/*`
- `src/Modules/Categories/*`
- `src/Modules/Transactions/*`
- `tests/*`

Em cada módulo, quando fizer sentido, separar em:
- `*.Domain` — regras e modelos do domínio (entidades, value objects, invariantes)
- `*.Application` — casos de uso (commands/queries), validações e orquestração
- `*.Infrastructure` — persistência (EF Core), integrações e implementações de repositórios

---

## 2. Tenancy e Rotas

### 2.1. Rotas públicas

- **Globais (sem slug):**
  - `POST /api/auth/register`
  - `POST /api/auth/login`
  - `POST /api/auth/refresh`
  - `GET /api/me`

- **Escopadas por workspace (slug na rota):**
  - `/api/workspaces/{workspaceSlug}/accounts`
  - `/api/workspaces/{workspaceSlug}/categories`
  - `/api/workspaces/{workspaceSlug}/transactions`
  - `/api/workspaces/{workspaceSlug}/recurring-transactions`

### 2.2. Workspace slug

Regras do slug (MVP):
- minúsculo
- `a-z`, `0-9`, `-`
- tamanho: 3–40
- único globalmente
- **não permitir rename** no MVP (pode ser adicionado depois)

Geração do slug no cadastro:
1. aplicar *slugify* no nome do workspace (minúsculo, remover acentos, espaços → `-`, remover caracteres inválidos)
2. se houver colisão: tentar `-2`, `-3`, ... (ou um sufixo curto aleatório após N tentativas)

---

## 3. Isolamento Multiusuário (segurança)

Para toda requisição escopada por workspace:
1. Validar JWT (usuário autenticado)
2. Resolver `{workspaceSlug}` → `TenantId`
3. Verificar vínculo (`TenantUsers`) para `(TenantId, UserId)`
4. Aplicar isolamento na persistência (filtros globais e escrita tenant-scoped)

Implementação recomendada:
- Um middleware (ou action filter) resolve o slug e define `ICurrentTenant.TenantId`.
- Uma policy de autorização `MustBelongToTenant` garante que o usuário pertence ao tenant.
- EF Core com **Global Query Filters** para forçar `TenantId` nas entidades tenant-scoped.

---

## 4. Moeda (MVP)

- Moeda única por workspace, fixo no MVP: `Currency = "BRL"`
- Armazenar dinheiro como **centavos inteiros** para evitar problemas de precisão:
  - `AmountCents: long`
- O sinal (entrada/saída) vem do `Type` (Income/Expense); o valor permanece positivo.

---

## 5. Modelo de Dados (PostgreSQL)

### 5.1. Identity

Usar as tabelas padrão do ASP.NET Identity para usuários/roles/tokens.

### 5.2. Tenancy

#### `Tenants`
- `Id (uuid PK)`
- `Name (text not null)` — nome do workspace (ex.: “Personal”)
- `Slug (text not null unique)` — identificador público em URL
- `Currency (char(3) not null)` — `BRL`
- `CreatedAt (timestamptz not null)`

#### `TenantUsers`
- `TenantId (uuid FK)`
- `UserId (uuid FK Identity)`
- `Role (text not null)` — `Owner` no MVP
- PK `(TenantId, UserId)`

Regra do MVP (1 workspace por usuário):
- impor via regra de aplicação
- opcional: índice unique em `TenantUsers(UserId)`

### 5.3. Domínio (tenant-scoped)

#### `Accounts`
- `Id (uuid PK)`
- `TenantId (uuid not null, index)`
- `Name (text not null)`
- `CreatedAt`

#### `Categories`
- `Id (uuid PK)`
- `TenantId (uuid not null, index)`
- `Name (text not null)`
- `CreatedAt`

#### `Transactions`
- `Id (uuid PK)`
- `TenantId (uuid not null, index)`
- `AccountId (uuid not null FK)`
- `CategoryId (uuid null FK)` — permitir nulo para “Sem categoria”
- `Date (date not null, index)` — data do lançamento
- `Description (text not null)`
- `Type (smallint not null)` — `1=Income`, `2=Expense`
- `AmountCents (bigint not null)`
- `CreatedAt`

---

## 6. Recorrência

### 6.1. Por que separar `Transactions` de `RecurringTransactions`

Recorrência é um **template + estado de execução** (quando rodar de novo, se está ativa, qual o próximo run). Separar evita misturar conceitos e facilita evoluir o mecanismo.

### 6.2. Tabelas

#### `RecurringTransactions`
- `Id (uuid PK)`
- `TenantId (uuid not null, index)`
- `AccountId (uuid not null FK)`
- `CategoryId (uuid null FK)`
- `Description (text not null)`
- `Type (smallint not null)`
- `AmountCents (bigint not null)`
- `StartDate (date not null)`
- `EndDate (date null)` — nulo = sem fim
- `Frequency (smallint not null)` — Monthly/Weekly
- `Interval (int not null default 1)` — a cada N períodos
- `NextRunOn (date not null, index)` — próximo dia a materializar
- `IsActive (bool not null default true)`
- `CreatedAt`

#### `RecurringTransactionRuns` (idempotência)
- `Id (uuid PK)`
- `TenantId (uuid not null)`
- `RecurringTransactionId (uuid not null)`
- `RunDate (date not null)` — data materializada
- Unique `(RecurringTransactionId, RunDate)` — impede duplicação

---

## 7. Processamento de recorrência (abordagem MVP)

### 7.1. Estratégia

As regras ficam em `RecurringTransactions`. Um comando as **materializa** em lançamentos concretos na tabela `Transactions`.

### 7.2. MVP sem job (execução manual)

Criar um endpoint protegido para gerar pendências (o frontend pode chamar ao abrir o app ou 1x/dia):

- `POST /api/workspaces/{slug}/recurring-transactions/run?until=YYYY-MM-DD`

Comportamento:
- Para cada regra ativa, gerar lançamentos enquanto `NextRunOn <= until` e (se houver `EndDate`, `NextRunOn <= EndDate`).
- Usar `RecurringTransactionRuns` para garantir idempotência.
- Atualizar `NextRunOn` após cada geração.

Evolução:
- Depois, trocar por job (`IHostedService`, Hangfire, Quartz) **sem mudar o modelo**.

---

## 8. Controllers e Endpoints (MVP)

### 8.1. Auth (global)
- `POST /api/auth/register` — cria usuário + workspace + vínculo
- `POST /api/auth/login`
- `POST /api/auth/refresh`

### 8.2. Me (global)
- `GET /api/me` — retorna usuário e `{ name, slug, currency }` do workspace único

### 8.3. Accounts (workspace)
- `POST /api/workspaces/{slug}/accounts`
- `GET /api/workspaces/{slug}/accounts`

### 8.4. Categories (workspace)
- `POST /api/workspaces/{slug}/categories`
- `GET /api/workspaces/{slug}/categories`

### 8.5. Transactions (workspace)
- `POST /api/workspaces/{slug}/transactions`
- `GET /api/workspaces/{slug}/transactions?from=YYYY-MM-DD&to=YYYY-MM-DD&accountId=&categoryId=&type=`

### 8.6. RecurringTransactions (workspace)
- `POST /api/workspaces/{slug}/recurring-transactions`
- `GET /api/workspaces/{slug}/recurring-transactions`
- `POST /api/workspaces/{slug}/recurring-transactions/run?until=YYYY-MM-DD`

---

## 9. DTOs (contrato mínimo)

### 9.1. `CreateTransactionRequest` (Opção A)
- `accountId: guid`
- `categoryId?: guid`
- `date: YYYY-MM-DD`
- `description: string`
- `type: "income" | "expense"`
- `amountCents: number` (inteiro > 0)

### 9.2. `CreateRecurringTransactionRequest`
Mesmos campos da transaction (menos `date`) + recorrência:
- `startDate: YYYY-MM-DD`
- `endDate?: YYYY-MM-DD`
- `frequency: "monthly" | "weekly"`
- `interval: number` (>= 1)

### 9.3. Resposta de listagem (sugestão)
Cada item:
- `id, date, description, type, amountCents, accountId, categoryId`

---

## 10. Validações (FluentValidation)

### 10.1. Transaction
- `amountCents > 0`
- `description` tamanho `1..200`
- `accountId` existe e pertence ao tenant
- `categoryId` (se informado) existe e pertence ao tenant

### 10.2. Recorrência
- `interval >= 1`
- `endDate >= startDate` (se endDate informado)
- `NextRunOn = startDate` na criação

No run:
- gerar enquanto `NextRunOn <= until` e (`endDate` nulo ou `NextRunOn <= endDate`)

---

## 11. Hospedagem (Render + Postgres)

Sugestão para MVP:
- **API:** Render (Web Service com Docker)
- **Banco:** Postgres (Neon free tier ou Postgres gerenciado do Render)

Variáveis de ambiente:
- `ConnectionStrings__Default`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `ASPNETCORE_ENVIRONMENT=Production`

---

## 12. Próximas evoluções (pós-MVP)

- Múltiplos workspaces por usuário (remover restrição do MVP)
- Papéis/permissões (Admin/Member)
- Importação de extratos (CSV/OFX)
- Job em background para recorrência
- Tags, notas, anexos
- Relatórios e dashboards com read models otimizados