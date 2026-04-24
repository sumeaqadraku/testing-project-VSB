using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VehicleServiceBooking.Web.Models.Entities;

public class Vehicle
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string LicensePlate { get; set; } = string.Empty;

    [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100")]
    public int? Year { get; set; }

    [StringLength(17)]
    public string? VIN { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    
    public string? ClientId { get; set; } = string.Empty;

    [JsonIgnore]
    [ForeignKey(nameof(ClientId))]
    public virtual ApplicationUser? Client { get; set; }

    [JsonIgnore]
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}


