using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Web.Controllers.Api;


public class ScheduleRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SchedulesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SchedulesApiController(ApplicationDbContext context)
    {
        _context = context;
    }


    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetSchedules()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _context.MechanicSchedules
            .Include(s => s.Mechanic)
            .ThenInclude(m => m.User)
            .AsQueryable();

        if (User.IsInRole("Mechanic"))
        {
            query = query.Where(s => s.Mechanic.UserId == userId);
        }

        return await query.Select(s => new {
            s.Id,
            FullName = s.Mechanic.User.FirstName + " " + s.Mechanic.User.LastName,
            s.DayOfWeek,
            s.StartTime,
            s.EndTime
        }).ToListAsync();
    }


    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> CreateSchedule([FromBody] ScheduleRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

     
        var mechanic = await _context.Mechanics
            .Include(m => m.User)
            .FirstOrDefaultAsync(m =>
                m.User.FirstName.ToLower() == request.FirstName.ToLower() &&
                m.User.LastName.ToLower() == request.LastName.ToLower());

        if (mechanic == null)
        {
            return BadRequest("Mekaniku me këtë emër dhe mbiemër nuk u gjet në sistem.");
        }

        var schedule = new MechanicSchedule
        {
            MechanicId = mechanic.Id,
            DayOfWeek = (DayOfWeek)request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        _context.MechanicSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Orari u krijua me sukses!", scheduleId = schedule.Id });
    }

    
    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var schedule = await _context.MechanicSchedules.FindAsync(id);
        if (schedule == null) return NotFound();

        _context.MechanicSchedules.Remove(schedule);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}