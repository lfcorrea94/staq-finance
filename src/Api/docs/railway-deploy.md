# Deploy — Railway + Neon (PostgreSQL)

Este guia descreve como publicar a API no **Railway** usando o banco de dados **Neon (PostgreSQL free tier)**.

---

## Pré-requisitos

- Conta no [Railway](https://railway.app) (free tier disponível)
- Conta no [Neon](https://neon.tech) (free tier permanente)
- Repositório no GitHub (`lfcorrea94/staq-finance`)

---

## 1. Banco de dados — Neon

1. Acesse [neon.tech](https://neon.tech) e crie uma conta.
2. Crie um novo **Project** (ex.: `staqfinance-prod`).
3. O branch `main` e o banco padrão já são criados automaticamente — renomeie o banco para `staqfinance` se preferir.
4. Na aba **Connection Details**, selecione o driver **Npgsql / .NET** e copie a connection string no formato:
   ```
   Host=ep-xxx.us-east-2.aws.neon.tech;Database=staqfinance;Username=staqfinance_owner;Password=xxx;SSL Mode=Require;Trust Server Certificate=true
   ```
5. Guarde essa string — ela será usada na variável `ConnectionStrings__Default` do Railway.

> ⚠️ O Neon exige SSL. A connection string **deve** conter `SSL Mode=Require;Trust Server Certificate=true`.

---

## 2. API — Railway

1. Acesse [railway.app](https://railway.app) e crie uma conta.
2. Clique em **New Project** → **Deploy from GitHub repo**.
3. Autorize o Railway e selecione o repositório `lfcorrea94/staq-finance`.
4. O Railway detecta o `Dockerfile` na raiz automaticamente e cria o serviço.
5. Na aba **Variables** do serviço, adicione as variáveis abaixo.
6. Na aba **Settings → Networking**:
   - Clique em **Generate Domain** para obter um domínio público (`.up.railway.app`).
   - Confirme que a porta pública aponta para `8080`.
7. O Railway faz **auto-deploy** a cada push no branch `main` por padrão.

---

## 3. Variáveis de ambiente (Railway)

Configure as seguintes variáveis na aba **Variables** do serviço:

| Variável                     | Valor / Descrição                                                                          |
|------------------------------|--------------------------------------------------------------------------------------------|
| `ASPNETCORE_ENVIRONMENT`     | `Production`                                                                               |
| `ConnectionStrings__Default` | Connection string do Neon (formato Npgsql com SSL — veja seção 1)                         |
| `Jwt__Key`                   | Chave HMAC mínimo 32 caracteres — gere com: `openssl rand -base64 32`                     |
| `Jwt__Issuer`                | `staqfinance-api`                                                                          |
| `Jwt__Audience`              | `staqfinance-client`                                                                       |
| `Jwt__ExpiresInMinutes`      | `60`                                                                                       |
| `Jwt__RefreshExpiresInDays`  | `7`                                                                                        |

> O Railway usa `__` como separador hierárquico, equivalente a `:` no `appsettings.json`.
> A variável `PORT` é injetada automaticamente pelo Railway. A API já está configurada para responder em `http://+:8080` via `ASPNETCORE_URLS`.

> ⚠️ **Nunca commite** a `Jwt__Key` real no repositório. Use sempre variáveis de ambiente.

---

## 4. Migrações

As migrações do EF Core são aplicadas **automaticamente no startup** da API via `MigrateAsync()`.

Não é necessário rodar nenhum comando manual — a cada deploy, a API verifica e aplica migrações pendentes antes de começar a servir requisições.

---

## 5. Health check

O Railway monitora o processo do container e reinicia automaticamente em caso de falha.

Para configurar health check HTTP (opcional):
- Aba **Settings → Health Check Path:** `/api/health`

---

## 6. Desenvolvimento local

Para rodar apenas o banco localmente (sem Docker para a API):

```bash
docker compose up -d
```

Isso sobe o PostgreSQL na porta `5432` com as credenciais definidas em `docker-compose.yml`.

A API pode ser rodada diretamente pelo Visual Studio ou via `dotnet run` em `src/Api/`.

A connection string para desenvolvimento está em `src/Api/appsettings.json`.
