using System.Net;

namespace StaqFinance.Api.IntegrationTests.Health;

[Collection("Integration")]
public sealed class HealthCheckTests
{
    private readonly HttpClient _client;

    public HealthCheckTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
