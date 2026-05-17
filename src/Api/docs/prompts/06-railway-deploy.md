# Prompt 06 — Docker + Railway + Neon (Postgres) + variáveis de ambiente + migrações

Você é um assistente de programação. Quero preparar o projeto `staq-finance` para deploy no **Railway** com banco de dados **PostgreSQL no Neon**.

Contexto do projeto:
- API em ASP.NET Core (.NET 10), usando Controllers.
- Arquitetura: Monólito Modular + Clean Architecture.
- Banco: PostgreSQL com EF Core (migrações centralizadas no projeto `src/Api` ou onde foram definidas — analise o repositório).
- Auth: ASP.NET Identity + JWT.
- Logging: Serilog.

---

## Tarefas

### 1. Analise o repositório

Antes de qualquer mudança:
- Liste os projetos existentes na solution.
- Identifique onde estão as migrations do EF Core.
- Identifique o `DbContext` principal e seu nome.
- Verifique se já existe `Dockerfile`, `.dockerignore` ou `docker-compose.yml`.

### 2. Dockerfile (produção)

Crie `Dockerfile` na raiz do repositório (ou em `src/Api` se for mais adequado — justifique).

Requisitos:
- Multi-stage build: `build` → `publish` → `runtime`.
- Imagem base de build: `mcr.microsoft.com/dotnet/sdk:10.0`.
- Imagem base de runtime: `mcr.microsoft.com/dotnet/aspnet:10.0`.
- Publicar o projeto `src/Api` com `dotnet publish -c Release`.
- Expor porta `8080` (padrão do Railway para containers).
- `ENTRYPOINT ["dotnet", "StaqFinance.Api.dll"]` (ajuste o nome do DLL se necessário).
- Não copiar `appsettings.Development.json` para a imagem de produção.

### 3. `.dockerignore`

Crie `.dockerignore` na raiz do repositório, excluindo:
- `.git`, `.vs`, `.idea`
- `**/bin`, `**/obj`
- `**/*.user`, `**/*.suo`
- `**/node_modules`
- `docker-compose*.yml` (não precisamos no contexto de build)

### 4. `docker-compose.yml` (desenvolvimento local)

Crie (ou atualize) `docker-compose.yml` na raiz para facilitar o desenvolvimento local:

```yaml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: staqfinance
      POSTGRES_PASSWORD: staqfinance
      POSTGRES_DB: staqfinance
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__Default: "Host=db;Port=5432;Database=staqfinance;Username=staqfinance;Password=staqfinance"
      Jwt__Key: "dev-secret-key-change-in-production-32chars"
      Jwt__Issuer: "staqfinance-api"
      Jwt__Audience: "staqfinance-client"
    depends_on:
      - db

volumes:
  pgdata:
```

> Ajuste o nome da variável de connection string (`ConnectionStrings__Default`) se o projeto usar outro nome.

### 5. Estratégia de migrações em produção

Implemente a aplicação automática de migrações no startup da API (adequado para MVP):

- Em `Program.cs` (ou equivalente), após o build do `WebApplication`, adicione um bloco que:
  1. Resolve o `DbContext` do container de DI.
  2. Chama `await context.Database.MigrateAsync()`.
  3. Loga sucesso ou falha com Serilog.
- Envolva em `try/catch` para que erros de migração apareçam claramente nos logs e derrubem a aplicação com código de saída não-zero.

> ⚠️ Não use `EnsureCreated` — use sempre `MigrateAsync` para preservar o histórico de migrações.

### 6. `appsettings.Production.json`

Crie (ou atualize) `src/Api/appsettings.Production.json` com valores de placeholder e Serilog configurado para output estruturado (JSON) no console — ideal para Railway:

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Json.JsonFormatter, Serilog"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  },
  "ConnectionStrings": {
    "Default": ""
  },
  "Jwt": {
    "Key": "",
    "Issuer": "staqfinance-api",
    "Audience": "staqfinance-client",
    "ExpiresInMinutes": 60,
    "RefreshExpiresInDays": 7
  }
}
```

### 7. Variáveis de ambiente no Railway

Documente no `README.md` (na raiz ou em `src/Api/docs/`) as variáveis de ambiente obrigatórias para configurar no painel do Railway:

| Variável                        | Exemplo / Descrição                                                                 |
|---------------------------------|-------------------------------------------------------------------------------------|
| `ASPNETCORE_ENVIRONMENT`        | `Production`                                                                        |
| `PORT`                          | `8080` — Railway injeta automaticamente, mas confirme que a API respeita essa variável |
| `ConnectionStrings__Default`    | String de conexão do PostgreSQL do **Neon** (formato: `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require`) |
| `Jwt__Key`                      | Chave HMAC com mínimo 32 caracteres (gere com `openssl rand -base64 32`)            |
| `Jwt__Issuer`                   | `staqfinance-api`                                                                   |
| `Jwt__Audience`                 | `staqfinance-client`                                                                |
| `Jwt__ExpiresInMinutes`         | `60`                                                                                |
| `Jwt__RefreshExpiresInDays`     | `7`                                                                                 |

> O Railway (assim como o ASP.NET Core) usa `__` como separador hierárquico (equivalente a `:` no `appsettings.json`).
> A variável `PORT` é injetada automaticamente pelo Railway — garanta que o `Program.cs` respeite `ASPNETCORE_URLS` ou `PORT` (o template padrão do ASP.NET Core já faz isso).

### 8. Banco de dados no Neon

Documente o passo a passo para provisionar o banco no Neon e conectar ao Railway:

1. Criar conta em [neon.tech](https://neon.tech) (free tier).
2. Criar um novo **Project** (ex.: `staqfinance-prod`).
3. Criar um **Branch** `main` (já vem por padrão) e um banco `staqfinance`.
4. Na aba **Connection Details**, selecionar o driver **Npgsql / .NET** e copiar a connection string no formato:
   ```
   Host=ep-xxx.us-east-2.aws.neon.tech;Database=staqfinance;Username=staqfinance_owner;Password=xxx;SSL Mode=Require;Trust Server Certificate=true
   ```
5. Colar essa string na variável `ConnectionStrings__Default` do Railway.

> ⚠️ O Neon exige SSL. Confirme que a connection string inclui `SSL Mode=Require` e `Trust Server Certificate=true` (ou configure o certificado CA do Neon).

### 9. Configuração do serviço no Railway

Documente (no mesmo README acima ou num arquivo `docs/railway-deploy.md` dedicado) o passo a passo:

1. Criar conta em [railway.app](https://railway.app).
2. Criar um novo **Project** → **Deploy from GitHub repo**.
3. Conectar ao repositório `lfcorrea94/staq-finance`.
4. O Railway detecta o `Dockerfile` automaticamente — confirme que o serviço foi criado com **Environment: Docker**.
5. Na aba **Variables**, adicionar todas as variáveis listadas na seção 7.
6. Na aba **Settings → Networking**:
   - Gerar um domínio público (Railway fornece um `.up.railway.app` gratuito).
   - Confirmar que a porta pública aponta para `8080`.
7. Deploy automático: por padrão o Railway já faz **auto-deploy** a cada push no branch configurado (`main`).

### 10. Health check no Railway

- O Railway monitora o processo do container (se o processo cair, reinicia automaticamente).
- Não há configuração de health check HTTP obrigatória, mas é boa prática.
- Se quiser configurar: na aba **Settings → Health Check**, informe o path `/api/health`.
- Verifique que `GET /api/health` retorna `200 OK` sem autenticação (já implementado no Prompt 01).

---

## Regras importantes

- **Não altere** nenhuma lógica de negócio, domínio ou aplicação — esta entrega é exclusivamente de infraestrutura e deploy.
- Mantenha `appsettings.Development.json` intacto (não suba segredos reais).
- A `Jwt__Key` de produção **nunca** deve ser commitada — use variáveis de ambiente do Railway.
- Prefira imagens Docker oficiais da Microsoft (`mcr.microsoft.com`) com tag de versão explícita (não `:latest`).
- Se o `dotnet publish` precisar de argumentos específicos para a solution (ex.: `-p:UseAppHost=false`), inclua-os.

---

## Entregáveis esperados

- [ ] `Dockerfile` (multi-stage, build + runtime)
- [ ] `.dockerignore`
- [ ] `docker-compose.yml` (desenvolvimento local com API + Postgres)
- [ ] `src/Api/appsettings.Production.json` (Serilog JSON + placeholders)
- [ ] `Program.cs` atualizado com `MigrateAsync` no startup
- [ ] Seção de deploy adicionada ao `README.md` (ou arquivo `docs/railway-deploy.md` dedicado) com variáveis de ambiente e passo a passo do Railway + Neon

Antes de codar: liste as decisões que você vai tomar (ex.: localização do Dockerfile, nome do DLL de saída, caminho das migrations) e espere minha confirmação.
