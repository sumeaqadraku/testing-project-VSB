using System.Net;
using System.Net.Http.Json;
using VehicleServiceBooking.Tests.Fixtures;

namespace VehicleServiceBooking.Tests.Unit.Controllers;

// PERSONA 2 — ServiceTypes endpoints
public class ServiceTypesApiControllerTests
{
    [Fact]
    public async Task GetServiceTypes_NoToken_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/servicetypesapi");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateServiceType_AsClient_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PostAsJsonAsync("/api/servicetypesapi", new
        {
            Name                      = "Oil Change",
            BasePrice                 = 25.00,
            EstimatedDurationMinutes  = 30,
            ServiceCenterId           = 1
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateServiceType_AsManager_Returns201()
    {
        await using var factory = new TestWebApplicationFactory();
        var manager = factory.CreateAuthenticatedClient("Manager");

        // Krijo ServiceCenter fillimisht
        var scResp = await manager.PostAsJsonAsync("/api/servicecentersapi", new
        {
            Name    = "Center A",
            Address = "Rr. A 1",
            City    = "Prishtinë",
            Phone   = "044000001",
            Email   = "a@a.com",
            IsActive = true
        });
        var scBody = await scResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var scId = scBody.GetProperty("id").GetInt32();

        var response = await manager.PostAsJsonAsync("/api/servicetypesapi", new
        {
            Name                     = "Oil Change",
            BasePrice                = 25.00,
            EstimatedDurationMinutes = 30,
            ServiceCenterId          = scId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeleteServiceType_AsMechanic_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var mechanic = factory.CreateAuthenticatedClient("Mechanic");

        var response = await mechanic.DeleteAsync("/api/servicetypesapi/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
