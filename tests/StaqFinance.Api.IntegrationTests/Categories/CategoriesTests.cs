using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace StaqFinance.Api.IntegrationTests.Categories;

[Collection("Integration")]
public sealed class CategoriesTests : WorkspaceTestBase
{
    private readonly ApiWebApplicationFactory _factory;

    public CategoriesTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // -------------------------------------------------------------------------
    // POST /api/workspaces/{slug}/categories
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateCategory_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/workspaces/any-slug/categories", new { name = "Alimentação" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithTokenOfOtherWorkspace_Returns403()
    {
        var client = _factory.CreateClient();
        var (tokenA, slugA) = await RegisterAndLoginAsync(client, $"cat-a-{Guid.NewGuid():N}@test.com");
        var (tokenB, _)     = await RegisterAndLoginAsync(client, $"cat-b-{Guid.NewGuid():N}@test.com");

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slugA}/categories", tokenB);
        request.Content = JsonContent.Create(new { name = "Alimentação" });
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithNonExistentWorkspace_Returns404()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndLoginAsync(client, $"cat-404-{Guid.NewGuid():N}@test.com");

        var request = AuthorizedRequest(HttpMethod.Post, "/api/workspaces/slug-inexistente/categories", token);
        request.Content = JsonContent.Create(new { name = "Alimentação" });
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithEmptyName_Returns400()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"cat-val-{Guid.NewGuid():N}@test.com");

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/categories", token);
        request.Content = JsonContent.Create(new { name = "" });
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithValidData_Returns201WithBody()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"cat-ok-{Guid.NewGuid():N}@test.com");

        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/categories", token);
        request.Content = JsonContent.Create(new { name = "Alimentação" });
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
        Assert.Equal("Alimentação", body.GetProperty("name").GetString());
        Assert.NotEqual(default, body.GetProperty("createdAt").GetDateTime());
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateName_Returns409()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"cat-dup-{Guid.NewGuid():N}@test.com");

        var postRequest = () =>
        {
            var r = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/categories", token);
            r.Content = JsonContent.Create(new { name = "Transporte" });
            return r;
        };

        await client.SendAsync(postRequest());
        var response = await client.SendAsync(postRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // GET /api/workspaces/{slug}/categories
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ListCategories_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/workspaces/any-slug/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListCategories_EmptyWorkspace_Returns200WithEmptyArray()
    {
        var client = _factory.CreateClient();
        var (token, slug) = await RegisterAndLoginAsync(client, $"cat-list0-{Guid.NewGuid():N}@test.com");

        var request = AuthorizedRequest(HttpMethod.Get, $"/api/workspaces/{slug}/categories", token);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(0, body.GetArrayLength());
    }

    [Fact]
    public async Task ListCategories_ReturnsOnlyCurrentTenantCategories()
    {
        var client = _factory.CreateClient();
        var (tokenA, slugA) = await RegisterAndLoginAsync(client, $"cat-iso-a-{Guid.NewGuid():N}@test.com");
        var (tokenB, slugB) = await RegisterAndLoginAsync(client, $"cat-iso-b-{Guid.NewGuid():N}@test.com");

        // Cria categoria no workspace A
        var createA = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slugA}/categories", tokenA);
        createA.Content = JsonContent.Create(new { name = "Categoria do Tenant A" });
        await client.SendAsync(createA);

        // Cria categoria no workspace B
        var createB = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slugB}/categories", tokenB);
        createB.Content = JsonContent.Create(new { name = "Categoria do Tenant B" });
        await client.SendAsync(createB);

        // Usuário A lista categorias — só deve ver a sua
        var listA = AuthorizedRequest(HttpMethod.Get, $"/api/workspaces/{slugA}/categories", tokenA);
        var responseA = await client.SendAsync(listA);

        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);

        var bodyA = await responseA.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, bodyA.GetArrayLength());
        Assert.Equal("Categoria do Tenant A", bodyA[0].GetProperty("name").GetString());
    }
}
