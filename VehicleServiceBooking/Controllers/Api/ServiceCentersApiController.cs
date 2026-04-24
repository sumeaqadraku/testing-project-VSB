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
public class ServiceCentersApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ServiceCentersApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ServiceCenter>>> GetServiceCenters()
    {
        return await _context.ServiceCenters
            .Where(sc => sc.IsActive)
            .OrderBy(sc => sc.Name)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceCenter>> GetServiceCenter(int id)
    {
        var serviceCenter = await _context.ServiceCenters.FindAsync(id);
        if (serviceCenter == null)
        {
            return NotFound();
        }
        return Ok(serviceCenter);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<ServiceCenter>> CreateServiceCenter([FromBody] ServiceCenter serviceCenter)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        serviceCenter.CreatedAt = DateTime.UtcNow;
        _context.ServiceCenters.Add(serviceCenter);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetServiceCenter), new { id = serviceCenter.Id }, serviceCenter);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateServiceCenter(int id, [FromBody] ServiceCenter serviceCenter)
    {
        if (id != serviceCenter.Id)
        {
            return BadRequest();
        }

        var existing = await _context.ServiceCenters.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.Entry(existing).CurrentValues.SetValues(serviceCenter);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteServiceCenter(int id)
    {
        var serviceCenter = await _context.ServiceCenters.FindAsync(id);
        if (serviceCenter == null)
        {
            return NotFound();
        }

        _context.ServiceCenters.Remove(serviceCenter);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}




