using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VehicleServiceBooking.Web.Models.Entities;

public class Mechanic
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int ServiceCenterId { get; set; }

    [StringLength(100)]
    public string? Specialization { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Hourly rate must be non-negative")]
    public decimal? HourlyRate { get; set; }

    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(ServiceCenterId))]
    public virtual ServiceCenter? ServiceCenter { get; set; }

    [JsonIgnore]
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    [JsonIgnore]
    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
    [JsonIgnore]
    public virtual ICollection<MechanicSchedule> Schedules { get; set; } = new List<MechanicSchedule>();
}


