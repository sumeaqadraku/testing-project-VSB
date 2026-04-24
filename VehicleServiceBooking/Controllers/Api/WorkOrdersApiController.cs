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
public class WorkOrdersApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public WorkOrdersApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetWorkOrders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        IQueryable<WorkOrder> query = _context.WorkOrders
            .Include(wo => wo.Booking)!.ThenInclude(b => b!.Client)
            .Include(wo => wo.Booking)!.ThenInclude(b => b!.Vehicle)
            .Include(wo => wo.Mechanic)!.ThenInclude(m => m!.User);

        if (User.IsInRole("Manager"))
        {
            // Manager sees all
        }
        else if (User.IsInRole("Mechanic"))
        {
            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.UserId == userId);
            if (mechanic != null)
            {
                query = query.Where(wo => wo.MechanicId == mechanic.Id);
            }
            else
            {
                return Ok(new List<object>());
            }
        }
        else if (User.IsInRole("Client"))
        {
            var clientBookings = await _context.Bookings
                .Where(b => b.ClientId == userId)
                .Select(b => b.Id)
                .ToListAsync();
            query = query.Where(wo => clientBookings.Contains(wo.BookingId));
        }

        var result = await query
            .OrderByDescending(wo => wo.CreatedAt)
            .Select(wo => new
            {
                wo.Id,
                wo.BookingId,
                wo.MechanicId,
                wo.Status,
                wo.Description,
                wo.MechanicNotes,
                wo.EstimatedDurationMinutes,
                wo.ActualDurationMinutes,
                wo.LaborCost,
                wo.PartsCost,
                wo.TotalCost,
                wo.StartedAt,
                wo.CompletedAt,
                wo.CreatedAt,
                wo.UpdatedAt,
                Booking = wo.Booking == null ? null : new
                {
                    wo.Booking.Id,
                    wo.Booking.BookingDate,
                    wo.Booking.BookingTime,
                    wo.Booking.Status,
                    Client = wo.Booking.Client == null ? null : new
                    {
                        wo.Booking.Client.FirstName,
                        wo.Booking.Client.LastName
                    },
                    Vehicle = wo.Booking.Vehicle == null ? null : new
                    {
                        wo.Booking.Vehicle.Make,
                        wo.Booking.Vehicle.Model,
                        wo.Booking.Vehicle.LicensePlate,
                        wo.Booking.Vehicle.Year
                    }
                },
                Mechanic = wo.Mechanic == null || wo.Mechanic.User == null ? null : new
                {
                    wo.Mechanic.Id,
                    FirstName = wo.Mechanic.User.FirstName,
                    LastName = wo.Mechanic.User.LastName,
                    wo.Mechanic.Specialization
                }
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkOrder>> GetWorkOrder(int id)
    {
        var workOrder = await _context.WorkOrders.FindAsync(id);
        if (workOrder == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.IsInRole("Mechanic") && !User.IsInRole("Manager"))
        {
            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.UserId == userId);
            if (mechanic == null || workOrder.MechanicId != mechanic.Id)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }
        }
        else if (User.IsInRole("Client"))
        {
            var booking = await _context.Bookings.FindAsync(workOrder.BookingId);
            if (booking == null || booking.ClientId != userId)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }
        }

        return Ok(workOrder);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<WorkOrder>> CreateWorkOrder([FromBody] WorkOrder workOrder)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        workOrder.CreatedAt = DateTime.UtcNow;
        _context.WorkOrders.Add(workOrder);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWorkOrder), new { id = workOrder.Id }, workOrder);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWorkOrder(int id, [FromBody] WorkOrder workOrder)
    {
        if (id != workOrder.Id)
        {
            return BadRequest();
        }

        var existing = await _context.WorkOrders.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var oldStatus = existing.Status;

        if (User.IsInRole("Mechanic") && !User.IsInRole("Manager"))
        {
            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.UserId == userId);
            if (mechanic == null || existing.MechanicId != mechanic.Id)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }

            existing.Status = workOrder.Status;
            existing.MechanicNotes = workOrder.MechanicNotes;
            existing.ActualDurationMinutes = workOrder.ActualDurationMinutes;

            
            if (workOrder.Status == WorkOrderStatus.InProgress && !existing.StartedAt.HasValue)
            {
                existing.StartedAt = DateTime.UtcNow;
            }

            if (workOrder.Status == WorkOrderStatus.Completed && !existing.CompletedAt.HasValue)
            {
                existing.CompletedAt = DateTime.UtcNow;

                if (workOrder.ActualDurationMinutes.HasValue && mechanic.HourlyRate.HasValue)
                {
                    var hours = (decimal)workOrder.ActualDurationMinutes.Value / 60;
                    existing.LaborCost = hours * mechanic.HourlyRate.Value;
                }
            }

            existing.UpdatedAt = DateTime.UtcNow;
        }
        else if (User.IsInRole("Manager"))
        {
          
            existing.Status = workOrder.Status;
            existing.Description = workOrder.Description;
            existing.MechanicNotes = workOrder.MechanicNotes;
            existing.EstimatedDurationMinutes = workOrder.EstimatedDurationMinutes;
            existing.ActualDurationMinutes = workOrder.ActualDurationMinutes;
            existing.LaborCost = workOrder.LaborCost;
            existing.PartsCost = workOrder.PartsCost;
            existing.TotalCost = workOrder.TotalCost;
            existing.StartedAt = workOrder.StartedAt;
            existing.CompletedAt = workOrder.CompletedAt;
            existing.UpdatedAt = DateTime.UtcNow;

          
            if (workOrder.Status == WorkOrderStatus.Completed)
            {
                var laborCost = workOrder.LaborCost ?? 0;
                var partsCost = workOrder.PartsCost ?? 0;
                existing.TotalCost = laborCost + partsCost;
            }

            if (workOrder.Status == WorkOrderStatus.ReadyForPayment)
            {
                var laborCost = existing.LaborCost ?? 0;
                var partsCost = existing.PartsCost ?? 0;
                if (existing.TotalCost == null || existing.TotalCost == 0)
                {
                    existing.TotalCost = laborCost + partsCost;
                }
            }
        }
        else
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteWorkOrder(int id)
    {
        var workOrder = await _context.WorkOrders.FindAsync(id);
        if (workOrder == null)
        {
            return NotFound();
        }

        _context.WorkOrders.Remove(workOrder);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

