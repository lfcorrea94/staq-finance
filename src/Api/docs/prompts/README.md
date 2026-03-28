# Prompts (ordem de execução)

Abaixo está a sequência recomendada de prompts para implementar o MVP descrito em `docs/mvp-blueprint-ptbr.md`.

## Execução

1. `01-foundation-setup.md` — Bootstrap do projeto + EF Core + Postgres + Tenancy (slug) + Authorization base
2. `02-auth-register-login-me.md` — Cadastro/Login/Refresh + criação automática de workspace e vínculo + `/api/me`
3. `03-accounts-categories.md` — Accounts e Categories (create/list) com isolamento por tenant
4. `04-transactions.md` — Transactions (create/list) com filtros (período/conta/categoria/tipo)
5. `05-recurring-transactions.md` — Recorrência (create/list) + endpoint `run` com idempotência
6. `06-render-deploy.md` — Docker + Render + Postgres + variáveis de ambiente + migrações
