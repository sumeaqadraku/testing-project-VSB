using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Data.Seed;
using VehicleServiceBooking.Web.Helpers.JWT;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtHelper _jwtHelper;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        JwtHelper jwtHelper,
        ApplicationDbContext context,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtHelper = jwtHelper;
        _context = context;
        _logger = logger;
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    [HttpPost("register-client")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterClient([FromBody] RegisterClientRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(e => e.Description).ToArray()
            });
        }

        await EnsureRoleExistsAsync(DbInitializer.ClientRole);
        await _userManager.AddToRoleAsync(user, DbInitializer.ClientRole);

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtHelper.GenerateToken(user, roles);

        return Ok(new
        {
            token,
            user = new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                roles = roles
            }
        });
    }

    [HttpPost("register-mechanic")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> RegisterMechanic([FromBody] RegisterMechanicRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var password = string.IsNullOrEmpty(request.Password)
            ? GenerateRandomPassword()
            : request.Password;

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await EnsureRoleExistsAsync(DbInitializer.MechanicRole);
        await _userManager.AddToRoleAsync(user, DbInitializer.MechanicRole);

        var mechanic = new Mechanic
        {
            UserId = user.Id,
            ServiceCenterId = request.ServiceCenterId,
            Specialization = request.Specialization,
            HourlyRate = request.HourlyRate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Mechanics.Add(mechanic);
        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            mechanic = new
            {
                id = mechanic.Id,
                userId = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                serviceCenterId = mechanic.ServiceCenterId,
                specialization = mechanic.Specialization,
                hourlyRate = mechanic.HourlyRate,
                startTime = mechanic.StartTime,
                endTime = mechanic.EndTime,
                isAvailable = mechanic.IsAvailable
            },
            password = string.IsNullOrEmpty(request.Password) ? password : null
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtHelper.GenerateToken(user, roles);

        return Ok(new
        {
            token,
            user = new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                roles = roles
            }
        });
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            roles = roles
        });
    }

    private string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

public class RegisterClientRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class RegisterMechanicRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int ServiceCenterId { get; set; }
    public string? Specialization { get; set; }
    public decimal? HourlyRate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}




