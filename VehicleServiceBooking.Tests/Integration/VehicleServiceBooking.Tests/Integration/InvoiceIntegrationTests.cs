using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;
using Xunit;

namespace VehicleServiceBooking.Tests.Integration;

public class InvoiceIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public InvoiceIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostInvoice_WithValidWorkOrder_CreatesInvoiceWithUniqueNumber()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient("Manager");
        var workOrderId = 1; // Supozojmë se WorkOrder 1 ekziston nga Seeding
        var invoiceDto = new { WorkOrderId = workOrderId, IssuedDate = DateTime.UtcNow };

        // Act
        var response = await client.PostAsJsonAsync("/api/invoices", invoiceDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var createdInvoice = await response.Content.ReadFromJsonAsync<Invoice>();
        
        Assert.NotNull(createdInvoice);
        Assert.False(string.IsNullOrEmpty(createdInvoice.InvoiceNumber), "Fatura duhet të ketë një numër unik.");
        Assert.Equal(workOrderId, createdInvoice.WorkOrderId);
    }

    [Fact]
    public async Task GetInvoiceByWorkOrder_ReturnsCorrectInvoice()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient("Manager");
        var workOrderId = 1;

        // Act
        var response = await client.GetAsync($"/api/invoices/workorder/{workOrderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invoice = await response.Content.ReadFromJsonAsync<Invoice>();
        Assert.Equal(workOrderId, invoice.WorkOrderId);
    }

    [Fact]
    public async Task GetAllInvoices_OnlyManagerCanAccess()
    {
        // Arrange
        var managerClient = _factory.CreateAuthenticatedClient("Manager");
        var mechanicClient = _factory.CreateAuthenticatedClient("Mechanic");

        // Act (Mechanic përpjekje për akses)
        var mechResponse = await mechanicClient.GetAsync("/api/invoices");

        // Act (Manager akses)
        var managerResponse = await managerClient.GetAsync("/api/invoices");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, mechResponse.StatusCode); // Duhet të jetë Forbidden
        Assert.Equal(HttpStatusCode.OK, managerResponse.StatusCode);     // Manager duhet të ketë akses
    }
}