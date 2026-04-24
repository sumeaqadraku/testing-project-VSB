using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Unit.Controllers;

// PERSONA 2 — WorkOrders endpoints
public class WorkOrdersApiControllerTests
{
    // Seed mechanic me UserId = MechanicId dhe workorder me MechanicId = mechanic.Id
    private static async Task<(int mechanicId, int workOrderId)> SeedMechanicAndWorkOrder(
        TestWebApplicationFactory factory,
        decimal hourlyRate = 60m,
        WorkOrderStatus status = WorkOrderStatus.InProgress)
    {
        using var scope = factory.CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var mechanic = new Mechanic
        {
            UserId        = TestAuthHelper.MechanicId,
            ServiceCenterId = 1, // InMemory nuk kontrollon FK
            HourlyRate    = hourlyRate,
            IsAvailable   = true,
            CreatedAt     = DateTime.UtcNow
        };
        db.Mechanics.Add(mechanic);
        await db.SaveChangesAsync();

        var wo = new WorkOrder
        {
            BookingId  = 1, // InMemory nuk kontrollon FK
            MechanicId = mechanic.Id,
            Status     = status,
            CreatedAt  = DateTime.UtcNow
        };
        db.WorkOrders.Add(wo);
        await db.SaveChangesAsync();

        return (mechanic.Id, wo.Id);
    }

    [Theory]
    [InlineData(0,   60,  0)]    // 0 minuta → $0
    [InlineData(60,  60,  60)]   // 60 min @ $60/h → $60
    [InlineData(90,  60,  90)]   // 90 min @ $60/h → $90
    [InlineData(480, 60,  480)]  // 480 min @ $60/h → $480
    public async Task UpdateWorkOrder_ToCompleted_LaborCostCalculatedCorrectly(
        int actualMinutes, decimal hourlyRate, decimal expectedCost)
    {
        await using var factory = new TestWebApplicationFactory();
        var (_, woId) = await SeedMechanicAndWorkOrder(factory, hourlyRate);

        var mechClient = factory.CreateAuthenticatedClient("Mechanic");
        var response = await mechClient.PutAsJsonAsync($"/api/workordersapi/{woId}", new
        {
            Id     = woId,
            Status = (int)WorkOrderStatus.Completed,
            ActualDurationMinutes = actualMinutes
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var wo = await db.WorkOrders.FindAsync(woId);
        Assert.Equal(expectedCost, wo!.LaborCost);
    }

    [Fact]
    public async Task UpdateWorkOrder_ToInProgress_SetsStartedAt()
    {
        await using var factory = new TestWebApplicationFactory();
        var (_, woId) = await SeedMechanicAndWorkOrder(factory, status: WorkOrderStatus.Scheduled);

        var mechClient = factory.CreateAuthenticatedClient("Mechanic");
        await mechClient.PutAsJsonAsync($"/api/workordersapi/{woId}", new
        {
            Id     = woId,
            Status = (int)WorkOrderStatus.InProgress
        });

        using var scope = factory.CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var wo = await db.WorkOrders.FindAsync(woId);
        Assert.NotNull(wo!.StartedAt);
    }

    [Fact]
    public async Task UpdateWorkOrder_ToCompleted_SetsCompletedAt()
    {
        await using var factory = new TestWebApplicationFactory();
        var (_, woId) = await SeedMechanicAndWorkOrder(factory);

        var mechClient = factory.CreateAuthenticatedClient("Mechanic");
        await mechClient.PutAsJsonAsync($"/api/workordersapi/{woId}", new
        {
            Id     = woId,
            Status = (int)WorkOrderStatus.Completed,
            ActualDurationMinutes = 60
        });

        using var scope = factory.CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var wo = await db.WorkOrders.FindAsync(woId);
        Assert.NotNull(wo!.CompletedAt);
    }

    [Fact]
    public async Task UpdateWorkOrder_AsClient_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var (_, woId) = await SeedMechanicAndWorkOrder(factory);

        var clientResp = factory.CreateAuthenticatedClient("Client");
        var response = await clientResp.PutAsJsonAsync($"/api/workordersapi/{woId}", new
        {
            Id     = woId,
            Status = (int)WorkOrderStatus.Completed
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkOrder_AsManager_Returns201()
    {
        await using var factory = new TestWebApplicationFactory();
        var (mechId, _) = await SeedMechanicAndWorkOrder(factory);

        var managerClient = factory.CreateAuthenticatedClient("Manager");
        var response = await managerClient.PostAsJsonAsync("/api/workordersapi", new
        {
            BookingId  = 99,
            MechanicId = mechId,
            Status     = (int)WorkOrderStatus.Scheduled
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkOrder_AsClient_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PostAsJsonAsync("/api/workordersapi", new
        {
            BookingId  = 1,
            MechanicId = 1,
            Status     = 0
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
