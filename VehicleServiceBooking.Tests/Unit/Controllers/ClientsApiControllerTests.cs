using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Unit.Controllers;

// PERSONA 3 — Clients endpoints
public class ClientsApiControllerTests
{
    [Fact]
    public async Task GetAllClients_AsClient_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.GetAsync("/api/clientsapi");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllClients_AsManager_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        var manager = factory.CreateAuthenticatedClient("Manager");

        var response = await manager.GetAsync("/api/clientsapi");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetClient_AsClient_OtherUser_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();

        // Regjistro Client2 dhe merr ID-në e tij/saj reale
        var anonClient = factory.CreateClient();
        var regResp = await anonClient.PostAsJsonAsync("/api/auth/register-client", new
        {
            Email     = "other@test.com",
            Password  = "Other@1234!",
            FirstName = "Other",
            LastName  = "User"
        });
        var body = await regResp.Content.ReadFromJsonAsync<JsonElement>();
        var otherId = body.GetProperty("user").GetProperty("id").GetString();

        // Client1 tenton të shohë profilin e Client2
        var client1 = factory.CreateAuthenticatedClient("Client");
        var response = await client1.GetAsync($"/api/clientsapi/{otherId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteClient_AsMechanic_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var mechanic = factory.CreateAuthenticatedClient("Mechanic");

        var response = await mechanic.DeleteAsync("/api/clientsapi/some-id");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllClients_NoToken_Returns401()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/clientsapi");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
