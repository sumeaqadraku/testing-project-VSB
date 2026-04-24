using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VehicleServiceBooking.Web.Models.Entities;

public enum WorkOrderStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    ReadyForPayment = 3,
    Closed = 4
}

public class WorkOrder
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }

    [Required]
    public int MechanicId { get; set; }

    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Scheduled;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(2000)]
    public string? MechanicNotes { get; set; }

    public int? EstimatedDurationMinutes { get; set; }
    public int? ActualDurationMinutes { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Labor cost must be non-negative")]
    public decimal? LaborCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Parts cost must be non-negative")]
    public decimal? PartsCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Total cost must be non-negative")]
    public decimal? TotalCost { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(BookingId))]
    public virtual Booking? Booking { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(MechanicId))]
    public virtual Mechanic? Mechanic { get; set; }

    [JsonIgnore]
    public virtual ICollection<WorkOrderPart> WorkOrderParts { get; set; } = new List<WorkOrderPart>();
    [JsonIgnore]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}


