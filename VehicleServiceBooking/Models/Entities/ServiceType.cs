using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VehicleServiceBooking.Web.Models.Entities;

public class ServiceType
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Base price must be non-negative")]
    public decimal BasePrice { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Estimated duration must be at least 1 minute")]
    public int EstimatedDurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? ServiceCenterId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(ServiceCenterId))]
    public virtual ServiceCenter? ServiceCenter { get; set; }

    [JsonIgnore]
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}


