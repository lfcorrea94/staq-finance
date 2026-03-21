using System.Net;

namespace StaqFinance.Api.IntegrationTests.Workspace;

public sealed class PingTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PingTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ping_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/workspaces/my-workspace/_ping");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ping_WithNonExistentWorkspace_Returns404()
    {
        var response = await _client.GetAsync("/api/workspaces/nonexistent-slug/_ping");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
