using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VehicleServiceBooking.Web.Models.Entities;

public class ServiceCenter
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [StringLength(50)]
    public string? City { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public virtual ICollection<Mechanic> Mechanics { get; set; } = new List<Mechanic>();
    [JsonIgnore]
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    [JsonIgnore]
    public virtual ICollection<ServiceType> ServiceTypes { get; set; } = new List<ServiceType>();
}




