using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VehicleServiceBooking.Tests.Fixtures;

namespace VehicleServiceBooking.Tests.Unit.Controllers;

// PERSONA 1 — Auth endpoints
public class AuthControllerTests
{
    [Fact]
    public async Task RegisterClient_DuplicateEmail_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();
        var payload = new { Email = "dup@test.com", Password = "Test@1234!", FirstName = "A", LastName = "B" };

        await client.PostAsJsonAsync("/api/auth/register-client", payload);
        var response = await client.PostAsJsonAsync("/api/auth/register-client", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register-client",
            new { Email = "loginwrong@test.com", Password = "Correct@1234!", FirstName = "A", LastName = "B" });

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = "loginwrong@test.com", Password = "Wrong@9999!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndUser()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register-client",
            new { Email = "loginvalid@test.com", Password = "Valid@1234!", FirstName = "Valid", LastName = "User" });

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = "loginvalid@test.com", Password = "Valid@1234!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task GetMe_NoToken_Returns401()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_ValidToken_ReturnsCurrentUser()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var reg = await client.PostAsJsonAsync("/api/auth/register-client",
            new { Email = "me@test.com", Password = "Me@12345!", FirstName = "Me", LastName = "User" });

        var body = await reg.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("me@test.com", me.GetProperty("email").GetString());
    }
}
