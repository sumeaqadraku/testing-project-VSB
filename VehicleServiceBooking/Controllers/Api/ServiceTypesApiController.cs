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
public class ServiceTypesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ServiceTypesApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ServiceType>>> GetServiceTypes([FromQuery] bool activeOnly = false)
    {
        var query = _context.ServiceTypes.AsQueryable();
        if (activeOnly)
        {
            query = query.Where(st => st.IsActive);
        }
        return await query.OrderBy(st => st.Name).ToListAsync();
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceType>> GetServiceType(int id)
    {
        var serviceType = await _context.ServiceTypes.FindAsync(id);
        if (serviceType == null)
        {
            return NotFound();
        }
        return Ok(serviceType);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<ServiceType>> CreateServiceType([FromBody] ServiceType serviceType)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        serviceType.CreatedAt = DateTime.UtcNow;
        _context.ServiceTypes.Add(serviceType);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetServiceType), new { id = serviceType.Id }, serviceType);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateServiceType(int id, [FromBody] ServiceType serviceType)
    {
        if (id != serviceType.Id)
        {
            return BadRequest();
        }

        var existing = await _context.ServiceTypes.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.Entry(existing).CurrentValues.SetValues(serviceType);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteServiceType(int id)
    {
        var serviceType = await _context.ServiceTypes.FindAsync(id);
        if (serviceType == null)
        {
            return NotFound();
        }

        _context.ServiceTypes.Remove(serviceType);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}




