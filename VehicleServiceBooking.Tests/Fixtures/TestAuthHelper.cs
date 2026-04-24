using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace VehicleServiceBooking.Tests.Fixtures;

public static class TestAuthHelper
{
    // Must match the key injected in TestWebApplicationFactory.ConfigureAppConfiguration
    public const string JwtSecretKey = "TestSecretKeyForVehicleServiceBookingTestingSuite32Chars!";
    private const string Issuer = "VehicleServiceBooking";
    private const string Audience = "VehicleServiceBooking";

    // Stable IDs — use these in integration tests for ownership/access checks
    public const string ManagerId  = "manager-test-user-id-001";
    public const string MechanicId = "mechanic-test-user-id-001";
    public const string ClientId   = "client-test-user-id-001";
    public const string Client2Id  = "client-test-user-id-002";

    public static string ManagerToken  => GenerateToken(ManagerId,  "manager@test.com",  "Manager",  "Manager");
    public static string MechanicToken => GenerateToken(MechanicId, "mechanic@test.com", "Mechanic", "Mechanic");
    public static string ClientToken   => GenerateToken(ClientId,   "client@test.com",   "Client",   "Client");
    public static string Client2Token  => GenerateToken(Client2Id,  "client2@test.com",  "Client2",  "Client");

    public static string GenerateToken(string userId, string email, string username, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Email, email),
            new("FirstName", "Test"),
            new("LastName", "User"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Used in negative tests: token i skaduar duhet të kthejë 401
    public static string GenerateExpiredToken(string role)
    {
        var userId = role switch
        {
            "Manager"  => ManagerId,
            "Mechanic" => MechanicId,
            _          => ClientId,
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(-1), // i skaduar
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
