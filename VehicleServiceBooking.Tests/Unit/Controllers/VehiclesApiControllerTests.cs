using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Unit.Controllers;

// PERSONA 1 — Vehicles endpoints
public class VehiclesApiControllerTests
{
    [Fact]
    public async Task UpdateVehicle_OtherClientsVehicle_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();

        int vehicleId;
        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var v = new Vehicle
            {
                ClientId     = TestAuthHelper.Client2Id,
                Make         = "Toyota",
                Model        = "Corolla",
                LicensePlate = "AB123CD",
                Year         = 2020,
                CreatedAt    = DateTime.UtcNow
            };
            db.Vehicles.Add(v);
            await db.SaveChangesAsync();
            vehicleId = v.Id;
        }

        var client = factory.CreateAuthenticatedClient("Client"); // Client1 tenton të editojë automjetin e Client2
        var response = await client.PutAsJsonAsync($"/api/vehiclesapi/{vehicleId}", new
        {
            Id           = vehicleId,
            ClientId     = TestAuthHelper.Client2Id,
            Make         = "Honda",
            Model        = "Civic",
            LicensePlate = "AB123CD",
            Year         = 2021
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVehicle_OtherClientsVehicle_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();

        int vehicleId;
        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var v = new Vehicle
            {
                ClientId     = TestAuthHelper.Client2Id,
                Make         = "BMW",
                Model        = "X5",
                LicensePlate = "XY456ZW",
                Year         = 2019,
                CreatedAt    = DateTime.UtcNow
            };
            db.Vehicles.Add(v);
            await db.SaveChangesAsync();
            vehicleId = v.Id;
        }

        var client = factory.CreateAuthenticatedClient("Client");
        var response = await client.DeleteAsync($"/api/vehiclesapi/{vehicleId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetVehicle_NoToken_Returns401()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/vehiclesapi/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateVehicle_DuplicateLicensePlate_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Vehicles.Add(new Vehicle
            {
                ClientId     = TestAuthHelper.ClientId,
                Make         = "Ford",
                Model        = "Focus",
                LicensePlate = "DUPL001",
                Year         = 2021,
                CreatedAt    = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = factory.CreateAuthenticatedClient("Client");
        var response = await client.PostAsJsonAsync("/api/vehiclesapi", new
        {
            Make         = "Opel",
            Model        = "Astra",
            LicensePlate = "DUPL001", // i njëjtë
            Year         = 2022
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
