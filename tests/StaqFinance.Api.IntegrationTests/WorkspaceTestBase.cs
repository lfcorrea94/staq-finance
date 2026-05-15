using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StaqFinance.Api.IntegrationTests;

public abstract class WorkspaceTestBase
{
    protected static async Task<(string AccessToken, string WorkspaceSlug)> RegisterAndLoginAsync(
        HttpClient client,
        string email,
        string workspaceName = "Test Workspace")
    {
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Test123!",
            displayName = "Test User",
            workspaceName
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

    protected static HttpRequestMessage AuthorizedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    protected static async Task<Guid> CreateAccountAsync(HttpClient client, string token, string slug, string name = "Conta Corrente")
    {
        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/accounts", token);
        request.Content = JsonContent.Create(new { name });
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    protected static async Task<Guid> CreateCategoryAsync(HttpClient client, string token, string slug, string name = "Alimentação")
    {
        var request = AuthorizedRequest(HttpMethod.Post, $"/api/workspaces/{slug}/categories", token);
        request.Content = JsonContent.Create(new { name });
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }
}
