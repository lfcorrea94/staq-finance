using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StaqFinance.Api.IntegrationTests.Workspace;

public sealed class PingTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public PingTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<(string accessToken, string workspaceSlug)> RegisterAndLoginAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Test123!",
            displayName = "Test User",
            workspaceName = "Test Workspace"
        });

        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Test123!" });
        var loginBody = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginBody.GetProperty("accessToken").GetString()!;

        var meResp = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/me")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
        });
        var meBody = await meResp.Content.ReadFromJsonAsync<JsonElement>();
        var workspaceSlug = meBody.GetProperty("workspace").GetProperty("slug").GetString()!;

        return (accessToken, workspaceSlug);
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Ping_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/workspaces/my-workspace/_ping");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ping_WithValidTokenAndMembership_Returns200()
    {
        var client = _factory.CreateClient();
        var email = $"ping-ok-{Guid.NewGuid():N}@test.com";
        var (token, slug) = await RegisterAndLoginAsync(client, email);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/workspaces/{slug}/_ping");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", body.GetProperty("status").GetString());
        Assert.Equal(slug, body.GetProperty("workspaceSlug").GetString());
        Assert.NotEqual(Guid.Empty, body.GetProperty("tenantId").GetGuid());
    }

    [Fact]
    public async Task Ping_WithValidTokenButNotMember_Returns403()
    {
        var client = _factory.CreateClient();

        var (tokenA, slugA) = await RegisterAndLoginAsync(client, $"ping-a-{Guid.NewGuid():N}@test.com");
        var (tokenB, _)     = await RegisterAndLoginAsync(client, $"ping-b-{Guid.NewGuid():N}@test.com");

        // Usuário B tenta acessar o workspace do usuário A
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/workspaces/{slugA}/_ping");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Ping_WithValidTokenAndNonExistentWorkspace_Returns404()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndLoginAsync(client, $"ping-404-{Guid.NewGuid():N}@test.com");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/workspaces/slug-que-nao-existe/_ping");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
