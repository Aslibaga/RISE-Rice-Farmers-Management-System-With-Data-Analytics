using System.ComponentModel.DataAnnotations;

namespace RISE.API.Dtos;

public sealed record RegisterRequestDto(
    [Required] [EmailAddress] [StringLength(254)] string Email,
    [Required] [StringLength(100, MinimumLength = 3)] string Username,
    [Required] [StringLength(200)] string OfficeDepartment,
    [Required] [StringLength(100, MinimumLength = 8)] string Password,
    [Required] string ConfirmPassword);

public sealed record LoginRequestDto(
    [Required] [StringLength(100)] string UsernameOrEmail,
    [Required] string Password);

public sealed record UserDto(
    int UserId,
    string UserName,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? ProfilePicturePath = null,
    DateTime? LastLoginAtUtc = null,
    bool NotifySystemAnnouncements = true,
    bool NotifyFarmerRecordUpdates = true,
    bool NotifyNewFarmerRegistration = true,
    bool NotifyReportGenerationUpdates = true,
    bool NotifyImportantSystemAlerts = true);

public sealed record LoginResponseDto(
    string Token,
    string ExpiresAt,
    UserDto User);

public sealed record ForgotPasswordRequestDto(
    [Required] [EmailAddress] string Email);

public sealed record ResetPasswordRequestDto(
    [Required] string Token,
    [Required] [StringLength(100, MinimumLength = 8)] string NewPassword,
    [Required] string ConfirmNewPassword);

public sealed record UpdateUserRequestDto(
    [StringLength(100, MinimumLength = 2)] string? FirstName,
    [StringLength(100, MinimumLength = 2)] string? LastName,
    [EmailAddress] [StringLength(254)] string? Email,
    [StringLength(100, MinimumLength = 3)] string? Username,
    [Phone] [StringLength(30)] string? ContactNumber,
    [StringLength(200)] string? OfficeDepartment,
    [StringLength(100)] string? Position,
    bool? IsActive,
    int? RoleId);

public sealed record CreateAdminUserRequestDto(
    [Required] [StringLength(100, MinimumLength = 2)] string FirstName,
    [Required] [StringLength(100, MinimumLength = 2)] string LastName,
    [Required] [EmailAddress] [StringLength(254)] string Email,
    [Required] [StringLength(100, MinimumLength = 3)] string Username,
    [Required] [Phone] [StringLength(30)] string ContactNumber,
    [Required] [StringLength(200)] string OfficeDepartment,
    [Required] [StringLength(100)] string Position,
    [Required] [StringLength(100, MinimumLength = 8)] string Password,
    [Required] string ConfirmPassword,
    [Required] int RoleId);

public sealed record ChangePasswordRequestDto(
    [Required] string CurrentPassword,
    [Required] [StringLength(100, MinimumLength = 8)] string NewPassword,
    [Required] string ConfirmNewPassword);

public sealed record UpdateEmailRequestDto(
    [Required] [EmailAddress] [StringLength(254)] string Email);

public sealed record ChangeUsernameRequestDto(
    [Required] [StringLength(100, MinimumLength = 3)] string Username);

public sealed record NotificationPreferencesDto(
    bool NotifySystemAnnouncements,
    bool NotifyFarmerRecordUpdates,
    bool NotifyNewFarmerRegistration,
    bool NotifyReportGenerationUpdates,
    bool NotifyImportantSystemAlerts);

public sealed record DeleteAccountRequestDto(
    [Required] string CurrentPassword);
