using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PartsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PartsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Part>>> GetParts()
    {
        return await _context.Parts
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Part>> GetPart(int id)
    {
        var part = await _context.Parts.FindAsync(id);
        if (part == null)
        {
            return NotFound();
        }
        return Ok(part);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<Part>> CreatePart([FromBody] Part part)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        part.CreatedAt = DateTime.UtcNow;
        _context.Parts.Add(part);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPart), new { id = part.Id }, part);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdatePart(int id, [FromBody] Part part)
    {
        if (id != part.Id)
        {
            return BadRequest();
        }

        var existing = await _context.Parts.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.Entry(existing).CurrentValues.SetValues(part);
        part.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeletePart(int id)
    {
        var part = await _context.Parts.FindAsync(id);
        if (part == null)
        {
            return NotFound();
        }

        _context.Parts.Remove(part);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}




