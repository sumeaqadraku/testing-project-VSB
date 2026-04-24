using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class VehiclesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public VehiclesApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isManager = User.IsInRole("Manager");

        if (isManager)
        {
            return await _context.Vehicles.ToListAsync();
        }
        else
        {
            return await _context.Vehicles
                .Where(v => v.ClientId == userId)
                .ToListAsync();
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Vehicle>> GetVehicle(int id)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole("Manager") && vehicle.ClientId != userId)
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }

        return Ok(vehicle);
    }

    [HttpPost]
    [Authorize(Roles = "Client,Manager")]
    public async Task<ActionResult<Vehicle>> CreateVehicle([FromBody] Vehicle vehicle)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!User.IsInRole("Manager"))
        {
            vehicle.ClientId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }

        var exists = await _context.Vehicles
            .AnyAsync(v => v.LicensePlate == vehicle.LicensePlate);
        if (exists)
        {
            return BadRequest(new { message = "License plate already exists." });
        }

        vehicle.CreatedAt = DateTime.UtcNow;
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Client,Manager")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] Vehicle vehicle)
    {
        if (id != vehicle.Id)
        {
            return BadRequest();
        }

        var existing = await _context.Vehicles.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole("Manager") && existing.ClientId != userId)
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }

        var exists = await _context.Vehicles
            .AnyAsync(v => v.LicensePlate == vehicle.LicensePlate && v.Id != id);
        if (exists)
        {
            return BadRequest(new { message = "License plate already exists." });
        }

        if (!User.IsInRole("Manager"))
        {
            vehicle.ClientId = userId!;
        }

        _context.Entry(existing).CurrentValues.SetValues(vehicle);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Client,Manager")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole("Manager") && vehicle.ClientId != userId)
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}




