using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Helpers.JWT;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Unit.Helpers;

// PERSONA 1 — JwtHelper unit tests
public class JwtHelperTests
{
    private static ApplicationUser MakeUser(string id = "test-id") => new()
    {
        Id        = id,
        UserName  = "test@test.com",
        Email     = "test@test.com",
        FirstName = "Test",
        LastName  = "User"
    };

    [Fact]
    public async Task GenerateToken_ContainsCorrectRoleClaim()
    {
        await using var factory = new TestWebApplicationFactory();
        using var scope = factory.CreateTestScope();
        var jwtHelper = scope.ServiceProvider.GetRequiredService<JwtHelper>();

        var token = jwtHelper.GenerateToken(MakeUser(), ["Client"]);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var roleClaim = parsed.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

        Assert.NotNull(roleClaim);
        Assert.Equal("Client", roleClaim.Value);
    }

    [Fact]
    public async Task GenerateToken_ContainsUserIdClaim()
    {
        await using var factory = new TestWebApplicationFactory();
        using var scope = factory.CreateTestScope();
        var jwtHelper = scope.ServiceProvider.GetRequiredService<JwtHelper>();

        var user = MakeUser("my-user-id-123");
        var token = jwtHelper.GenerateToken(user, ["Manager"]);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var nameClaim = parsed.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        Assert.NotNull(nameClaim);
        Assert.Equal("my-user-id-123", nameClaim.Value);
    }

    [Fact]
    public async Task GenerateToken_ExpiresInFuture()
    {
        await using var factory = new TestWebApplicationFactory();
        using var scope = factory.CreateTestScope();
        var jwtHelper = scope.ServiceProvider.GetRequiredService<JwtHelper>();

        var token = jwtHelper.GenerateToken(MakeUser(), ["Mechanic"]);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.True(parsed.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateToken_MultipleRoles_AllPresent()
    {
        await using var factory = new TestWebApplicationFactory();
        using var scope = factory.CreateTestScope();
        var jwtHelper = scope.ServiceProvider.GetRequiredService<JwtHelper>();

        var token = jwtHelper.GenerateToken(MakeUser(), ["Manager", "Client"]);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var roles = parsed.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

        Assert.Contains("Manager", roles);
        Assert.Contains("Client", roles);
    }
}
