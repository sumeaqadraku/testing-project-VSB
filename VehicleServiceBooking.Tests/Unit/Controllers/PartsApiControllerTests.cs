using System.Net;
using System.Net.Http.Json;
using VehicleServiceBooking.Tests.Fixtures;

namespace VehicleServiceBooking.Tests.Unit.Controllers;

// PERSONA 3 — Parts endpoints
public class PartsApiControllerTests
{
    [Fact]
    public async Task GetParts_NoToken_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/partsapi");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreatePart_AsClient_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PostAsJsonAsync("/api/partsapi", new
        {
            Name          = "Oil Filter",
            PartNumber    = "OF-001",
            UnitPrice     = 5.99,
            StockQuantity = 100,
            MinStockLevel = 10
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatePart_AsManager_Returns201()
    {
        await using var factory = new TestWebApplicationFactory();
        var manager = factory.CreateAuthenticatedClient("Manager");

        var response = await manager.PostAsJsonAsync("/api/partsapi", new
        {
            Name          = "Brake Pad",
            PartNumber    = "BP-001",
            UnitPrice     = 12.50,
            StockQuantity = 50,
            MinStockLevel = 5,
            IsActive      = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeletePart_AsMechanic_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var mechanic = factory.CreateAuthenticatedClient("Mechanic");

        var response = await mechanic.DeleteAsync("/api/partsapi/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePart_AsClient_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PutAsJsonAsync("/api/partsapi/1", new
        {
            Id            = 1,
            Name          = "Changed",
            PartNumber    = "X-001",
            UnitPrice     = 1.00,
            StockQuantity = 1,
            MinStockLevel = 1
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
