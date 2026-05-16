using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace StaqFinance.Api.IntegrationTests.RecurringTransactions;

[Collection("Integration")]
public sealed class RecurringTransactionsTests : WorkspaceTestBase
{
    private readonly HttpClient _client;

    public RecurringTransactionsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static object ValidRequest(Guid accountId, Guid? categoryId = null) => new
    {
        accountId,
        categoryId,
        description = "Aluguel",
        type = 2,
        amountCents = 150000,
        startDate = "2025-08-01",
        endDate = (string?)null,
        frequency = 1,
        interval = 1
    };

    private static async Task<Guid> CreateRecurringTransactionAsync(
        HttpClient client, string token, string slug, object body)
    {
        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions", token);
        request.Content = JsonContent.Create(body);
        var response = await client.SendAsync(request);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetGuid();
    }

    // 1. POST sem token → 401
    [Fact]
    public async Task Post_SemToken_Retorna401()
    {
        var response = await _client.PostAsJsonAsync("/api/workspaces/qualquer/recurring-transactions",
            new { accountId = Guid.NewGuid(), description = "X", type = 2, amountCents = 100, startDate = "2025-08-01", frequency = 1, interval = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // 2. POST com token de outro tenant → 403
    [Fact]
    public async Task Post_TokenDeOutroTenant_Retorna403()
    {
        var (_, slug) = await RegisterAndLoginAsync(_client, $"owner-{Guid.NewGuid()}@test.com");
        var (token2, _) = await RegisterAndLoginAsync(_client, $"other-{Guid.NewGuid()}@test.com");

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions", token2);
        request.Content = JsonContent.Create(ValidRequest(Guid.NewGuid()));
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // 3. POST com accountId inexistente → 404
    [Fact]
    public async Task Post_AccountIdInexistente_Retorna404()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions", token);
        request.Content = JsonContent.Create(ValidRequest(Guid.NewGuid()));
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // 4. POST com categoryId inexistente → 404
    [Fact]
    public async Task Post_CategoryIdInexistente_Retorna404()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");
        var accountId = await CreateAccountAsync(_client, token, slug);

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions", token);
        request.Content = JsonContent.Create(ValidRequest(accountId, Guid.NewGuid()));
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // 5. POST com amountCents = 0 → 400
    [Fact]
    public async Task Post_AmountCentsZero_Retorna400()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");
        var accountId = await CreateAccountAsync(_client, token, slug);

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions", token);
        request.Content = JsonContent.Create(new
        {
            accountId,
            description = "Aluguel",
            type = 2,
            amountCents = 0,
            startDate = "2025-08-01",
            frequency = 1,
            interval = 1
        });
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // 6. POST com interval = 0 → 400
    [Fact]
    public async Task Post_IntervalZero_Retorna400()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");
        var accountId = await CreateAccountAsync(_client, token, slug);

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions", token);
        request.Content = JsonContent.Create(new
        {
            accountId,
            description = "Aluguel",
            type = 2,
            amountCents = 10000,
            startDate = "2025-08-01",
            frequency = 1,
            interval = 0
        });
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // 7. POST com endDate anterior a startDate → 400
    [Fact]
    public async Task Post_EndDateAnteriorAStartDate_Retorna400()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");
        var accountId = await CreateAccountAsync(_client, token, slug);

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions", token);
        request.Content = JsonContent.Create(new
        {
            accountId,
            description = "Aluguel",
            type = 2,
            amountCents = 10000,
            startDate = "2025-08-01",
            endDate = "2025-07-01",
            frequency = 1,
            interval = 1
        });
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // 8. POST válido sem categoria e sem endDate → 201 + body correto
    [Fact]
    public async Task Post_ValidoSemCategoriaEEndDate_Retorna201()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");
        var accountId = await CreateAccountAsync(_client, token, slug);

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions", token);
        request.Content = JsonContent.Create(new
        {
            accountId,
            description = "Aluguel",
            type = 2,
            amountCents = 150000,
            startDate = "2025-08-01",
            frequency = 1,
            interval = 1
        });
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(accountId, body.GetProperty("accountId").GetGuid());
        Assert.Equal(150000, body.GetProperty("amountCents").GetInt64());
        Assert.Equal("2025-08-01", body.GetProperty("nextRunOn").GetString());
        Assert.True(body.GetProperty("isActive").GetBoolean());
    }

    // 9. POST válido com categoria e endDate → 201 + body correto
    [Fact]
    public async Task Post_ValidoComCategoriaEEndDate_Retorna201()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");
        var accountId = await CreateAccountAsync(_client, token, slug);
        var categoryId = await CreateCategoryAsync(_client, token, slug);

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions", token);
        request.Content = JsonContent.Create(new
        {
            accountId,
            categoryId,
            description = "Netflix",
            type = 2,
            amountCents = 4500,
            startDate = "2025-08-01",
            endDate = "2026-07-01",
            frequency = 1,
            interval = 1
        });
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(categoryId, body.GetProperty("categoryId").GetGuid());
        Assert.Equal("2026-07-01", body.GetProperty("endDate").GetString());
    }

    // 10. GET sem token → 401
    [Fact]
    public async Task Get_SemToken_Retorna401()
    {
        var response = await _client.GetAsync("/api/workspaces/qualquer/recurring-transactions");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // 11. GET autenticado com lista vazia → 200 []
    [Fact]
    public async Task Get_ListaVazia_Retorna200Array()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");

        var request = AuthorizedRequest(HttpMethod.Get, $"/api/workspaces/{slug}/recurring-transactions", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(0, body.GetArrayLength());
    }

    // 12. GET retorna apenas recorrências do tenant corrente
    [Fact]
    public async Task Get_RetornaApenasDoTenantCorrente()
    {
        var (token1, slug1) = await RegisterAndLoginAsync(_client, $"u1-{Guid.NewGuid()}@test.com");
        var (token2, slug2) = await RegisterAndLoginAsync(_client, $"u2-{Guid.NewGuid()}@test.com");

        var accountId1 = await CreateAccountAsync(_client, token1, slug1);
        await CreateRecurringTransactionAsync(_client, token1, slug1, new
        {
            accountId = accountId1,
            description = "Aluguel tenant1",
            type = 2,
            amountCents = 100000,
            startDate = "2025-08-01",
            frequency = 1,
            interval = 1
        });

        var request = AuthorizedRequest(HttpMethod.Get, $"/api/workspaces/{slug2}/recurring-transactions", token2);
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, body.GetArrayLength());
    }

    // 13. GET com filtro isActive=false retorna apenas inativas
    [Fact]
    public async Task Get_FiltroIsActiveFalse_RetornaApenasInativas()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");
        var accountId = await CreateAccountAsync(_client, token, slug);

        // Cria recorrência com endDate = startDate (será desativada após um run)
        await CreateRecurringTransactionAsync(_client, token, slug, new
        {
            accountId,
            description = "Uma vez só",
            type = 2,
            amountCents = 10000,
            startDate = "2025-01-01",
            endDate = "2025-01-01",
            frequency = 1,
            interval = 1
        });

        // Executa run para desativar
        var runReq = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions/run?until=2025-01-31", token);
        await _client.SendAsync(runReq);

        var request = AuthorizedRequest(HttpMethod.Get, $"/api/workspaces/{slug}/recurring-transactions?isActive=false", token);
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.GetArrayLength());
        Assert.False(body[0].GetProperty("isActive").GetBoolean());
    }

    // 14. POST /run sem token → 401
    [Fact]
    public async Task Run_SemToken_Retorna401()
    {
        var response = await _client.PostAsync("/api/workspaces/qualquer/recurring-transactions/run?until=2025-08-31", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // 15. POST /run sem parâmetro until → 400
    [Fact]
    public async Task Run_SemUntil_Retorna400()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions/run", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // 16. POST /run com until antes do startDate → generatedTransactions = 0
    [Fact]
    public async Task Run_UntilAntesDoStartDate_NaoGeraLancamentos()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");
        var accountId = await CreateAccountAsync(_client, token, slug);

        await CreateRecurringTransactionAsync(_client, token, slug, new
        {
            accountId,
            description = "Aluguel",
            type = 2,
            amountCents = 100000,
            startDate = "2025-09-01",
            frequency = 1,
            interval = 1
        });

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions/run?until=2025-08-01", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body.GetProperty("generatedTransactions").GetInt32());
    }

    // 17. POST /run gera lançamentos para recorrência mensal
    [Fact]
    public async Task Run_RecorrenciaMensal_GeraLancamentosCorretos()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");
        var accountId = await CreateAccountAsync(_client, token, slug);

        await CreateRecurringTransactionAsync(_client, token, slug, new
        {
            accountId,
            description = "Plano",
            type = 2,
            amountCents = 5000,
            startDate = "2025-01-01",
            frequency = 1,
            interval = 1
        });

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions/run?until=2025-03-31", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("processedRules").GetInt32());
        Assert.Equal(3, body.GetProperty("generatedTransactions").GetInt32());
    }

    // 18. POST /run é idempotente
    [Fact]
    public async Task Run_Idempotente_SegundaChamadaNaoDuplica()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");
        var accountId = await CreateAccountAsync(_client, token, slug);

        await CreateRecurringTransactionAsync(_client, token, slug, new
        {
            accountId,
            description = "Streaming",
            type = 2,
            amountCents = 3500,
            startDate = "2025-01-01",
            frequency = 1,
            interval = 1
        });

        var run1 = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions/run?until=2025-02-28", token);
        var resp1 = await _client.SendAsync(run1);
        var body1 = await resp1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body1.GetProperty("generatedTransactions").GetInt32());

        var run2 = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions/run?until=2025-02-28", token);
        var resp2 = await _client.SendAsync(run2);
        var body2 = await resp2.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
        Assert.Equal(0, body2.GetProperty("generatedTransactions").GetInt32());
    }

    // 19. POST /run respeita endDate e desativa recorrência
    [Fact]
    public async Task Run_RespeiraEndDate_DesativaRecorrencia()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");
        var accountId = await CreateAccountAsync(_client, token, slug);

        await CreateRecurringTransactionAsync(_client, token, slug, new
        {
            accountId,
            description = "Temporário",
            type = 2,
            amountCents = 10000,
            startDate = "2025-01-01",
            endDate = "2025-02-01",
            frequency = 1,
            interval = 1
        });

        var runReq = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions/run?until=2025-12-31", token);
        var runResp = await _client.SendAsync(runReq);
        var runBody = await runResp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, runResp.StatusCode);
        Assert.Equal(2, runBody.GetProperty("generatedTransactions").GetInt32());

        // Verifica que foi desativada
        var listReq = AuthorizedRequest(HttpMethod.Get, $"/api/workspaces/{slug}/recurring-transactions", token);
        var listResp = await _client.SendAsync(listReq);
        var listBody = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(listBody[0].GetProperty("isActive").GetBoolean());
    }

    // 20. POST /run gera múltiplos lançamentos para vários períodos
    [Fact]
    public async Task Run_VariosPeriodos_GeraQuantidadeCorreta()
    {
        var (token, slug) = await RegisterAndLoginAsync(_client, $"u-{Guid.NewGuid()}@test.com");
        var accountId = await CreateAccountAsync(_client, token, slug);

        await CreateRecurringTransactionAsync(_client, token, slug, new
        {
            accountId,
            description = "Semanal",
            type = 1,
            amountCents = 2000,
            startDate = "2025-01-06",
            frequency = 2,
            interval = 1
        });

        // 4 semanas: 06/01, 13/01, 20/01, 27/01
        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/recurring-transactions/run?until=2025-01-31", token);
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(4, body.GetProperty("generatedTransactions").GetInt32());
    }
}
