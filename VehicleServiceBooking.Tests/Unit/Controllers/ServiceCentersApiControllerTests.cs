using System.Net;
using System.Net.Http.Json;
using VehicleServiceBooking.Tests.Fixtures;

namespace VehicleServiceBooking.Tests.Unit.Controllers;

// PERSONA 2 — ServiceCenters endpoints
public class ServiceCentersApiControllerTests
{
    [Fact]
    public async Task GetServiceCenters_NoToken_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/servicecentersapi");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateServiceCenter_AsClient_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PostAsJsonAsync("/api/servicecentersapi", new
        {
            Name    = "Test Center",
            Address = "Rr. Test 1",
            City    = "Prishtinë",
            Phone   = "044000000",
            Email   = "center@test.com",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateServiceCenter_AsManager_Returns201()
    {
        await using var factory = new TestWebApplicationFactory();
        var manager = factory.CreateAuthenticatedClient("Manager");

        var response = await manager.PostAsJsonAsync("/api/servicecentersapi", new
        {
            Name     = "Manager Center",
            Address  = "Rr. Manager 1",
            City     = "Prishtinë",
            Phone    = "044111111",
            Email    = "mgr@center.com",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeleteServiceCenter_AsClient_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.DeleteAsync("/api/servicecentersapi/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
