using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Integration;

// PERSONA 3 - clients integration tests
public class ClientsIntegrationTests
{
    private const string Route = "/api/ClientsApi";

    // Test: Manager mund të marrë listën e plotë të clients
    [Fact]
    public async Task GetClients_AsManager_ReturnsOk()
    {
        await using var factory = new TestWebApplicationFactory();

        using (var scope = factory.CreateTestScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var clientUser = new ApplicationUser
            {
                Id = TestAuthHelper.ClientId,
                UserName = "client@test.com",
                Email = "client@test.com",
                FirstName = "Test",
                LastName = "Client",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(clientUser, "Test123!");
            Assert.True(result.Succeeded);

            await userManager.AddToRoleAsync(clientUser, "Client");
        }

        var manager = factory.CreateAuthenticatedClient("Manager");

        var response = await manager.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Test: Client nuk mund të marrë listën e plotë të clients
    [Fact]
    public async Task GetClients_AsClient_ReturnsForbidden()
    {
        await using var factory = new TestWebApplicationFactory();

        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Test: Manager fshin client me sukses
    [Fact]
    public async Task DeleteClient_AsManager_DeletesClient()
    {
        await using var factory = new TestWebApplicationFactory();

        string clientId;

        using (var scope = factory.CreateTestScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var clientUser = new ApplicationUser
            {
                UserName = "delete.client@test.com",
                Email = "delete.client@test.com",
                FirstName = "Delete",
                LastName = "Client",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(clientUser, "Test123!");
            Assert.True(result.Succeeded);

            await userManager.AddToRoleAsync(clientUser, "Client");

            clientId = clientUser.Id;
        }

        var manager = factory.CreateAuthenticatedClient("Manager");

        var response = await manager.DeleteAsync($"{Route}/{clientId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = factory.CreateTestScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var deletedClient = await userManager.FindByIdAsync(clientId);

            Assert.Null(deletedClient);
        }
    }
}