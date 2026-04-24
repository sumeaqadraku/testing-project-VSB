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
public class InvoicesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public InvoicesApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<IEnumerable<object>>> GetInvoices()
    {
        var invoices = await _context.Invoices
            .Include(i => i.WorkOrder)!.ThenInclude(wo => wo!.Booking)!.ThenInclude(b => b!.Client)
            .Include(i => i.WorkOrder)!.ThenInclude(wo => wo!.Booking)!.ThenInclude(b => b!.Vehicle)
            .Include(i => i.WorkOrder)!.ThenInclude(wo => wo!.Mechanic)!.ThenInclude(m => m!.User)
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.WorkOrderId,
                i.SubTotal,
                i.TaxAmount,
                i.TotalAmount,
                i.InvoiceDate,
                i.DueDate,
                i.CreatedAt,
                WorkOrder = i.WorkOrder == null ? null : new
                {
                    i.WorkOrder.Id,
                    i.WorkOrder.Status,
                    i.WorkOrder.TotalCost,
                    Booking = i.WorkOrder.Booking == null ? null : new
                    {
                        i.WorkOrder.Booking.Id,
                        Client = i.WorkOrder.Booking.Client == null ? null : new
                        {
                            i.WorkOrder.Booking.Client.FirstName,
                            i.WorkOrder.Booking.Client.LastName
                        },
                        Vehicle = i.WorkOrder.Booking.Vehicle == null ? null : new
                        {
                            i.WorkOrder.Booking.Vehicle.Make,
                            i.WorkOrder.Booking.Vehicle.Model,
                            i.WorkOrder.Booking.Vehicle.LicensePlate
                        }
                    }
                }
            })
            .ToListAsync();

        return Ok(invoices);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetInvoice(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.WorkOrder)!.ThenInclude(wo => wo!.Booking)!.ThenInclude(b => b!.Client)
            .Include(i => i.WorkOrder)!.ThenInclude(wo => wo!.Booking)!.ThenInclude(b => b!.Vehicle)
            .Include(i => i.WorkOrder)!.ThenInclude(wo => wo!.Mechanic)!.ThenInclude(m => m!.User)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Kontrollo autorizimin
        if (User.IsInRole("Client"))
        {
            if (invoice.WorkOrder?.Booking?.ClientId != userId)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }
        }
        else if (User.IsInRole("Mechanic") && !User.IsInRole("Manager"))
        {
            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.UserId == userId);
            if (mechanic == null || invoice.WorkOrder?.MechanicId != mechanic.Id)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }
        }

        return Ok(new
        {
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.WorkOrderId,
            invoice.SubTotal,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.InvoiceDate,
            invoice.DueDate,
            invoice.CreatedAt,
            WorkOrder = invoice.WorkOrder == null ? null : new
            {
                invoice.WorkOrder.Id,
                invoice.WorkOrder.Status,
                invoice.WorkOrder.TotalCost,
                Booking = invoice.WorkOrder.Booking == null ? null : new
                {
                    invoice.WorkOrder.Booking.Id,
                    Client = invoice.WorkOrder.Booking.Client == null ? null : new
                    {
                        invoice.WorkOrder.Booking.Client.FirstName,
                        invoice.WorkOrder.Booking.Client.LastName
                    },
                    Vehicle = invoice.WorkOrder.Booking.Vehicle == null ? null : new
                    {
                        invoice.WorkOrder.Booking.Vehicle.Make,
                        invoice.WorkOrder.Booking.Vehicle.Model,
                        invoice.WorkOrder.Booking.Vehicle.LicensePlate
                    }
                }
            }
        });
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<Invoice>> CreateInvoice([FromBody] CreateInvoiceDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var workOrder = await _context.WorkOrders
            .Include(wo => wo.Booking)
            .FirstOrDefaultAsync(wo => wo.Id == dto.WorkOrderId);

        if (workOrder == null)
        {
            return BadRequest(new { message = "WorkOrder not found." });
        }

        // Kontrollo n�se WorkOrder �sht� gati p�r invoice
        if (workOrder.Status != WorkOrderStatus.ReadyForPayment && workOrder.Status != WorkOrderStatus.Completed)
        {
            return BadRequest(new { message = "WorkOrder must be Completed or ReadyForPayment to create invoice." });
        }

        // Kontrollo n�se ekziston tashm� invoice p�r k�t� WorkOrder
        var existingInvoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.WorkOrderId == dto.WorkOrderId);

        if (existingInvoice != null)
        {
            return BadRequest(new { message = "Invoice already exists for this WorkOrder." });
        }

        // Gjenero invoice number
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{workOrder.Id:D4}";

        // Llogarit shumat
        var subTotal = workOrder.TotalCost ?? 0;
        var taxRate = dto.TaxRate ?? 0.18m; // 18% VAT default
        var taxAmount = subTotal * taxRate;
        var totalAmount = subTotal + taxAmount;

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            WorkOrderId = dto.WorkOrderId,
            SubTotal = subTotal,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30), // 30 dit� p�r pages�
            CreatedAt = DateTime.UtcNow
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
    }

    [HttpGet("workorder/{workOrderId}")]
    public async Task<ActionResult<object>> GetInvoiceByWorkOrder(int workOrderId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.WorkOrder)!.ThenInclude(wo => wo!.Booking)!.ThenInclude(b => b!.Client)
            .Include(i => i.WorkOrder)!.ThenInclude(wo => wo!.Booking)!.ThenInclude(b => b!.Vehicle)
            .FirstOrDefaultAsync(i => i.WorkOrderId == workOrderId);

        if (invoice == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Kontrollo autorizimin
        if (User.IsInRole("Client"))
        {
            if (invoice.WorkOrder?.Booking?.ClientId != userId)
            {
                return Forbid(JwtBearerDefaults.AuthenticationScheme);
            }
        }

        return Ok(new
        {
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.WorkOrderId,
            invoice.SubTotal,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.InvoiceDate,
            invoice.DueDate
        });
    }
}

public class CreateInvoiceDto
{
    public int WorkOrderId { get; set; }
    public decimal? TaxRate { get; set; }
}