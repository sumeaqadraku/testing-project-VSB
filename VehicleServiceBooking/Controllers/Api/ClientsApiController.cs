using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ClientsApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ClientsApiController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<IEnumerable<object>>> GetClients()
    {
        var clients = await _userManager.GetUsersInRoleAsync("Client");
        return Ok(clients.Select(c => new
        {
            c.Id,
            c.Email,
            c.FirstName,
            c.LastName,
            c.IsActive,
            c.CreatedAt
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetClient(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isManager = User.IsInRole("Manager");

        if (User.IsInRole("Client") && !isManager && id != userId)
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }

        var client = await _userManager.FindByIdAsync(id);
        if (client == null)
        {
            return NotFound();
        }

        var isClientRole = await _userManager.IsInRoleAsync(client, "Client");
        if (!isClientRole)
        {
            return NotFound();
        }

        return Ok(new
        {
            client.Id,
            client.Email,
            client.FirstName,
            client.LastName,
            client.IsActive,
            client.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient(string id, [FromBody] UpdateClientRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isManager = User.IsInRole("Manager");

        if (User.IsInRole("Client") && !isManager && id != userId)
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }

        var client = await _userManager.FindByIdAsync(id);
        if (client == null)
        {
            return NotFound();
        }

        var isClientRole = await _userManager.IsInRoleAsync(client, "Client");
        if (!isClientRole)
        {
            return NotFound();
        }

        client.FirstName = request.FirstName;
        client.LastName = request.LastName;
        if (isManager)
        {
            client.Email = request.Email;
            client.UserName = request.Email;
            client.IsActive = request.IsActive ?? client.IsActive;
        }

        var result = await _userManager.UpdateAsync(client);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> DeleteClient(string id)
    {
        var client = await _userManager.FindByIdAsync(id);
        if (client == null)
        {
            return NotFound();
        }

        var isClientRole = await _userManager.IsInRoleAsync(client, "Client");
        if (!isClientRole)
        {
            return NotFound();
        }

        var result = await _userManager.DeleteAsync(client);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        return NoContent();
    }
}

public class UpdateClientRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool? IsActive { get; set; }
}