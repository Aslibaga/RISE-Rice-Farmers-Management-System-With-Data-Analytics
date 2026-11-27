namespace RISE.API.Models;

public class AssistanceProgram
{
    public int AssistanceProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Distribution> Distributions { get; set; } = new List<Distribution>();
}

public class SupplyCategory
{
    public int SupplyCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OwnerUserId { get; set; }
    public ICollection<Supply> Supplies { get; set; } = new List<Supply>();
}


public class Supply
{
    public int SupplyId { get; set; }
    public int SupplyCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public int OwnerUserId { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public SupplyCategory Category { get; set; } = null!;
    public ICollection<DistributionItem> DistributionItems { get; set; } = new List<DistributionItem>();
}

public class Distribution
{
    public int DistributionId { get; set; }
    public int FarmerId { get; set; }
    public int? AssistanceProgramId { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateOnly DistributionDate { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Farmer Farmer { get; set; } = null!;
    public AssistanceProgram? AssistanceProgram { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<DistributionItem> Items { get; set; } = new List<DistributionItem>();
}

public class DistributionItem
{
    public int DistributionItemId { get; set; }
    public int DistributionId { get; set; }
    public int SupplyId { get; set; }
    public decimal Quantity { get; set; }
    public DateOnly? TargetMonitoringDate { get; set; }
    public Distribution Distribution { get; set; } = null!;
    public Supply Supply { get; set; } = null!;
    public UtilizationMonitoring? UtilizationMonitoring { get; set; }
}






public class User
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public string? ProfilePicturePath { get; set; }
    public bool NotifySystemAnnouncements { get; set; } = true;
    public bool NotifyFarmerRecordUpdates { get; set; } = true;
    public bool NotifyNewFarmerRegistration { get; set; } = true;
    public bool NotifyReportGenerationUpdates { get; set; } = true;
    public bool NotifyImportantSystemAlerts { get; set; } = true;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Distribution> CreatedDistributions { get; set; } = new List<Distribution>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public class Role
{
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public class AuditLog
{
    public long AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? IpAddress { get; set; }
    public User? User { get; set; }
}


public class UtilizationMonitoring
{
    public int UtilizationMonitoringId { get; set; }
    public int DistributionItemId { get; set; }
    public decimal? QuantityUsed { get; set; }
    public decimal? QuantityStillStocked { get; set; }
    public decimal? QuantityDisposed { get; set; }
    public DateOnly MonitoringDate { get; set; }
    public string? Remarks { get; set; }
    public DistributionItem DistributionItem { get; set; } = null!;
}

public class PasswordResetToken
{
    public int PasswordResetTokenId { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; }
    public User User { get; set; } = null!;
}
