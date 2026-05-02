using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StaqFinance.Api.IntegrationTests.Accounts;

[Collection("Integration")]
public sealed class AccountsTests : WorkspaceTestBase
{
    private readonly ApiWebApplicationFactory _factory;

    public AccountsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // -------------------------------------------------------------------------
    // POST /api/workspaces/{slug}/accounts
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAccount_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/workspaces/any-slug/accounts", new { name = "Carteira" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_WithTokenOfOtherWorkspace_Returns403()
    {
        var client = _factory.CreateClient();
        var (tokenA, slugA) = await RegisterAndLoginAsync(client, $"acc-a-{Guid.NewGuid():N}@test.com");
        var (tokenB, _)     = await RegisterAndLoginAsync(client, $"acc-b-{Guid.NewGuid():N}@test.com");

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slugA}/accounts", tokenB);
        request.Content = JsonContent.Create(new { name = "Carteira" });
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_WithNonExistentWorkspace_Returns404()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndLoginAsync(client, $"acc-404-{Guid.NewGuid():N}@test.com");

        var request = AuthorizedRequest(HttpMethod.Post, "/api/workspaces/slug-inexistente/accounts", token);
        request.Content = JsonContent.Create(new { name = "Carteira" });
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_WithEmptyName_Returns400()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"acc-val-{Guid.NewGuid():N}@test.com");

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/accounts", token);
        request.Content = JsonContent.Create(new { name = "" });
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_WithValidData_Returns201WithBody()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"acc-ok-{Guid.NewGuid():N}@test.com");

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/accounts", token);
        request.Content = JsonContent.Create(new { name = "Conta Corrente" });
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
        Assert.Equal("Conta Corrente", body.GetProperty("name").GetString());
        Assert.NotEqual(default, body.GetProperty("createdAt").GetDateTime());
    }

    [Fact]
    public async Task CreateAccount_WithDuplicateName_Returns409()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"acc-dup-{Guid.NewGuid():N}@test.com");

        var postRequest = () =>
        {
            var r = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/accounts", token);
            r.Content = JsonContent.Create(new { name = "Poupança" });
            return r;
        };

        await client.SendAsync(postRequest());
        var response = await client.SendAsync(postRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // GET /api/workspaces/{slug}/accounts
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ListAccounts_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/workspaces/any-slug/accounts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListAccounts_EmptyWorkspace_Returns200WithEmptyArray()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"acc-list0-{Guid.NewGuid():N}@test.com");

        var request = AuthorizedRequest(HttpMethod.Get, $"/api/workspaces/{slug}/accounts", token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(0, body.GetArrayLength());
    }

    [Fact]
    public async Task ListAccounts_ReturnsOnlyCurrentTenantAccounts()
    {
        var client = _factory.CreateClient();
        var (tokenA, slugA) = await RegisterAndLoginAsync(client, $"acc-iso-a-{Guid.NewGuid():N}@test.com");
        var (tokenB, slugB) = await RegisterAndLoginAsync(client, $"acc-iso-b-{Guid.NewGuid():N}@test.com");

        // Cria conta no workspace A
        var createA = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slugA}/accounts", tokenA);
        createA.Content = JsonContent.Create(new { name = "Conta do Tenant A" });
        await client.SendAsync(createA);

        // Cria conta no workspace B
        var createB = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slugB}/accounts", tokenB);
        createB.Content = JsonContent.Create(new { name = "Conta do Tenant B" });
        await client.SendAsync(createB);

        // Usuário A lista contas — só deve ver a sua
        var listA = AuthorizedRequest(HttpMethod.Get, $"/api/workspaces/{slugA}/accounts", tokenA);
        var responseA = await client.SendAsync(listA);

        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);

        var bodyA = await responseA.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, bodyA.GetArrayLength());
        Assert.Equal("Conta do Tenant A", bodyA[0].GetProperty("name").GetString());
    }
}
