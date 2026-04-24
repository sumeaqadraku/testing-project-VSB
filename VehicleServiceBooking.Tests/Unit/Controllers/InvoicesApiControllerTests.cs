using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Unit.Controllers;

// PERSONA 2 — Invoices endpoints
public class InvoicesApiControllerTests
{
    private static async Task<int> SeedCompletedWorkOrder(
        TestWebApplicationFactory factory, decimal totalCost = 100m)
    {
        // Create a real Booking first — InvoicesApiController uses Include(wo => wo.Booking),
        // which behaves like an INNER JOIN in EF Core InMemory and filters out WorkOrders
        // whose related Booking doesn't exist in the store.
        var client = factory.CreateAuthenticatedClient("Client");
        var bookingResp = await client.PostAsJsonAsync("/api/bookingsapi", new
        {
            BookingDate = DateTime.Today.AddDays(7),
            BookingTime = "10:00:00",
            Status      = 0
        });
        var bookingBody = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
            await bookingResp.Content.ReadAsStringAsync());
        var bookingId = bookingBody.GetProperty("id").GetInt32();

        var manager = factory.CreateAuthenticatedClient("Manager");
        var createResp = await manager.PostAsJsonAsync("/api/workordersapi", new
        {
            BookingId  = bookingId,
            MechanicId = 1,
            Status     = (int)WorkOrderStatus.Completed,
            TotalCost  = totalCost
        });
        var rawBody = await createResp.Content.ReadAsStringAsync();
        Assert.True(createResp.IsSuccessStatusCode,
            $"WorkOrder creation failed: {createResp.StatusCode} — {rawBody}");
        var body = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(rawBody);
        return body.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task CreateInvoice_TaxAmount_Is18Percent()
    {
        await using var factory = new TestWebApplicationFactory();
        var woId = await SeedCompletedWorkOrder(factory, totalCost: 100m);

        var manager = factory.CreateAuthenticatedClient("Manager");
        var resp = await manager.PostAsJsonAsync("/api/invoicesapi", new { WorkOrderId = woId });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(18m, body.GetProperty("taxAmount").GetDecimal());
    }

    [Fact]
    public async Task CreateInvoice_TotalAmount_IsSubTotalPlusTax()
    {
        await using var factory = new TestWebApplicationFactory();
        var woId = await SeedCompletedWorkOrder(factory, totalCost: 200m);

        var manager = factory.CreateAuthenticatedClient("Manager");
        var resp = await manager.PostAsJsonAsync("/api/invoicesapi", new { WorkOrderId = woId });

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var sub   = body.GetProperty("subTotal").GetDecimal();
        var tax   = body.GetProperty("taxAmount").GetDecimal();
        var total = body.GetProperty("totalAmount").GetDecimal();

        Assert.Equal(sub + tax, total);
        Assert.Equal(236m, total); // 200 + 36
    }

    [Fact]
    public async Task CreateInvoice_InvoiceNumber_FollowsFormat()
    {
        await using var factory = new TestWebApplicationFactory();
        var woId = await SeedCompletedWorkOrder(factory);

        var manager = factory.CreateAuthenticatedClient("Manager");
        var resp = await manager.PostAsJsonAsync("/api/invoicesapi", new { WorkOrderId = woId });

        var body   = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var number = body.GetProperty("invoiceNumber").GetString()!;

        Assert.Matches(@"^INV-\d{8}-\d{4}$", number);
    }

    [Fact]
    public async Task CreateInvoice_DuplicateForSameWorkOrder_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        var woId = await SeedCompletedWorkOrder(factory);

        var manager = factory.CreateAuthenticatedClient("Manager");
        await manager.PostAsJsonAsync("/api/invoicesapi", new { WorkOrderId = woId });
        var resp2 = await manager.PostAsJsonAsync("/api/invoicesapi", new { WorkOrderId = woId });

        Assert.Equal(HttpStatusCode.BadRequest, resp2.StatusCode);
    }

    [Fact]
    public async Task GetInvoices_AsClient_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.GetAsync("/api/invoicesapi");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvoice_WorkOrderNotCompletedStatus_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();

        // Create a real Booking so the Include resolves correctly
        var client = factory.CreateAuthenticatedClient("Client");
        var bookingResp = await client.PostAsJsonAsync("/api/bookingsapi", new
        {
            BookingDate = DateTime.Today.AddDays(7),
            BookingTime = "11:00:00",
            Status      = 0
        });
        var bookingBody = await bookingResp.Content.ReadFromJsonAsync<JsonElement>();
        var bookingId = bookingBody.GetProperty("id").GetInt32();

        var manager = factory.CreateAuthenticatedClient("Manager");
        var createResp = await manager.PostAsJsonAsync("/api/workordersapi", new
        {
            BookingId  = bookingId,
            MechanicId = 1,
            Status     = (int)WorkOrderStatus.Scheduled,
            TotalCost  = 100.0m
        });
        var body = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var woId = body.GetProperty("id").GetInt32();

        var resp = await manager.PostAsJsonAsync("/api/invoicesapi", new { WorkOrderId = woId });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
