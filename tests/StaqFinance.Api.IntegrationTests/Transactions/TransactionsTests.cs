using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace StaqFinance.Api.IntegrationTests.Transactions;

[Collection("Integration")]
public sealed class TransactionsTests : WorkspaceTestBase
{
    private readonly ApiWebApplicationFactory _factory;

    public TransactionsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static object ValidTransactionBody(Guid accountId, Guid? categoryId = null) => new
    {
        accountId,
        categoryId,
        date = "2025-07-10",
        description = "Supermercado",
        type = 2,
        amountCents = 15000
    };

    // -------------------------------------------------------------------------
    // POST /api/workspaces/{slug}/transactions
    // -------------------------------------------------------------------------

    // Cenário 1
    [Fact]
    public async Task CreateTransaction_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/workspaces/any-slug/transactions", new
        {
            accountId = Guid.NewGuid(),
            date = "2025-07-10",
            description = "Test",
            type = 2,
            amountCents = 1000
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Cenário 2
    [Fact]
    public async Task CreateTransaction_WithTokenFromOtherTenant_Returns403()
    {
        var client = _factory.CreateClient();
        var (_, slugA) = await RegisterAndLoginAsync(client, $"trx-403a-{Guid.NewGuid():N}@test.com");
        var (tokenB, _) = await RegisterAndLoginAsync(client, $"trx-403b-{Guid.NewGuid():N}@test.com");

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slugA}/transactions", tokenB);
        request.Content = JsonContent.Create(new
        {
            accountId = Guid.NewGuid(),
            date = "2025-07-10",
            description = "Test",
            type = 2,
            amountCents = 1000
        });
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Cenário 3
    [Fact]
    public async Task CreateTransaction_WithNonExistentAccountId_Returns404()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"trx-404acc-{Guid.NewGuid():N}@test.com");

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/transactions", token);
        request.Content = JsonContent.Create(ValidTransactionBody(Guid.NewGuid()));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Cenário 4
    [Fact]
    public async Task CreateTransaction_WithNonExistentCategoryId_Returns404()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"trx-404cat-{Guid.NewGuid():N}@test.com");
        var accountId = await CreateAccountAsync(client, token, slug);

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/transactions", token);
        request.Content = JsonContent.Create(ValidTransactionBody(accountId, Guid.NewGuid()));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Cenário 5
    [Fact]
    public async Task CreateTransaction_WithZeroAmountCents_Returns400()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"trx-400amt-{Guid.NewGuid():N}@test.com");
        var accountId = await CreateAccountAsync(client, token, slug);

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/transactions", token);
        request.Content = JsonContent.Create(new
        {
            accountId,
            date = "2025-07-10",
            description = "Test",
            type = 2,
            amountCents = 0
        });
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Cenário 6
    [Fact]
    public async Task CreateTransaction_ValidWithoutCategory_Returns201()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"trx-201nocat-{Guid.NewGuid():N}@test.com");
        var accountId = await CreateAccountAsync(client, token, slug);

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/transactions", token);
        request.Content = JsonContent.Create(ValidTransactionBody(accountId));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
        Assert.Equal(accountId, body.GetProperty("accountId").GetGuid());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("categoryId").ValueKind);
        Assert.Equal("2025-07-10", body.GetProperty("date").GetString());
        Assert.Equal("Supermercado", body.GetProperty("description").GetString());
        Assert.Equal(2, body.GetProperty("type").GetInt32());
        Assert.Equal(15000, body.GetProperty("amountCents").GetInt64());
        Assert.NotEqual(default, body.GetProperty("createdAt").GetDateTime());
    }

    // Cenário 7
    [Fact]
    public async Task CreateTransaction_ValidWithCategory_Returns201()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"trx-201cat-{Guid.NewGuid():N}@test.com");
        var accountId = await CreateAccountAsync(client, token, slug);
        var categoryId = await CreateCategoryAsync(client, token, slug);

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/transactions", token);
        request.Content = JsonContent.Create(ValidTransactionBody(accountId, categoryId));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
        Assert.Equal(accountId, body.GetProperty("accountId").GetGuid());
        Assert.Equal(categoryId, body.GetProperty("categoryId").GetGuid());
    }

    // -------------------------------------------------------------------------
    // GET /api/workspaces/{slug}/transactions
    // -------------------------------------------------------------------------

    // Cenário 8
    [Fact]
    public async Task ListTransactions_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/workspaces/any-slug/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Cenário 9
    [Fact]
    public async Task ListTransactions_EmptyWorkspace_Returns200WithEmptyArray()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"trx-list0-{Guid.NewGuid():N}@test.com");

        var request = AuthorizedRequest(HttpMethod.Get, $"/api/workspaces/{slug}/transactions", token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(0, body.GetArrayLength());
    }

    // Cenário 10
    [Fact]
    public async Task ListTransactions_ReturnsOnlyCurrentTenantTransactions()
    {
        var client = _factory.CreateClient();
        var (tokenA, slugA) = await RegisterAndLoginAsync(client, $"trx-iso-a-{Guid.NewGuid():N}@test.com");
        var (tokenB, slugB) = await RegisterAndLoginAsync(client, $"trx-iso-b-{Guid.NewGuid():N}@test.com");

        var accountIdA = await CreateAccountAsync(client, tokenA, slugA, "Conta A");
        var accountIdB = await CreateAccountAsync(client, tokenB, slugB, "Conta B");

        var createA = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slugA}/transactions", tokenA);
        createA.Content = JsonContent.Create(ValidTransactionBody(accountIdA));
        await client.SendAsync(createA);

        var createB = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slugB}/transactions", tokenB);
        createB.Content = JsonContent.Create(ValidTransactionBody(accountIdB));
        await client.SendAsync(createB);

        var listA = AuthorizedRequest(HttpMethod.Get, $"/api/workspaces/{slugA}/transactions", tokenA);
        var responseA = await client.SendAsync(listA);

        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);

        var bodyA = await responseA.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, bodyA.GetArrayLength());
        Assert.Equal(accountIdA, bodyA[0].GetProperty("accountId").GetGuid());
    }

    // Cenário 11
    [Fact]
    public async Task ListTransactions_FilterByType_ReturnsOnlyExpenses()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"trx-ftype-{Guid.NewGuid():N}@test.com");
        var accountId = await CreateAccountAsync(client, token, slug);

        async Task Post(int type)
        {
            var r = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/transactions", token);
            r.Content = JsonContent.Create(new { accountId, date = "2025-07-10", description = "Test", type, amountCents = 1000 });
            await client.SendAsync(r);
        }

        await Post(1); // Income
        await Post(2); // Expense

        var request = AuthorizedRequest(HttpMethod.Get, $"/api/workspaces/{slug}/transactions?type=2", token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal(2, body[0].GetProperty("type").GetInt32());
    }

    // Cenário 12
    [Fact]
    public async Task ListTransactions_FilterByAccountId_ReturnsOnlyFromThatAccount()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"trx-facc-{Guid.NewGuid():N}@test.com");
        var accountId1 = await CreateAccountAsync(client, token, slug, "Conta 1");
        var accountId2 = await CreateAccountAsync(client, token, slug, "Conta 2");

        async Task Post(Guid accId)
        {
            var r = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/transactions", token);
            r.Content = JsonContent.Create(new { accountId = accId, date = "2025-07-10", description = "Test", type = 2, amountCents = 1000 });
            await client.SendAsync(r);
        }

        await Post(accountId1);
        await Post(accountId2);

        var request = AuthorizedRequest(HttpMethod.Get, $"/api/workspaces/{slug}/transactions?accountId={accountId1}", token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal(accountId1, body[0].GetProperty("accountId").GetGuid());
    }

    // Cenário 13
    [Fact]
    public async Task ListTransactions_FilterByFromAndTo_RespectsDateRange()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"trx-fdate-{Guid.NewGuid():N}@test.com");
        var accountId = await CreateAccountAsync(client, token, slug);

        async Task Post(string date)
        {
            var r = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/transactions", token);
            r.Content = JsonContent.Create(new { accountId, date, description = "Test", type = 2, amountCents = 1000 });
            await client.SendAsync(r);
        }

        await Post("2025-06-01");
        await Post("2025-07-10");
        await Post("2025-08-01");

        var request = AuthorizedRequest(HttpMethod.Get, $"/api/workspaces/{slug}/transactions?from=2025-07-01&to=2025-07-31", token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal("2025-07-10", body[0].GetProperty("date").GetString());
    }
}
