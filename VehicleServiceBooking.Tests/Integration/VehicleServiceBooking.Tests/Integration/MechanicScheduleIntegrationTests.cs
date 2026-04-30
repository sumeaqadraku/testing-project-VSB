using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Integration;

// PERSONA 3 - mechanic schedules integration tests
public class MechanicScheduleIntegrationTests
{
    private const string Route = "/api/SchedulesApi";

    // Test: Manager krijon schedule për mechanic me emër dhe mbiemër
    [Fact]
    public async Task ManagerCreatesMechanicSchedule_ReturnsOk()
    {
        await using var factory = new TestWebApplicationFactory();

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var mechanicUser = new ApplicationUser
            {
                UserName = "schedule.mechanic@test.com",
                Email = "schedule.mechanic@test.com",
                FirstName = "Schedule",
                LastName = "Mechanic",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(mechanicUser, "Test123!");
            Assert.True(result.Succeeded);

            await userManager.AddToRoleAsync(mechanicUser, "Mechanic");

            db.Mechanics.Add(new Mechanic
            {
                UserId = mechanicUser.Id,
                ServiceCenterId = 1,
                Specialization = "Engine",
                HourlyRate = 25m,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var manager = factory.CreateAuthenticatedClient("Manager");

        var response = await manager.PostAsJsonAsync(Route, new
        {
            FirstName = "Schedule",
            LastName = "Mechanic",
            DayOfWeek = 1,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0)
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var scheduleExists = await db.MechanicSchedules.AnyAsync(s =>
                s.DayOfWeek == DayOfWeek.Monday &&
                s.StartTime == new TimeSpan(9, 0, 0) &&
                s.EndTime == new TimeSpan(17, 0, 0));

            Assert.True(scheduleExists);
        }
    }

    // Test: Mechanic sheh vetëm schedule të vet
    [Fact]
    public async Task GetSchedules_AsMechanic_ReturnsOnlyOwnSchedules()
    {
        await using var factory = new TestWebApplicationFactory();

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var mechanicUser = await userManager.FindByIdAsync(TestAuthHelper.MechanicId);

            if (mechanicUser == null)
            {
                mechanicUser = new ApplicationUser
                {
                    Id = TestAuthHelper.MechanicId,
                    UserName = "mechanic@test.com",
                    Email = "mechanic@test.com",
                    FirstName = "Main",
                    LastName = "Mechanic",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(mechanicUser, "Test123!");
                Assert.True(result.Succeeded);

                await userManager.AddToRoleAsync(mechanicUser, "Mechanic");
            }

            var ownMechanic = new Mechanic
            {
                UserId = TestAuthHelper.MechanicId,
                ServiceCenterId = 1,
                Specialization = "Engine",
                HourlyRate = 25m,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            var otherUser = new ApplicationUser
            {
                UserName = "other.mechanic@test.com",
                Email = "other.mechanic@test.com",
                FirstName = "Other",
                LastName = "Mechanic",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var otherResult = await userManager.CreateAsync(otherUser, "Test123!");
            Assert.True(otherResult.Succeeded);

            await userManager.AddToRoleAsync(otherUser, "Mechanic");

            var otherMechanic = new Mechanic
            {
                UserId = otherUser.Id,
                ServiceCenterId = 1,
                Specialization = "Brakes",
                HourlyRate = 30m,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Mechanics.AddRange(ownMechanic, otherMechanic);
            await db.SaveChangesAsync();

            db.MechanicSchedules.AddRange(
                new MechanicSchedule
                {
                    MechanicId = ownMechanic.Id,
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(16, 0, 0)
                },
                new MechanicSchedule
                {
                    MechanicId = otherMechanic.Id,
                    DayOfWeek = DayOfWeek.Tuesday,
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0)
                }
            );

            await db.SaveChangesAsync();
        }

        var mechanicClient = factory.CreateAuthenticatedClient("Mechanic");

        var response = await mechanicClient.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Test: Manager fshin schedule me sukses
    [Fact]
    public async Task DeleteSchedule_AsManager_ReturnsNoContent()
    {
        await using var factory = new TestWebApplicationFactory();

        int scheduleId;

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var mechanicUser = new ApplicationUser
            {
                UserName = "delete.schedule.mechanic@test.com",
                Email = "delete.schedule.mechanic@test.com",
                FirstName = "Delete",
                LastName = "Schedule",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(mechanicUser, "Test123!");
            Assert.True(result.Succeeded);

            await userManager.AddToRoleAsync(mechanicUser, "Mechanic");

            var mechanic = new Mechanic
            {
                UserId = mechanicUser.Id,
                ServiceCenterId = 1,
                Specialization = "Oil",
                HourlyRate = 20m,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Mechanics.Add(mechanic);
            await db.SaveChangesAsync();

            var schedule = new MechanicSchedule
            {
                MechanicId = mechanic.Id,
                DayOfWeek = DayOfWeek.Friday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(15, 0, 0)
            };

            db.MechanicSchedules.Add(schedule);
            await db.SaveChangesAsync();

            scheduleId = schedule.Id;
        }

        var manager = factory.CreateAuthenticatedClient("Manager");

        var response = await manager.DeleteAsync($"{Route}/{scheduleId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var exists = await db.MechanicSchedules.AnyAsync(s => s.Id == scheduleId);

            Assert.False(exists);
        }
    }

    // Test: Client nuk mund të krijojë schedule
    [Fact]
    public async Task CreateSchedule_AsClient_ReturnsForbidden()
    {
        await using var factory = new TestWebApplicationFactory();

        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PostAsJsonAsync(Route, new
        {
            FirstName = "Any",
            LastName = "Mechanic",
            DayOfWeek = 1,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0)
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}