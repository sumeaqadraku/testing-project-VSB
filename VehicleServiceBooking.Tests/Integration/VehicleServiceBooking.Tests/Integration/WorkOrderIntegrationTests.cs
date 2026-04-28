using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;
using Xunit;

namespace VehicleServiceBooking.Tests.Integration;

public class WorkOrderIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public WorkOrderIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AssignMechanicToBooking_CreatesWorkOrderInDb()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient("Manager");
        var bookingId = 1; // Supozojmë se ky booking ekziston ose e shton me Seeding

        // Act
        // Supozojmë endpointin: POST /api/bookings/{id}/assign
        var response = await client.PostAsJsonAsync($"/api/bookings/{bookingId}/assign", new { MechanicId = 1 });

        // Assert
        response.EnsureSuccessStatusCode();

        using var scope = _factory.CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var wo = db.WorkOrders.FirstOrDefault(w => w.BookingId == bookingId);
        
        Assert.NotNull(wo); // Verifikojmë që u krijua në DB
    }

    [Fact]
    public async Task MechanicUpdateWorkOrder_CalculatesLaborCost()
    {
        // Arrange
        var mechClient = _factory.CreateAuthenticatedClient("Mechanic");
        var woId = 1; // ID e një WorkOrder që ekziston në Seeding

        // Act
        // Përditësojmë me kohëzgjatje 60 min (1 orë)
        var response = await mechClient.PutAsJsonAsync($"/api/workorders/{woId}", new 
        { 
            Id = woId, 
            Status = 1, // InProgress
            ActualDurationMinutes = 60 
        });

        // Assert
        response.EnsureSuccessStatusCode();

        using var scope = _factory.CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var wo = await db.WorkOrders.FindAsync(woId);

        // Verifikojmë nëse LaborCost është llogaritur (p.sh. HourlyRate * 1)
        Assert.True(wo?.LaborCost > 0); 
    }

    [Fact]
    public async Task ManagerCreatesWorkOrder_IsVisibleInGetList()
    {
        // Arrange
        var managerClient = _factory.CreateAuthenticatedClient("Manager");
        var newWo = new { BookingId = 2, MechanicId = 1, Status = 0 };

        // Act
        await managerClient.PostAsJsonAsync("/api/workorders", newWo);
        var response = await managerClient.GetAsync("/api/workorders");

        // Assert
        response.EnsureSuccessStatusCode();
        var workOrders = await response.Content.ReadFromJsonAsync<List<WorkOrder>>();
        
        Assert.Contains(workOrders, w => w.BookingId == 2);
    }
}