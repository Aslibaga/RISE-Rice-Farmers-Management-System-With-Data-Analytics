namespace RISE.API.Models;

public class Farm
{
    public int FarmId { get; set; }
    public int FarmerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Barangay { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public decimal AreaHectares { get; set; }
    public string? SoilType { get; set; }
    public bool IsActive { get; set; } = true;
    public Farmer Farmer { get; set; } = null!;
    public ICollection<PlantingRecord> PlantingRecords { get; set; } = new List<PlantingRecord>();
    public ICollection<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();
}

public class AssistanceProgram
{
    public int AssistanceProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
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
    public ICollection<Supply> Supplies { get; set; } = new List<Supply>();
}

public class Supplier
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
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
    public string? Description { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public SupplyCategory Category { get; set; } = null!;
    public ICollection<DistributionItem> DistributionItems { get; set; } = new List<DistributionItem>();
    public ICollection<SupplyUsageRecord> UsageRecords { get; set; } = new List<SupplyUsageRecord>();
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
    public ICollection<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();
}

public class DistributionItem
{
    public int DistributionItemId { get; set; }
    public int DistributionId { get; set; }
    public int SupplyId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? TargetMonitoringDate { get; set; }
    public Distribution Distribution { get; set; } = null!;
    public Supply Supply { get; set; } = null!;
    public ICollection<PlantingRecord> PlantingRecords { get; set; } = new List<PlantingRecord>();
    public ICollection<SupplyUsageRecord> UsageRecords { get; set; } = new List<SupplyUsageRecord>();
    public UtilizationMonitoring? UtilizationMonitoring { get; set; }
}

public class PlantingRecord
{
    public int PlantingRecordId { get; set; }
    public int FarmId { get; set; }
    public int? SeedDistributionItemId { get; set; }
    public string CropName { get; set; } = "Rice";
    public string? Variety { get; set; }
    public string? Season { get; set; }
    public DateOnly PlantingDate { get; set; }
    public decimal AreaPlantedHectares { get; set; }
    public DateOnly? ExpectedHarvestDate { get; set; }
    public string Status { get; set; } = "Growing";
    public Farm Farm { get; set; } = null!;
    public DistributionItem? SeedDistributionItem { get; set; }
    public ICollection<CropMonitoringRecord> MonitoringRecords { get; set; } = new List<CropMonitoringRecord>();
    public ICollection<SupplyUsageRecord> SupplyUsageRecords { get; set; } = new List<SupplyUsageRecord>();
    public ICollection<HarvestRecord> HarvestRecords { get; set; } = new List<HarvestRecord>();
    public ICollection<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();
}

public class CropMonitoringRecord
{
    public int CropMonitoringRecordId { get; set; }
    public int PlantingRecordId { get; set; }
    public int? RecordedByUserId { get; set; }
    public DateTime ObservedAtUtc { get; set; }
    public string GrowthStage { get; set; } = string.Empty;
    public string? CropHealth { get; set; }
    public string? PestOrDisease { get; set; }
    public decimal? RainfallMillimeters { get; set; }
    public string? Notes { get; set; }
    public PlantingRecord PlantingRecord { get; set; } = null!;
    public User? RecordedByUser { get; set; }
}

public class SupplyUsageRecord
{
    public int SupplyUsageRecordId { get; set; }
    public int PlantingRecordId { get; set; }
    public int SupplyId { get; set; }
    public int? DistributionItemId { get; set; }
    public int? RecordedByUserId { get; set; }
    public DateOnly UsageDate { get; set; }
    public decimal QuantityUsed { get; set; }
    public string? Notes { get; set; }
    public PlantingRecord PlantingRecord { get; set; } = null!;
    public Supply Supply { get; set; } = null!;
    public DistributionItem? DistributionItem { get; set; }
    public User? RecordedByUser { get; set; }
}

public class HarvestRecord
{
    public int HarvestRecordId { get; set; }
    public int PlantingRecordId { get; set; }
    public DateOnly HarvestDate { get; set; }
    public decimal QuantityKilograms { get; set; }
    public decimal? MoisturePercent { get; set; }
    public string? Grade { get; set; }
    public string? Notes { get; set; }
    public PlantingRecord PlantingRecord { get; set; } = null!;
    public ICollection<HarvestVerificationRecord> Verifications { get; set; } = new List<HarvestVerificationRecord>();
    public ICollection<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();
}

public class HarvestVerificationRecord
{
    public int HarvestVerificationRecordId { get; set; }
    public int HarvestRecordId { get; set; }
    public int? VerifiedByUserId { get; set; }
    public DateTime VerifiedAtUtc { get; set; }
    public decimal? VerifiedQuantityKilograms { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Remarks { get; set; }
    public HarvestRecord HarvestRecord { get; set; } = null!;
    public User? VerifiedByUser { get; set; }
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
    public ICollection<CropMonitoringRecord> CropMonitoringRecords { get; set; } = new List<CropMonitoringRecord>();
    public ICollection<SupplyUsageRecord> SupplyUsageRecords { get; set; } = new List<SupplyUsageRecord>();
    public ICollection<HarvestVerificationRecord> HarvestVerifications { get; set; } = new List<HarvestVerificationRecord>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();
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

public class SupportingDocument
{
    public int SupportingDocumentId { get; set; }
    public int? FarmerId { get; set; }
    public int? FarmId { get; set; }
    public int? DistributionId { get; set; }
    public int? PlantingRecordId { get; set; }
    public int? HarvestRecordId { get; set; }
    public int? UploadedByUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public Farmer? Farmer { get; set; }
    public Farm? Farm { get; set; }
    public Distribution? Distribution { get; set; }
    public PlantingRecord? PlantingRecord { get; set; }
    public HarvestRecord? HarvestRecord { get; set; }
    public User? UploadedByUser { get; set; }
}


public class UtilizationMonitoring
{
    public int UtilizationMonitoringId { get; set; }
    public int DistributionItemId { get; set; }
    public decimal? QuantityUsed { get; set; }
    public decimal? QuantityStillStocked { get; set; }
    public decimal? QuantityNotYetUsed { get; set; }
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
