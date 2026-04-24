using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VehicleServiceBooking.Web.Models.Entities;

public class MechanicSchedule
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MechanicId { get; set; }

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    public bool IsAvailable { get; set; } = true;

    [JsonIgnore]
    [ForeignKey(nameof(MechanicId))]
    public virtual Mechanic? Mechanic { get; set; }
}




