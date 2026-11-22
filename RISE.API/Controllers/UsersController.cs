using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RISE.API.Data;
using RISE.API.Dtos;
using RISE.API.Models;
using RISE.API.Services;
using System.Security.Claims;

namespace RISE.API.Controllers;

[Route("api/users")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class UsersController : ControllerBase
{
    private readonly RiseDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        RiseDbContext context,
        IPasswordService passwordService,
        ILogger<UsersController> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);

        var result = users.Select(u => new
        {
            u.UserId,
            u.UserName,
            u.Email,
            u.DisplayName,
            u.IsActive,
            u.CreatedAtUtc,
            Role = u.UserRoles.FirstOrDefault(ur => ur.Role != null)?.Role?.Name ?? "None",
            RoleId = u.UserRoles.FirstOrDefault()?.RoleId
        });

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            user.UserId,
            user.UserName,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.CreatedAtUtc,
            Role = user.UserRoles.FirstOrDefault(ur => ur.Role != null)?.Role?.Name ?? "None",
            RoleId = user.UserRoles.FirstOrDefault()?.RoleId
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateAdminUserRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!request.Password.Equals(request.ConfirmPassword ?? string.Empty, StringComparison.Ordinal))
        {
            return BadRequest("Password and confirm password do not match.");
        }

        if (await _context.Users.AnyAsync(u => u.Email == request.Email.Trim(), cancellationToken))
        {
            return Conflict("An account with this email address already exists.");
        }

        if (await _context.Users.AnyAsync(u => u.UserName == request.Username.Trim(), cancellationToken))
        {
            return Conflict("An account with this username already exists.");
        }

        var role = await _context.Roles.FindAsync(new object[] { request.RoleId }, cancellationToken);
        if (role is null)
        {
            return NotFound("Role not found.");
        }

        var user = new User
        {
            UserName = request.Username.Trim(),
            Email = request.Email.Trim(),
            DisplayName = $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            PasswordHash = _passwordService.HashPassword(request.Password),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Failed to create user.");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "The user could not be created.");
        }

        _context.UserRoles.Add(new UserRole
        {
            UserId = user.UserId,
            RoleId = role.RoleId,
            AssignedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Administrator created user: {UserName} with role {RoleName}", user.UserName, role.Name);

        return Ok(new
        {
            message = "User created successfully.",
            user = new
            {
                user.UserId,
                user.UserName,
                user.Email,
                user.DisplayName,
                user.IsActive,
                Role = role.Name
            }
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(
        int id,
        [FromBody] UpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.UserId == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        // Prevent removing the last administrator
        if (request.IsActive.HasValue && !request.IsActive.Value)
        {
            var isAdmin = user.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Administrator");
            var adminCount = await _context.UserRoles
                .Include(ur => ur.Role)
                .CountAsync(ur => ur.Role!.Name == "Administrator", cancellationToken);

            if (isAdmin && adminCount <= 1)
            {
                return BadRequest("Cannot deactivate the last Administrator account.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email.Trim() != user.Email)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email.Trim() && u.UserId != id, cancellationToken))
            {
                return Conflict("An account with this email address already exists.");
            }
            user.Email = request.Email.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username.Trim() != user.UserName)
        {
            if (await _context.Users.AnyAsync(u => u.UserName == request.Username.Trim() && u.UserId != id, cancellationToken))
            {
                return Conflict("An account with this username already exists.");
            }
            user.UserName = request.Username.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName) ||
            !string.IsNullOrWhiteSpace(request.LastName))
        {
            var firstName = string.IsNullOrWhiteSpace(request.FirstName)
                ? user.DisplayName.Split(' ').FirstOrDefault() ?? ""
                : request.FirstName.Trim();
            var lastName = string.IsNullOrWhiteSpace(request.LastName)
                ? user.DisplayName.Split(' ').Skip(1).FirstOrDefault() ?? ""
                : request.LastName.Trim();
            user.DisplayName = $"{firstName} {lastName}".Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.ContactNumber))
        {
            // ContactNumber isn't on User model, skip - it's not stored there
        }

        user.IsActive = request.IsActive ?? user.IsActive;

        // Update role if provided
        if (request.RoleId.HasValue)
        {
            var role = await _context.Roles.FindAsync(new object[] { request.RoleId.Value }, cancellationToken);
            if (role is null)
            {
                return NotFound("Role not found.");
            }

            var existingRole = user.UserRoles.FirstOrDefault();
            if (existingRole is not null)
            {
                _context.UserRoles.Remove(existingRole);
            }

            _context.UserRoles.Add(new UserRole
            {
                UserId = user.UserId,
                RoleId = role.RoleId,
                AssignedAtUtc = DateTime.UtcNow
            });
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Failed to update user {UserId}.", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "The user could not be updated.");
        }

        _logger.LogInformation("Administrator updated user {UserId}", id);

        return Ok(new { message = "User updated successfully." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? User.FindFirst("userId")?.Value;

        if (currentUserIdClaim is null || !int.TryParse(currentUserIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        if (currentUserId == id)
        {
            return BadRequest("Cannot delete your own account.");
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        // Prevent deleting the last administrator
        var isAdmin = user.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Administrator");
        if (isAdmin)
        {
            var adminCount = await _context.UserRoles
                .Include(ur => ur.Role)
                .CountAsync(ur => ur.Role!.Name == "Administrator", cancellationToken);

            if (adminCount <= 1)
            {
                return BadRequest("Cannot delete the last Administrator account.");
            }
        }

        _context.Users.Remove(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Failed to delete user {UserId}.", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "The user could not be deleted.");
        }

        _logger.LogInformation("Administrator deleted user {UserId} ({UserName})", id, user.UserName);

        return NoContent();
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .OrderBy(r => r.Name)
            .Select(r => new { r.RoleId, r.Name, r.Description })
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }
}
