using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<ServiceCenter> ServiceCenters { get; set; }
    public DbSet<ServiceType> ServiceTypes { get; set; }
    public DbSet<Mechanic> Mechanics { get; set; }
    public DbSet<MechanicSchedule> MechanicSchedules { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<WorkOrder> WorkOrders { get; set; }
    public DbSet<Part> Parts { get; set; }
    public DbSet<WorkOrderPart> WorkOrderParts { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Invoice> Invoices { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Vehicle>()
            .HasOne(v => v.Client)
            .WithMany(u => u.Vehicles)
            .HasForeignKey(v => v.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.Client)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.Vehicle)
            .WithMany(v => v.Bookings)
            .HasForeignKey(b => b.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.ServiceType)
            .WithMany(st => st.Bookings)
            .HasForeignKey(b => b.ServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.ServiceCenter)
            .WithMany(sc => sc.Bookings)
            .HasForeignKey(b => b.ServiceCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.Mechanic)
            .WithMany(m => m.Bookings)
            .HasForeignKey(b => b.MechanicId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Mechanic>()
            .HasOne(m => m.User)
            .WithOne(u => u.MechanicProfile)
            .HasForeignKey<Mechanic>(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Mechanic>()
            .HasOne(m => m.ServiceCenter)
            .WithMany(sc => sc.Mechanics)
            .HasForeignKey(m => m.ServiceCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MechanicSchedule>()
            .HasOne(ms => ms.Mechanic)
            .WithMany(m => m.Schedules)
            .HasForeignKey(ms => ms.MechanicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkOrder>()
            .HasOne(wo => wo.Booking)
            .WithMany(b => b.WorkOrders)
            .HasForeignKey(wo => wo.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkOrder>()
            .HasOne(wo => wo.Mechanic)
            .WithMany(m => m.WorkOrders)
            .HasForeignKey(wo => wo.MechanicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkOrderPart>()
            .HasOne(wop => wop.WorkOrder)
            .WithMany(wo => wo.WorkOrderParts)
            .HasForeignKey(wop => wop.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkOrderPart>()
            .HasOne(wop => wop.Part)
            .WithMany(p => p.WorkOrderParts)
            .HasForeignKey(wop => wop.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Payment>()
            .HasOne(p => p.WorkOrder)
            .WithMany(wo => wo.Payments)
            .HasForeignKey(p => p.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .HasOne(i => i.WorkOrder)
            .WithMany()
            .HasForeignKey(i => i.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ServiceType>()
            .HasOne(st => st.ServiceCenter)
            .WithMany(sc => sc.ServiceTypes)
            .HasForeignKey(st => st.ServiceCenterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Vehicle>()
            .HasIndex(v => v.LicensePlate)
            .IsUnique();

        builder.Entity<Invoice>()
            .HasIndex(i => i.InvoiceNumber)
            .IsUnique();
    }
}




