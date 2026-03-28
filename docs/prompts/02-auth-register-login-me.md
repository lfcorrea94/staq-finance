# Prompt 02 — Auth: Register / Login / Refresh + workspace automático + `/api/me`

Você é um assistente de programação. A fundação do projeto já foi implementada (Prompt 01): solution estruturada, EF Core + PostgreSQL, entidades `Tenant` e `TenantUser`, middleware `TenantResolutionMiddleware`, policy `MustBelongToTenant`, JWT configurado e endpoints `/api/health` e `/api/workspaces/{workspaceSlug}/_ping` funcionando.

Contexto do MVP (reforço):
- API em ASP.NET Core (.NET), usando Controllers.
- Arquitetura: Monólito Modular + Clean Architecture (`src/Modules/*` com Domain / Application / Infrastructure).
- Auth: ASP.NET Core Identity + JWT Bearer + refresh token.
- Regra do MVP: **cada usuário tem exatamente 1 workspace** (impor via regra de aplicação).
- Geração de slug: slugify do nome do workspace (minúsculo, sem acentos, espaços → `-`, só `a-z 0-9 -`), tamanho 3–40. Em caso de colisão: tentar sufixo `-2`, `-3`, ... (ou sufixo aleatório curto após N tentativas).
- Banco: PostgreSQL via EF Core. `ApplicationUser` herda de `IdentityUser<Guid>` e possui `DisplayName (string)` e `CreatedAt (datetime UTC)`.
- Refresh token: armazenado na tabela do Identity (`AspNetUserTokens`) ou em tabela dedicada — **decida e justifique**.
- Moeda padrão do workspace no cadastro: `Currency = "BRL"` (fixo no MVP).

---

## Tarefa (segunda entrega: autenticação completa)

1. **Analise o estado atual do repositório** — entenda o que já existe em `src/Modules/Identity` e `src/Api` antes de criar qualquer arquivo.

2. **Implemente `POST /api/auth/register`**
   - Body:
     ```json
     {
       "email": "string",
       "password": "string",
       "displayName": "string",
       "workspaceName": "string"
     }
     ```
   - Fluxo atômico (tudo em uma transação):
     1. Validar os campos (ver Validações abaixo).
     2. Criar o `ApplicationUser` via `UserManager`.
     3. Gerar o slug a partir de `workspaceName` (slugify + tratamento de colisão).
     4. Criar o `Tenant` com `Name = workspaceName`, `Slug`, `Currency = "BRL"`, `CreatedAt = UtcNow`.
     5. Criar o vínculo `TenantUser` com `Role = "Owner"`, `JoinedAt = UtcNow`.
   - Resposta `201 Created`:
     ```json
     {
       "userId": "guid",
       "email": "string",
       "displayName": "string",
       "workspace": {
         "name": "string",
         "slug": "string",
         "currency": "BRL"
       }
     }
     ```
   - Erros esperados:
     - `400` — validação de campos (ver abaixo).
     - `409 Conflict` — e-mail já cadastrado.

3. **Implemente `POST /api/auth/login`**
   - Body:
     ```json
     {
       "email": "string",
       "password": "string"
     }
     ```
   - Fluxo:
     1. Validar credenciais via `SignInManager` ou `UserManager.CheckPasswordAsync`.
     2. Gerar access token JWT com claims: `sub` (userId), `email`, `name` (displayName).
     3. Gerar refresh token e persistir.
   - Resposta `200 OK`:
     ```json
     {
       "accessToken": "string",
       "expiresIn": 3600,
       "refreshToken": "string"
     }
     ```
   - Erros esperados:
     - `400` — body inválido.
     - `401` — credenciais incorretas.

4. **Implemente `POST /api/auth/refresh`**
   - Body:
     ```json
     {
       "refreshToken": "string"
     }
     ```
   - Fluxo:
     1. Validar o refresh token (existência, expiração, vinculação ao usuário).
     2. Revogar o refresh token atual (rotação obrigatória — emitir sempre um novo).
     3. Gerar novo access token + novo refresh token.
   - Resposta `200 OK`: mesma estrutura de `/login`.
   - Erros esperados:
     - `400` — body inválido.
     - `401` — refresh token inválido ou expirado.

5. **Implemente `GET /api/me`**
   - Requer `Authorization: Bearer {token}` (sem escopo de workspace).
   - Fluxo: ler `UserId` do JWT, buscar usuário + seu workspace único.
   - Resposta `200 OK`:
     ```json
     {
       "userId": "guid",
       "email": "string",
       "displayName": "string",
       "createdAt": "datetime",
       "workspace": {
         "name": "string",
         "slug": "string",
         "currency": "string"
       }
     }
     ```
   - Erros esperados:
     - `401` — token ausente ou inválido.
     - `404` — usuário não encontrado no banco (caso extremo).

---

## Validações (FluentValidation ou DataAnnotations — use o padrão já adotado no projeto)

### Register
- `email`: obrigatório, formato válido, máx 254 chars.
- `password`: obrigatório, mín 8 chars, pelo menos 1 letra maiúscula, 1 minúscula e 1 número.
- `displayName`: obrigatório, `1..100` chars.
- `workspaceName`: obrigatório, `2..80` chars (o slug gerado deve ter 3–40 após slugify — se ficar fora do range, retornar `400`).

### Login
- `email` e `password`: obrigatórios.

### Refresh
- `refreshToken`: obrigatório, não vazio.

---

## Estrutura esperada dos novos arquivos (guia — adapte se necessário)

```
src/Modules/Identity/
  Domain/
    ApplicationUser.cs            ← (já existe ou criar aqui)
    RefreshToken.cs               ← entidade de refresh token (se não usar AspNetUserTokens)
  Application/
    Commands/
      RegisterUserCommand.cs
      RegisterUserCommandHandler.cs
      LoginCommand.cs
      LoginCommandHandler.cs
      RefreshTokenCommand.cs
      RefreshTokenCommandHandler.cs
    Queries/
      GetMeQuery.cs
      GetMeQueryHandler.cs
    DTOs/
      RegisterRequest.cs
      RegisterResponse.cs
      LoginRequest.cs
      AuthResponse.cs
      MeResponse.cs
    Services/
      ITokenService.cs
      TokenService.cs             ← geração de JWT e refresh token
      ISlugService.cs
      SlugService.cs              ← slugify + colisão
  Infrastructure/
    IdentityModule.cs             ← registro de DI do módulo
src/Api/
  Controllers/
    AuthController.cs
    MeController.cs
```

> Adapte a estrutura se o padrão do projeto já divergir do guia acima. O importante é manter a separação Domain / Application / Infrastructure.

---

## Migration

- Gere uma nova migration EF Core para quaisquer mudanças de schema (ex.: tabela de refresh token dedicada, se for essa a decisão).
- Nomeie a migration de forma descritiva: ex. `AddRefreshTokens`.
- **Não remova nem altere** as migrations existentes.

---

## Testes

- Crie ao menos **testes de integração** (usando o projeto de testes já existente) para os fluxos principais:
  - Register com sucesso (verifica usuário, tenant e TenantUser criados).
  - Register com e-mail duplicado → `409`.
  - Login com sucesso (verifica tokens retornados).
  - Login com senha errada → `401`.
  - Refresh com token válido (verifica rotação do token).
  - Refresh com token inválido → `401`.
  - `GET /api/me` autenticado → retorna dados corretos.
  - `GET /api/me` sem token → `401`.

---

## Regras importantes

- **Transação atômica no register**: se qualquer etapa falhar (criar user, criar tenant, criar vínculo), fazer rollback completo. Não deixar usuário órfão sem workspace.
- **Rotação de refresh token**: ao usar `/refresh`, o token anterior deve ser invalidado imediatamente.
- **Não expor detalhes internos em erros**: em `401` de login, retornar mensagem genérica (`"Invalid credentials."`), sem indicar se o e-mail não existe ou a senha está errada.
- Use nomes de classes e arquivos em **inglês**; comentários e mensagens de log podem ser em português.
- Garanta que `GET /api/me` **não** passe pelo `TenantResolutionMiddleware` (é uma rota global, sem slug).
- Atualize o `docs/API.md` com os novos endpoints (`/api/auth/register`, `/api/auth/login`, `/api/auth/refresh`, `/api/me`), modelos de request/response e códigos de erro.

---

## Entrega esperada

1. Lista de todos os arquivos **criados** e **alterados** com uma linha descrevendo o que mudou.
2. Código completo de cada arquivo criado/alterado.
3. Comando(s) para gerar/aplicar a migration (se houver).
4. Confirmação de que os testes passam (`dotnet test`).