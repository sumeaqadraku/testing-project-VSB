using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Text;
using VehicleServiceBooking.Web.Data;

namespace VehicleServiceBooking.Tests.Fixtures;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // Emri i DB-së kapet njëherë për gjithë jetën e factory — të gjitha requests dhe scopes ndajnë të njëjtën DB
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();
    // Root eksplicit — garanton ndarjen e të dhënave ndërmjet scopes brenda të njëjtës factory
    private readonly InMemoryDatabaseRoot _dbRoot = new InMemoryDatabaseRoot();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Zëvendëso JWT secret me vlerën e njohur të testimit
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"]            = TestAuthHelper.JwtSecretKey,
                ["JwtSettings:Issuer"]               = "VehicleServiceBooking",
                ["JwtSettings:Audience"]             = "VehicleServiceBooking",
                ["JwtSettings:ExpirationInMinutes"]  = "60",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Hiq DbContext-in ekzistues (InMemory ose SQL Server)
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // _dbRoot eksplicit — garanton ndarjen e të dhënave ndërmjet scopes brenda të njëjtës factory
            services.AddDbContext<ApplicationDbContext>(options =>
                options
                    .UseInMemoryDatabase(_dbName, _dbRoot)
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning)));

            // Override signing key pas gjithë konfigurimeve të tjera (evit IDX10517 config-ordering issue)
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestAuthHelper.JwtSecretKey));
                options.TokenValidationParameters.IssuerSigningKey = key;
                options.TokenValidationParameters.IssuerSigningKeys = new[] { key };
            });
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Kthen HttpClient me Authorization header të vendosur për rolin e dhënë.
    /// Rolet e vlefshme: "Manager", "Mechanic", "Client", "Client2"
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string role)
    {
        var token = role switch
        {
            "Manager"  => TestAuthHelper.ManagerToken,
            "Mechanic" => TestAuthHelper.MechanicToken,
            "Client"   => TestAuthHelper.ClientToken,
            "Client2"  => TestAuthHelper.Client2Token,
            _ => throw new ArgumentException($"Rol i panjohur: {role}")
        };

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Kthen scope për seeding të dhënash ose aksesim të shërbimeve nga teste.
    /// </summary>
    public IServiceScope CreateTestScope() => Services.CreateScope();
}
