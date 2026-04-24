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
public class BookingsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BookingsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetBookings([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        IQueryable<Booking> query = _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Mechanic)!.ThenInclude(m => m.User)
            .Include(b => b.Vehicle);

        if (User.IsInRole("Manager"))
        {
            query = query.Where(b => b.Status != BookingStatus.Cancelled);
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(b => b.BookingDate >= startDate.Value && b.BookingDate <= endDate.Value);
            }
        }
        else if (User.IsInRole("Mechanic"))
        {
            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.UserId == userId);
            if (mechanic != null)
            {
                query = query.Where(b => b.MechanicId == mechanic.Id);
            }
            else
            {
                return Ok(new List<object>());
            }
        }
        else if (User.IsInRole("Client"))
        {
            query = query.Where(b => b.ClientId == userId);
        }

        var result = await query
            .OrderByDescending(b => b.BookingDate)
            .ThenBy(b => b.BookingTime)
            .Select(b => new
            {
                b.Id,
                b.BookingDate,
                b.BookingTime,
                b.Status,
                StatusName = b.Status.ToString(),
                b.ServiceCenterId,
                b.VehicleId,
                b.ServiceTypeId,
                b.MechanicId,
                Client = b.Client == null ? null : new
                {
                    b.Client.FirstName,
                    b.Client.LastName
                },
                Vehicle = b.Vehicle == null ? null : new
                {
                    LicensePlate = b.Vehicle.LicensePlate
                },
                Mechanic = b.Mechanic == null || b.Mechanic.User == null ? null : new
                {
                    FirstName = b.Mechanic.User.FirstName,
                    LastName = b.Mechanic.User.LastName,
                    b.Mechanic.Specialization
                }
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Booking>> GetBooking(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.IsInRole("Client") && booking.ClientId != userId)
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }
        else if (User.IsInRole("Mechanic"))
        {
            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.UserId == userId);
            if (mechanic == null || booking.MechanicId != mechanic.Id)
            {
                return Forbid();
            }
        }

        return Ok(booking);
    }

    [HttpPost]
    [Authorize(Roles = "Client,Manager")]
    public async Task<ActionResult<Booking>> CreateBooking([FromBody] Booking booking)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!User.IsInRole("Manager"))
        {
            booking.ClientId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }

        if (booking.MechanicId.HasValue)
        {
            var exists = await _context.Bookings
                .AnyAsync(b => b.MechanicId == booking.MechanicId &&
                              b.BookingDate.Date == booking.BookingDate.Date &&
                              b.BookingTime == booking.BookingTime &&
                              b.Status != BookingStatus.Cancelled);
            if (exists)
            {
                return BadRequest(new { message = "The selected time slot is not available." });
            }
        }

        if (booking.BookingDate.Date < DateTime.Today)
        {
            return BadRequest(new { message = "Booking date cannot be in the past." });
        }

        booking.CreatedAt = DateTime.UtcNow;
        booking.Status = BookingStatus.Pending;
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateBooking(int id, [FromBody] Booking booking)
    {
        if (id != booking.Id)
        {
            return BadRequest();
        }

        var existing = await _context.Bookings
            .Include(b => b.ServiceType)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (existing == null)
        {
            return NotFound();
        }

        var oldMechanicId = existing.MechanicId;
        var newMechanicId = booking.MechanicId;

        existing.MechanicId = booking.MechanicId;
        existing.Status = booking.Status;
        existing.UpdatedAt = DateTime.UtcNow;

        if (newMechanicId.HasValue && (!oldMechanicId.HasValue || oldMechanicId != newMechanicId))
        {
            var existingWorkOrder = await _context.WorkOrders
                .FirstOrDefaultAsync(wo => wo.BookingId == existing.Id);

            if (existingWorkOrder == null)
            {
                var workOrder = new WorkOrder
                {
                    BookingId = existing.Id,
                    MechanicId = newMechanicId.Value,
                    Status = WorkOrderStatus.Scheduled,
                    Description = existing.ServiceType != null
                        ? $"Service: {existing.ServiceType.Name}"
                        : "Vehicle service booking",
                    EstimatedDurationMinutes = existing.ServiceType?.EstimatedDurationMinutes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.WorkOrders.Add(workOrder);
            }
            else
            {
                existingWorkOrder.MechanicId = newMechanicId.Value;
                existingWorkOrder.UpdatedAt = DateTime.UtcNow;
            }

            if (existing.Status == BookingStatus.Pending)
            {
                existing.Status = BookingStatus.Confirmed;
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Client,Manager")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.IsInRole("Client") && booking.ClientId != userId)
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }

        var bookingDateTime = booking.BookingDate.Date.Add(booking.BookingTime);
        var hoursUntilBooking = (bookingDateTime - DateTime.Now).TotalHours;

        if (User.IsInRole("Client") && hoursUntilBooking < 24)
        {
            return BadRequest(new { message = "Booking cannot be cancelled. Minimum 24 hours notice required." });
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Booking cancelled successfully.", status = booking.Status, statusName = booking.Status.ToString() });
    }
}