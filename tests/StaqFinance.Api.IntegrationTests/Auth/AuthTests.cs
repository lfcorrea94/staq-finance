using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StaqFinance.Api.IntegrationTests.Auth;

[Collection("Integration")]
public sealed class AuthTests
{
    private readonly ApiWebApplicationFactory _factory;

    public AuthTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static object ValidRegisterPayload(string email, string workspaceName = "My Workspace")
        => new { email, password = "Test123!", displayName = "Test User", workspaceName };

    private async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string workspaceName = "My Workspace")
        => await client.PostAsJsonAsync("/api/auth/register", ValidRegisterPayload(email, workspaceName));

    private async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = "Test123!")
        => await client.PostAsJsonAsync("/api/auth/login", new { email, password });

    private async Task<string> GetAccessTokenAsync(HttpClient client, string email)
    {
        var loginResp = await LoginAsync(client, email);
        var body = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    // -------------------------------------------------------------------------
    // Register
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Register_WithValidData_Returns201WithUserAndWorkspace()
    {
        var client = _factory.CreateClient();
        var email = $"reg-ok-{Guid.NewGuid():N}@test.com";

        var response = await RegisterAsync(client, email);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, body.GetProperty("userId").GetGuid());
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal("Test User", body.GetProperty("displayName").GetString());
        Assert.Equal("BRL", body.GetProperty("workspace").GetProperty("currency").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("workspace").GetProperty("slug").GetString()));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var email = $"reg-dup-{Guid.NewGuid():N}@test.com";

        await RegisterAsync(client, email, "First Workspace");
        var response = await RegisterAsync(client, email, "Second Workspace");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_CreatesUserTenantAndTenantUser()
    {
        var client = _factory.CreateClient();
        var email = $"reg-check-{Guid.NewGuid():N}@test.com";

        var response = await RegisterAsync(client, email, "Check Workspace");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEmpty(body.GetProperty("workspace").GetProperty("slug").GetString()!);
        Assert.Equal("Check Workspace", body.GetProperty("workspace").GetProperty("name").GetString());
    }

    // -------------------------------------------------------------------------
    // Login
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithTokens()
    {
        var client = _factory.CreateClient();
        var email = $"login-ok-{Guid.NewGuid():N}@test.com";

        await RegisterAsync(client, email);

        var response = await LoginAsync(client, email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("refreshToken").GetString()));
        Assert.True(body.GetProperty("expiresIn").GetInt32() > 0);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var email = $"login-fail-{Guid.NewGuid():N}@test.com";

        await RegisterAsync(client, email);

        var response = await LoginAsync(client, email, "WrongPass999!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await LoginAsync(client, "nobody@nowhere.com");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Refresh
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Refresh_WithValidToken_Returns200WithRotatedTokens()
    {
        var client = _factory.CreateClient();
        var email = $"refresh-ok-{Guid.NewGuid():N}@test.com";

        await RegisterAsync(client, email);
        var loginResp = await LoginAsync(client, email);
        var loginBody = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var originalRefreshToken = loginBody.GetProperty("refreshToken").GetString()!;

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { token = originalRefreshToken });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        Assert.NotEqual(originalRefreshToken, body.GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task Refresh_WithExpiredOrInvalidToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { token = "invalid-token-value" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ReusingOldToken_Returns401()
    {
        var client = _factory.CreateClient();
        var email = $"refresh-reuse-{Guid.NewGuid():N}@test.com";

        await RegisterAsync(client, email);
        var loginResp = await LoginAsync(client, email);
        var loginBody = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var originalRefreshToken = loginBody.GetProperty("refreshToken").GetString()!;

        // Primeira rotação — consome o token original
        await client.PostAsJsonAsync("/api/auth/refresh", new { token = originalRefreshToken });

        // Reutilizar o token original deve retornar 401
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { token = originalRefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // GET /api/me
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetMe_WithValidToken_Returns200WithUserAndWorkspace()
    {
        var client = _factory.CreateClient();
        var email = $"me-ok-{Guid.NewGuid():N}@test.com";

        await RegisterAsync(client, email, "Me Workspace");
        var token = await GetAccessTokenAsync(client, email);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal("Test User", body.GetProperty("displayName").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("workspace").GetProperty("slug").GetString()));
        Assert.Equal("BRL", body.GetProperty("workspace").GetProperty("currency").GetString());
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
