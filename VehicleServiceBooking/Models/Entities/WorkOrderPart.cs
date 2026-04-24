using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VehicleServiceBooking.Web.Models.Entities;

public class WorkOrderPart
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorkOrderId { get; set; }

    [Required]
    public int PartId { get; set; }

    [Required]
    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    [ForeignKey(nameof(WorkOrderId))]
    public virtual WorkOrder? WorkOrder { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(PartId))]
    public virtual Part? Part { get; set; }
}




