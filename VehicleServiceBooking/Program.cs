using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Data.Seed;
using VehicleServiceBooking.Web.Helpers.JWT;
using VehicleServiceBooking.Web.Middleware;
using VehicleServiceBooking.Web.Models.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Vehicle Service Booking API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter: Bearer {your JWT token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("VehicleServiceBookingDb")
);



builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


// JWT SETTINGS (MUST MATCH TOKEN GENERATION)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey missing");
var issuer = jwtSettings["Issuer"]
    ?? throw new InvalidOperationException("JwtSettings:Issuer missing");
var audience = jwtSettings["Audience"]
    ?? throw new InvalidOperationException("JwtSettings:Audience missing");

if (secretKey.Length < 32)
{
    throw new InvalidOperationException("JwtSettings:SecretKey must be at least 32 characters long.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.RequireHttpsMetadata = false; // ok for local dev
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

            
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT Authentication failed: {Error}", context.Exception.Message);

                
                context.NoResult();
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
              
                context.HandleResponse();

                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT Challenge triggered: {Error}", context.Error);

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var errorMessage = context.Error switch
                {
                    "invalid_token" => "Invalid or malformed token.",
                    "token_expired" => "Token has expired.",
                    _ => "Unauthorized. Valid JWT token required."
                };

                var jsonResponse = System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = new { message = errorMessage }
                });

                return context.Response.WriteAsync(jsonResponse);
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                logger.LogDebug("JWT Token validated for user: {UserId}", userId);
                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManagerOnly", p => p.RequireRole("Manager"));
    options.AddPolicy("MechanicOnly", p => p.RequireRole("Mechanic"));
    options.AddPolicy("ClientOnly", p => p.RequireRole("Client"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5174", "http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Helpers
builder.Services.AddScoped<JwtHelper>();

var app = builder.Build();


await DbInitializer.InitializeAsync(app.Services);


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ReactApp");
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

public partial class Program { }
