using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VehicleServiceBooking.Web.Models.Entities;

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    ReadyForPayment = 5,
    Closed = 6
}

public class Booking
{
    [Key]
    public int Id { get; set; }

    
    public string? ClientId { get; set; }

    
    public int? VehicleId { get; set; }

    
    public int? ServiceTypeId { get; set; }

    
    public int? ServiceCenterId { get; set; }

    public int? MechanicId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime BookingDate { get; set; }

    [Required]
    [DataType(DataType.Time)]
    public TimeSpan BookingTime { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(500)]
    public string? ClientNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(ClientId))]
    public virtual ApplicationUser? Client { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(VehicleId))]
    public virtual Vehicle? Vehicle { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(ServiceTypeId))]
    public virtual ServiceType? ServiceType { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(ServiceCenterId))]
    public virtual ServiceCenter? ServiceCenter { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(MechanicId))]
    public virtual Mechanic? Mechanic { get; set; }

    [JsonIgnore]
    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}


