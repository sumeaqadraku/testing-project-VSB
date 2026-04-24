using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Unit.Controllers;

// PERSONA 1 — Bookings endpoints
public class BookingsApiControllerTests
{
    [Fact]
    public async Task CreateBooking_PastDate_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PostAsJsonAsync("/api/bookingsapi", new
        {
            BookingDate = DateTime.Today.AddDays(-1),
            BookingTime = "10:00:00",
            Status = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_FutureDate_Returns201()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PostAsJsonAsync("/api/bookingsapi", new
        {
            BookingDate = DateTime.Today.AddDays(5),
            BookingTime = "10:00:00",
            Status = 0
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_AsClient_Within23Hours_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("Client");

        // Booking 23h from now
        var dt = DateTime.Now.AddHours(23);
        var createResp = await client.PostAsJsonAsync("/api/bookingsapi", new
        {
            BookingDate = dt.Date,
            BookingTime = dt.ToString("HH:mm:ss"),
            Status = 0
        });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var booking = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var id = booking.GetProperty("id").GetInt32();

        var cancelResp = await client.PostAsJsonAsync($"/api/bookingsapi/{id}/cancel", new { });

        Assert.Equal(HttpStatusCode.BadRequest, cancelResp.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_AsClient_After25Hours_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("Client");

        // Booking 25h from now
        var dt = DateTime.Now.AddHours(25);
        var createResp = await client.PostAsJsonAsync("/api/bookingsapi", new
        {
            BookingDate = dt.Date,
            BookingTime = dt.ToString("HH:mm:ss"),
            Status = 0
        });

        var booking = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var id = booking.GetProperty("id").GetInt32();

        var cancelResp = await client.PostAsJsonAsync($"/api/bookingsapi/{id}/cancel", new { });

        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
    }

    [Fact]
    public async Task GetBookings_Client_SeesOnlyOwnBookings()
    {
        await using var factory = new TestWebApplicationFactory();

        // Seed 2 bookings — njëra e Client1, tjetra e Client2
        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Bookings.AddRange(
                new Booking { ClientId = TestAuthHelper.ClientId,  BookingDate = DateTime.Today.AddDays(2), BookingTime = TimeSpan.FromHours(9),  Status = BookingStatus.Pending, CreatedAt = DateTime.UtcNow },
                new Booking { ClientId = TestAuthHelper.Client2Id, BookingDate = DateTime.Today.AddDays(3), BookingTime = TimeSpan.FromHours(10), Status = BookingStatus.Pending, CreatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        }

        var client = factory.CreateAuthenticatedClient("Client"); // ClientId
        var response = await client.GetAsync("/api/bookingsapi");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, list.GetArrayLength()); // vetëm booking-u i Client1
    }

    [Fact]
    public async Task CancelBooking_AsClient_OtherClientsBooking_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();

        int bookingId;
        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var b = new Booking
            {
                ClientId   = TestAuthHelper.Client2Id,
                BookingDate = DateTime.Today.AddDays(5),
                BookingTime = TimeSpan.FromHours(10),
                Status     = BookingStatus.Pending,
                CreatedAt  = DateTime.UtcNow
            };
            db.Bookings.Add(b);
            await db.SaveChangesAsync();
            bookingId = b.Id;
        }

        var client = factory.CreateAuthenticatedClient("Client"); // Client1 tries to cancel Client2's booking
        var response = await client.PostAsJsonAsync($"/api/bookingsapi/{bookingId}/cancel", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
