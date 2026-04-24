using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Web.Controllers.Api;

public class MechanicCreateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int ServiceCenterId { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsAvailable { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MechanicsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager; 

    public MechanicsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetMechanics()
    {
        return await _context.Mechanics
            .Include(m => m.User)
            .Select(m => new {
                m.Id,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                m.Specialization,
                m.HourlyRate,
                m.ServiceCenterId,
                m.IsAvailable
            }).ToListAsync();
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> CreateMechanic([FromBody] MechanicCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, "Mechanic");

        var mechanic = new Mechanic
        {
            UserId = user.Id,
            ServiceCenterId = dto.ServiceCenterId,
            Specialization = dto.Specialization,
            HourlyRate = dto.HourlyRate,
            IsAvailable = dto.IsAvailable,
            CreatedAt = DateTime.UtcNow
        };

        _context.Mechanics.Add(mechanic);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Sukses" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteMechanic(int id)
    {
        var mechanic = await _context.Mechanics
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mechanic == null) return NotFound();
        var user = mechanic.User;
        _context.Mechanics.Remove(mechanic);
        await _context.SaveChangesAsync(); 

        if (user != null)
        {
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest("Mekaniku u fshi, por llogaria e përdoruesit nuk u fshi dot.");
            }
        }

        return NoContent();
    }
}