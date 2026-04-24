using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Unit.Controllers;

// PERSONA 3 — Payments endpoints
public class PaymentsApiControllerTests
{
    // Seed: Booking + WorkOrder (Completed) + Invoice (total=100)
    private static async Task<int> SeedPayableWorkOrder(
        TestWebApplicationFactory factory, decimal invoiceTotal = 100m)
    {
        using var scope = factory.CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var mechanic = new Mechanic
        {
            UserId = TestAuthHelper.MechanicId,
            ServiceCenterId = 1,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Mechanics.Add(mechanic);
        await db.SaveChangesAsync();

        var booking = new Booking
        {
            ClientId    = TestAuthHelper.ClientId,
            BookingDate = DateTime.Today.AddDays(1),
            BookingTime = TimeSpan.FromHours(10),
            Status      = BookingStatus.Confirmed,
            CreatedAt   = DateTime.UtcNow
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var wo = new WorkOrder
        {
            BookingId  = booking.Id,
            MechanicId = mechanic.Id,
            Status     = WorkOrderStatus.ReadyForPayment,
            TotalCost  = invoiceTotal,
            CreatedAt  = DateTime.UtcNow
        };
        db.WorkOrders.Add(wo);
        await db.SaveChangesAsync();

        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-TEST-{wo.Id:D4}",
            WorkOrderId   = wo.Id,
            SubTotal      = invoiceTotal,
            TaxAmount     = invoiceTotal * 0.18m,
            TotalAmount   = invoiceTotal * 1.18m,
            InvoiceDate   = DateTime.UtcNow,
            DueDate       = DateTime.UtcNow.AddDays(30),
            CreatedAt     = DateTime.UtcNow
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        return wo.Id;
    }

    [Fact]
    public async Task CreatePayment_AboveInvoiceBalance_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        var woId = await SeedPayableWorkOrder(factory, invoiceTotal: 100m);

        var client = factory.CreateAuthenticatedClient("Client");
        var response = await client.PostAsJsonAsync("/api/paymentsapi", new
        {
            WorkOrderId = woId,
            Amount      = 999.99m,   // mbi balancën (118)
            Method      = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_Full_ClosesWorkOrderAndBooking()
    {
        await using var factory = new TestWebApplicationFactory();
        var woId = await SeedPayableWorkOrder(factory, invoiceTotal: 100m);

        var client = factory.CreateAuthenticatedClient("Client");
        var response = await client.PostAsJsonAsync("/api/paymentsapi", new
        {
            WorkOrderId = woId,
            Amount      = 118m,    // saktësisht totalAmount (100 + 18% = 118)
            Method      = 0
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = factory.CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var wo = await db.WorkOrders.FindAsync(woId);
        var booking = await db.Bookings.FindAsync(wo!.BookingId);

        Assert.Equal(WorkOrderStatus.Closed,       wo.Status);
        Assert.Equal(BookingStatus.Completed, booking!.Status);
    }

    [Fact]
    public async Task CreatePayment_Partial_StatusUnchanged()
    {
        await using var factory = new TestWebApplicationFactory();
        var woId = await SeedPayableWorkOrder(factory, invoiceTotal: 100m);

        var client = factory.CreateAuthenticatedClient("Client");
        await client.PostAsJsonAsync("/api/paymentsapi", new
        {
            WorkOrderId = woId,
            Amount      = 50m,
            Method      = 0
        });

        using var scope = factory.CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var wo = await db.WorkOrders.FindAsync(woId);

        // Pagesa e pjesshme — WorkOrder NUK duhet mbyllur
        Assert.NotEqual(WorkOrderStatus.Closed, wo!.Status);
    }

    [Fact]
    public async Task CreatePayment_NoInvoice_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();

        int woId;
        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var mechanic = new Mechanic { UserId = "x", ServiceCenterId = 1, IsAvailable = true, CreatedAt = DateTime.UtcNow };
            db.Mechanics.Add(mechanic);
            await db.SaveChangesAsync();
            var booking = new Booking { ClientId = TestAuthHelper.ClientId, BookingDate = DateTime.Today.AddDays(1), BookingTime = TimeSpan.FromHours(9), Status = BookingStatus.Confirmed, CreatedAt = DateTime.UtcNow };
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();
            var wo = new WorkOrder { BookingId = booking.Id, MechanicId = mechanic.Id, Status = WorkOrderStatus.ReadyForPayment, TotalCost = 100, CreatedAt = DateTime.UtcNow };
            db.WorkOrders.Add(wo);
            await db.SaveChangesAsync();
            woId = wo.Id;
        }

        var client = factory.CreateAuthenticatedClient("Client");
        var response = await client.PostAsJsonAsync("/api/paymentsapi", new
        {
            WorkOrderId = woId,
            Amount      = 100m,
            Method      = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
