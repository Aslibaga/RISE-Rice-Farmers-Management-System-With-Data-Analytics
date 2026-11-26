using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RISE.API.Data;
using RISE.API.Dtos;
using RISE.API.Models;
using RISE.API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RISE.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly RiseDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        RiseDbContext context,
        IPasswordService passwordService,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpGet("admin-exists")]
    public async Task<IActionResult> AdminExists(CancellationToken cancellationToken)
    {
        var exists = await _context.UserRoles.AnyAsync(ur => ur.Role != null && ur.Role.Name == "Administrator", cancellationToken);
        return Ok(new { exists });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // First account becomes Administrator; every subsequent account becomes a DA Officer.
        // This allows many users to register while keeping a single full-access administrator.
        var adminExists = await _context.UserRoles.AnyAsync(ur => ur.Role != null && ur.Role.Name == "Administrator", cancellationToken);
        var assignedRoleName = adminExists ? "DA Officer" : "Administrator";

        if (!request.Password.Equals(request.ConfirmPassword, StringComparison.Ordinal))
        {
            return BadRequest("Password and confirm password do not match.");
        }

        // Validate password strength
        if (!IsPasswordStrong(request.Password))
        {
            return BadRequest("Password must be at least 8 characters and contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
        }

        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email.Trim(), cancellationToken))
        {
            return Conflict("An account with this email address already exists.");
        }

        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.UserName == request.Username.Trim(), cancellationToken))
        {
            return Conflict("An account with this username already exists.");
        }

        // Resolve the role to assign to the new account
        var assignedRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == assignedRoleName, cancellationToken);

        if (assignedRole is null)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "System configuration error.",
                detail: $"{assignedRoleName} role not found.");
        }

        var user = new User
        {
            UserName = request.Username.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.Username.Trim(),
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
            _logger.LogError(exception, "Failed to register user.");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "The account could not be created.");
        }

        // Assign the resolved role
        _context.UserRoles.Add(new UserRole
        {
            UserId = user.UserId,
            RoleId = assignedRole.RoleId,
            AssignedAtUtc = DateTime.UtcNow
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Failed to assign role to user {UserId}.", user.UserId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "The account could not be configured.");
        }

        _logger.LogInformation("New user registered: {UserName} ({Email})", user.UserName, user.Email);

        return Ok(new
        {
            message = "Registration successful. You can now log in.",
            user = new UserDto(
                user.UserId,
                user.UserName,
                user.Email,
                user.DisplayName,
                assignedRoleName,
                user.IsActive,
                user.CreatedAtUtc)
        });
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var input = request.UsernameOrEmail.Trim();

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u =>
                u.UserName == input || u.Email == input, cancellationToken);

        if (user is null)
        {
            // Don't reveal whether the username/email exists
            _logger.LogWarning("Login attempt for unknown user: {Input}", input);
            return Unauthorized("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user: {UserName}", user.UserName);
            return Unauthorized("Your account is inactive. Please contact the system administrator.");
        }

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user: {UserName}", user.UserName);
            return Unauthorized("Invalid username or password.");
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var roles = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name)
            .ToList();

        if (roles.Count == 0)
        {
            return Unauthorized("Your account has no assigned role. Please contact the system administrator.");
        }

        var token = _tokenService.GenerateToken(user, roles);

        // Update last login
        // Note: User model doesn't have LastLoginAt, skip for now

        _logger.LogInformation("User logged in: {UserName}", user.UserName);

        return Ok(new LoginResponseDto(
            Token: token,
            ExpiresAt: DateTime.UtcNow.AddHours(8).ToString("o"),
            User: new UserDto(
                user.UserId,
                user.UserName,
                user.Email,
                user.DisplayName,
                roles.First() ?? "DA Officer",
                user.IsActive,
                user.CreatedAtUtc,
                user.ProfilePicturePath,
                user.LastLoginAtUtc,
                user.NotifySystemAnnouncements,
                user.NotifyFarmerRecordUpdates,
                user.NotifyNewFarmerRegistration,
                user.NotifyReportGenerationUpdates,
                user.NotifyImportantSystemAlerts)));
    }

    [HttpPut("change-username")]
    public async Task<IActionResult> ChangeUsername([FromBody] ChangeUsernameRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId)) return Unauthorized();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null) return NotFound();
        if (await _context.Users.AnyAsync(u => u.UserName == request.Username.Trim() && u.UserId != userId, cancellationToken))
            return Conflict("An account with this username already exists.");
        user.UserName = request.Username.Trim();
        user.DisplayName = request.Username.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Username updated successfully." });
    }

    [HttpPut("update-email")]
    public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId)) return Unauthorized();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null) return NotFound();
        if (await _context.Users.AnyAsync(u => u.Email == request.Email.Trim() && u.UserId != userId, cancellationToken))
            return Conflict("An account with this email address already exists.");
        user.Email = request.Email.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Email updated successfully." });
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId)) return Unauthorized();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null) return NotFound();
        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return BadRequest("Current password is incorrect.");
        if (!request.NewPassword.Equals(request.ConfirmNewPassword, StringComparison.Ordinal))
            return BadRequest("New password and confirm password do not match.");
        if (!IsPasswordStrong(request.NewPassword))
            return BadRequest("Password must be at least 8 characters and contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Password changed successfully." });
    }

    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotifications([FromBody] NotificationPreferencesDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId)) return Unauthorized();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null) return NotFound();
        user.NotifySystemAnnouncements = request.NotifySystemAnnouncements;
        user.NotifyFarmerRecordUpdates = request.NotifyFarmerRecordUpdates;
        user.NotifyNewFarmerRegistration = request.NotifyNewFarmerRegistration;
        user.NotifyReportGenerationUpdates = request.NotifyReportGenerationUpdates;
        user.NotifyImportantSystemAlerts = request.NotifyImportantSystemAlerts;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Notification preferences updated." });
    }

    [HttpPost("profile-picture")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId)) return Unauthorized();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null) return NotFound();
        if (file is null || file.Length == 0) return BadRequest("No file uploaded.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".jpg" and not ".jpeg" and not ".png" and not ".webp") return BadRequest("Only JPG, JPEG, PNG, and WEBP are allowed.");
        if (file.Length > 5 * 1024 * 1024) return BadRequest("File size must be 5MB or less.");
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"{userId}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);
        using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }
        if (!string.IsNullOrWhiteSpace(user.ProfilePicturePath))
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicturePath.TrimStart('/'));
            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
        }
        user.ProfilePicturePath = $"/uploads/profiles/{fileName}";
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { profilePicturePath = user.ProfilePicturePath });
    }

    [HttpDelete("profile-picture")]
    public async Task<IActionResult> RemoveProfilePicture(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId)) return Unauthorized();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(user.ProfilePicturePath))
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicturePath.TrimStart('/'));
            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
        }
        user.ProfilePicturePath = null;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Profile picture removed." });
    }

    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId)) return Unauthorized();
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null) return NotFound();
        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return BadRequest("Current password is incorrect.");
        // Delete all operational records so the new account starts fresh
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM DistributionItems", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM UtilizationMonitorings", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM HarvestVerificationRecords", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM HarvestRecords", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM SupplyUsageRecords", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM CropMonitoringRecords", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM PlantingRecords", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Distributions", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM SupportingDocuments", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Farmers", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Supplies", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM SupplyCategories", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Suppliers", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM AssistancePrograms", cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM AuditLogs", cancellationToken);

        // Remove user roles
        _context.UserRoles.RemoveRange(user.UserRoles);
        // Remove password reset tokens
        var tokens = await _context.PasswordResetTokens.Where(t => t.UserId == userId).ToListAsync(cancellationToken);
        _context.PasswordResetTokens.RemoveRange(tokens);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Account deleted successfully." });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // With JWT, logout is handled client-side by removing the token.
        // This endpoint exists for API completeness and audit logging.
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("userId")?.Value;

        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        if (!user.IsActive)
        {
            return Unauthorized("Your account is inactive.");
        }

        var roles = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name)
            .ToList();

        return Ok(new UserDto(
            user.UserId,
            user.UserName,
            user.Email,
            user.DisplayName,
            roles.First() ?? "DA Officer",
            user.IsActive,
            user.CreatedAtUtc,
            user.ProfilePicturePath,
            user.LastLoginAtUtc,
            user.NotifySystemAnnouncements,
            user.NotifyFarmerRecordUpdates,
            user.NotifyNewFarmerRegistration,
            user.NotifyReportGenerationUpdates,
            user.NotifyImportantSystemAlerts));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim() && u.IsActive, cancellationToken);

        if (user is null)
        {
            // Don't reveal whether the email exists
            return Ok(new { message = "If an account exists for this email, a password reset link has been sent." });
        }

        // Generate reset token
        var resetToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var existingToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.UserId == user.UserId && !t.IsUsed, cancellationToken);

        if (existingToken is not null)
        {
            existingToken.IsUsed = true;
        }

        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.UserId,
            Token = resetToken,
            ExpiresAtUtc = expiresAt,
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        // In production, send an email with the reset link.
        // For development, return the token so it can be tested directly.
        var resetLink = $"{Request.Scheme}://{Request.Host}/reset-password.html?token={resetToken}";

        _logger.LogInformation("Password reset requested for user: {UserName}", user.UserName);

        return Ok(new
        {
            message = "If an account exists for this email, a password reset link has been sent.",
            resetLink // Remove in production when email is configured
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!request.NewPassword.Equals(request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            return BadRequest("Password and confirm password do not match.");
        }

        if (!IsPasswordStrong(request.NewPassword))
        {
            return BadRequest("Password must be at least 8 characters and contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
        }

        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.Token == request.Token &&
                !t.IsUsed &&
                t.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);

        if (resetToken is null)
        {
            return BadRequest("Invalid or expired reset token.");
        }

        var user = resetToken.User;

        if (!user.IsActive)
        {
            return Unauthorized("Your account is inactive.");
        }

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        resetToken.IsUsed = true;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Failed to reset password for user {UserId}.", user.UserId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "The password could not be reset.");
        }

        _logger.LogInformation("Password reset for user: {UserName}", user.UserName);

        return Ok(new { message = "Password has been reset successfully. You can now log in." });
    }

    private static bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return false;
        }

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasUpper && hasLower && hasDigit && hasSymbol;
    }
}



