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
public class PaymentsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PaymentsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetPayments()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        IQueryable<Payment> query = _context.Payments
            .Include(p => p.WorkOrder)!.ThenInclude(wo => wo!.Booking)!.ThenInclude(b => b!.Client)
            .Include(p => p.WorkOrder)!.ThenInclude(wo => wo!.Booking)!.ThenInclude(b => b!.Vehicle);

        if (User.IsInRole("Manager"))
        {
            // Manager sees all
        }
        else if (User.IsInRole("Client"))
        {
            var clientBookings = await _context.Bookings
                .Where(b => b.ClientId == userId)
                .Select(b => b.Id)
                .ToListAsync();
            var clientWorkOrders = await _context.WorkOrders
                .Where(wo => clientBookings.Contains(wo.BookingId))
                .Select(wo => wo.Id)
                .ToListAsync();
            query = query.Where(p => clientWorkOrders.Contains(p.WorkOrderId));
        }
        else if (User.IsInRole("Mechanic"))
        {
            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.UserId == userId);
            if (mechanic != null)
            {
                var mechanicWorkOrders = await _context.WorkOrders
                    .Where(wo => wo.MechanicId == mechanic.Id)
                    .Select(wo => wo.Id)
                    .ToListAsync();
                query = query.Where(p => mechanicWorkOrders.Contains(p.WorkOrderId));
            }
            else
            {
                return Ok(new List<object>());
            }
        }

        var result = await query
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new
            {
                p.Id,
                p.WorkOrderId,
                p.Amount,
                p.Method,
                p.Status,
                p.TransactionId,
                p.Notes,
                p.PaymentDate,
                p.CreatedAt,
                WorkOrder = p.WorkOrder == null ? null : new
                {
                    p.WorkOrder.Id,
                    p.WorkOrder.Status,
                    Booking = p.WorkOrder.Booking == null ? null : new
                    {
                        p.WorkOrder.Booking.Id,
                        Client = p.WorkOrder.Booking.Client == null ? null : new
                        {
                            p.WorkOrder.Booking.Client.FirstName,
                            p.WorkOrder.Booking.Client.LastName
                        },
                        Vehicle = p.WorkOrder.Booking.Vehicle == null ? null : new
                        {
                            p.WorkOrder.Booking.Vehicle.Make,
                            p.WorkOrder.Booking.Vehicle.Model,
                            p.WorkOrder.Booking.Vehicle.LicensePlate
                        }
                    }
                }
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Payment>> GetPayment(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.IsInRole("Client"))
        {
            var workOrder = await _context.WorkOrders.FindAsync(payment.WorkOrderId);
            if (workOrder != null)
            {
                var booking = await _context.Bookings.FindAsync(workOrder.BookingId);
                if (booking == null || booking.ClientId != userId)
                {
                    return Forbid(JwtBearerDefaults.AuthenticationScheme);
                }
            }
        }
        else if (User.IsInRole("Mechanic") && !User.IsInRole("Manager"))
        {
            var workOrder = await _context.WorkOrders.FindAsync(payment.WorkOrderId);
            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.UserId == userId);
            if (mechanic == null || workOrder == null || workOrder.MechanicId != mechanic.Id)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }
        }

        return Ok(payment);
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var workOrder = await _context.WorkOrders
            .Include(wo => wo.Booking)
            .FirstOrDefaultAsync(wo => wo.Id == dto.WorkOrderId);

        if (workOrder == null)
        {
            return BadRequest(new { message = "WorkOrder not found." });
        }

        // Kontrollo autorizimin
        if (User.IsInRole("Client"))
        {
            if (workOrder.Booking?.ClientId != userId)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }
        }
        else if (!User.IsInRole("Manager"))
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }

        // Kontrollo nëse WorkOrder është gati për pagesë
        if (workOrder.Status != WorkOrderStatus.ReadyForPayment && workOrder.Status != WorkOrderStatus.Completed)
        {
            return BadRequest(new { message = "WorkOrder must be ReadyForPayment or Completed to make payment." });
        }

        // Kontrollo nëse ekziston Invoice për këtë WorkOrder
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.WorkOrderId == dto.WorkOrderId);

        if (invoice == null)
        {
            return BadRequest(new { message = "Invoice must be created before payment." });
        }

        // Llogarit shumën totale të paguar për këtë WorkOrder
        var totalPaid = await _context.Payments
            .Where(p => p.WorkOrderId == dto.WorkOrderId && p.Status == PaymentStatus.Completed)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        var remainingAmount = invoice.TotalAmount - totalPaid;

        if (dto.Amount > remainingAmount)
        {
            return BadRequest(new { message = $"Payment amount exceeds remaining balance. Remaining: ${remainingAmount:F2}" });
        }

        // Krijo payment
        var payment = new Payment
        {
            WorkOrderId = dto.WorkOrderId,
            Amount = dto.Amount,
            Method = dto.Method,
            Status = PaymentStatus.Pending,
            TransactionId = dto.TransactionId,
            Notes = dto.Notes,
            PaymentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Nëse pagesa është e plotë, përditëso statusin e WorkOrder në Closed
        var newTotalPaid = totalPaid + dto.Amount;
        if (newTotalPaid >= invoice.TotalAmount)
        {
            workOrder.Status = WorkOrderStatus.Closed;
            payment.Status = PaymentStatus.Completed;

            // Përditëso statusin e booking në Completed
            if (workOrder.Booking != null)
            {
                workOrder.Booking.Status = BookingStatus.Completed;
            }
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
    }

    [HttpPut("{id}/complete")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> CompletePayment(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.WorkOrder)!.ThenInclude(wo => wo!.Booking)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            return NotFound();
        }

        if (payment.Status == PaymentStatus.Completed)
        {
            return BadRequest(new { message = "Payment is already completed." });
        }

        payment.Status = PaymentStatus.Completed;
        await _context.SaveChangesAsync();

        // Kontrollo nëse të gjitha pagesat janë të kompletuara
        var workOrder = payment.WorkOrder;
        if (workOrder != null)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.WorkOrderId == workOrder.Id);

            if (invoice != null)
            {
                var totalPaid = await _context.Payments
                    .Where(p => p.WorkOrderId == workOrder.Id && p.Status == PaymentStatus.Completed)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                if (totalPaid >= invoice.TotalAmount)
                {
                    workOrder.Status = WorkOrderStatus.Closed;
                    if (workOrder.Booking != null)
                    {
                        workOrder.Booking.Status = BookingStatus.Completed;
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }

        return NoContent();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] Payment payment)
    {
        if (id != payment.Id)
        {
            return BadRequest();
        }

        var existing = await _context.Payments.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.Entry(existing).CurrentValues.SetValues(payment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeletePayment(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
        {
            return NotFound();
        }

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class CreatePaymentDto
{
    public int WorkOrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }
}