# Prompt 01 — Foundation setup (solution + infra + tenancy + auth base)

Você é um assistente de programação. Quero iniciar a implementação do MVP descrito em `docs/mvp-blueprint-ptbr.md` no repositório GitHub `lfcorrea94/staq-finance`.

Contexto do MVP:
- API em ASP.NET Core (.NET), usando Controllers.
- Arquitetura: Monólito Modular + Clean Architecture (módulos em src/Modules/* com Domain/Application/Infrastructure).
- Multiusuário com isolamento por workspace (tenant) identificado por slug na rota: /api/workspaces/{workspaceSlug}/...
- Regra do MVP: cada usuário tem 1 workspace.
- Moeda única: BRL. Valores em centavos (AmountCents: long).
- Banco: PostgreSQL com EF Core.
- Auth: ASP.NET Identity + JWT + refresh token.
- Recorrência: tabelas RecurringTransactions e RecurringTransactionRuns, e endpoint manual POST /api/workspaces/{slug}/recurring-transactions/run?until=YYYY-MM-DD.

Tarefa (primeira entrega: fundação do projeto):
1) Analise o estado atual do repositório (arquivos existentes).
2) Proponha e crie a estrutura inicial da solution:
   - src/Api
   - src/BuildingBlocks
   - src/Modules/Tenancy (Domain/Application/Infrastructure)
   - src/Modules/Identity (Domain/Application/Infrastructure) ou integração direta com Identity no Api (decida e justifique)
   - tests (pelo menos um projeto de teste de API/integração)
3) Configure a API com:
   - Swagger/OpenAPI
   - Serilog (ou logging padrão bem configurado) e correlation id básico
   - Health check endpoint
4) Configure EF Core + PostgreSQL:
   - DbContext principal (ou por módulo com migrations centralizadas — escolha 1 e justifique)
   - Entidades Tenants e TenantUsers conforme blueprint
   - Migration inicial
5) Implemente tenancy:
   - Interface ICurrentTenant (TenantId, WorkspaceSlug)
   - Middleware (ou action filter) que resolve workspaceSlug da rota -> TenantId consultando Tenants
   - Se slug não existir: 404
6) Implemente autorização:
   - JWT auth configurada (sem endpoints ainda, apenas setup)
   - Policy MustBelongToTenant que valida TenantUsers (TenantId + UserId)
7) Crie endpoints mínimos para validar a fundação:
   - GET /api/health
   - GET /api/workspaces/{workspaceSlug}/_ping (autenticado + MustBelongToTenant) que retorna ok + tenantId resolvido
   (Se precisar, crie um endpoint temporário de seed para criar um tenant e associar a um user, mas prefira deixar isso para quando implementarmos register.)

Regras importantes:
- NÃO implemente Accounts/Categories/Transactions ainda.
- Use nomes de classes e arquivos em inglês, mas comentários e descrição no README podem ser em português.
- Garanta que o middleware/policy não permita vazamento entre tenants.
- Inclua instruções no README para rodar local com Postgres via docker-compose.
- Entregue como mudanças no repositório (arquivos e código), com uma lista do que foi criado/alterado.

Antes de codar: liste as decisões que você vai tomar (ex.: 1 DbContext vs vários, onde ficam migrations, padrão de DI por módulo) e espere minha confirmação.
