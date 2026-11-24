using System.ComponentModel.DataAnnotations;

namespace RISE.API.Dtos;

public sealed record AssociationDto(int FarmerAssociationId, string Name, int FarmerCount);

public sealed class AssociationWriteDto
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public sealed record FarmDto(
    int FarmId,
    int FarmerId,
    string FarmerName,
    string Name,
    string Barangay,
    string Municipality,
    decimal AreaHectares,
    string? SoilType,
    bool IsActive);

public sealed class FarmWriteDto
{
    [Range(1, int.MaxValue)]
    public int FarmerId { get; set; }

    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Barangay { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Municipality { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.0001", "99999999.9999")]
    public decimal AreaHectares { get; set; }

    [StringLength(100)]
    public string? SoilType { get; set; }
}

public sealed record PlantingDto(
    int PlantingRecordId,
    int FarmId,
    string FarmName,
    string FarmerName,
    string CropName,
    string? Variety,
    string? Season,
    DateOnly PlantingDate,
    decimal AreaPlantedHectares,
    DateOnly? ExpectedHarvestDate,
    string Status);

public sealed class PlantingWriteDto
{
    [Range(1, int.MaxValue)]
    public int FarmId { get; set; }

    [Required, StringLength(100)]
    public string CropName { get; set; } = "Rice";

    [StringLength(100)]
    public string? Variety { get; set; }

    [StringLength(50)]
    public string? Season { get; set; }

    public DateOnly PlantingDate { get; set; }

    [Range(typeof(decimal), "0.0001", "99999999.9999")]
    public decimal AreaPlantedHectares { get; set; }

    public DateOnly? ExpectedHarvestDate { get; set; }

    [Required, RegularExpression("^(Growing|Harvested|Failed|Cancelled)$")]
    public string Status { get; set; } = "Growing";
}

public sealed record HarvestDto(
    int HarvestRecordId,
    int PlantingRecordId,
    string CropName,
    string FarmerName,
    DateOnly HarvestDate,
    decimal QuantityKilograms,
    decimal? MoisturePercent,
    string? Grade,
    string? Notes);

public sealed class HarvestWriteDto
{
    [Range(1, int.MaxValue)]
    public int PlantingRecordId { get; set; }

    public DateOnly HarvestDate { get; set; }

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal QuantityKilograms { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? MoisturePercent { get; set; }

    [StringLength(50)]
    public string? Grade { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public sealed record SupplyDto(
    int SupplyId,
    int SupplyCategoryId,
    string CategoryName,
    string Name,
    string UnitOfMeasure,
    decimal StockQuantity,
    bool IsActive,
    decimal UnitCost);

public sealed class SupplyWriteDto
{
    public string? CategoryName { get; set; }

    [Range(1, int.MaxValue)]
    public int? SupplyCategoryId { get; set; }

    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal UnitCost { get; set; }

    [Range(typeof(decimal), "0", "999999999999999.999")]
    public decimal? StockQuantity { get; set; }
}

public sealed class AddStockRequest
{
    [Required]
    [Range(typeof(decimal), "0", "999999999999999.999")]
    public decimal StockQuantity { get; set; }
}

public sealed record SupplyCategoryDto(
    int SupplyCategoryId,
    string Name,
    string? Description,
    int SupplyCount);

public sealed class SupplyCategoryWriteDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public sealed record SupplierDto(
    int SupplierId,
    string Name,
    string? ContactPerson,
    string? PhoneNumber,
    string? Email,
    string? Address,
    bool IsActive);

public sealed class SupplierWriteDto
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(150)]
    public string? ContactPerson { get; set; }

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }
}

public sealed record SupplyOptionsDto(
    IReadOnlyList<LookupDto> Categories,
    IReadOnlyList<LookupDto> Suppliers);

public sealed record LookupDto(int Id, string Name);

public sealed record AuditLogDto(
    long AuditLogId,
    int? UserId,
    string? UserName,
    string Action,
    string EntityName,
    string? EntityId,
    DateTime OccurredAtUtc,
    string? OldValuesJson,
    string? NewValuesJson);

public sealed class DistributionWriteDto
{
    [Range(1, int.MaxValue)]
    public int FarmerId { get; set; }

    public int? AssistanceProgramId { get; set; }

    public DateOnly DistributionDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    [Required, MinLength(1)]
    public List<DistributionItemWriteDto> Items { get; set; } = [];
}

public sealed class DistributionItemWriteDto
{
    [Range(1, int.MaxValue)]
    public int SupplyId { get; set; }

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal UnitCost { get; set; }

    [StringLength(100)]
    public string? LotNumber { get; set; }

    public DateOnly? TargetMonitoringDate { get; set; }
}

public sealed record DistributionItemDto(
    int DistributionItemId,
    int SupplyId,
    string SupplyName,
    string UnitOfMeasure,
    decimal Quantity,
    decimal UnitCost,
    string? LotNumber,
    DateOnly? TargetMonitoringDate);

public sealed record DistributionDto(
    int DistributionId,
    int FarmerId,
    string FarmerName,
    string? Barangay,
    string? Municipality,
    int? AssistanceProgramId,
    string? AssistanceProgramName,
    string? ReferenceNumber,
    DateOnly DistributionDate,
    string Status,
    string? Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<DistributionItemDto> Items);

public sealed record FarmerDistributionSummaryDto(
    int FarmerId,
    string FarmerName,
    string? FirstName,
    string? LastName,
    string? MiddleName,
    string? Barangay,
    string? Municipality,
    string? Province,
    string Status,
    int TotalDistributions,
    decimal TotalItemsReceived,
    DateOnly LastDistributionDate,
    int DistinctSuppliesCount);

public sealed record FarmerDistributionSummaryWithItemsDto(
    int FarmerId,
    string FarmerName,
    string? Barangay,
    string? Municipality,
    string? Province,
    string ContactNumber,
    int TotalDistributions,
    decimal TotalItemsReceived,
    DateOnly LastDistributionDate,
    int DistinctSuppliesCount,
    IReadOnlyList<DistributionHistoryItemDto> History);

public sealed record DistributionHistoryItemDto(
    int DistributionId,
    int SupplyId,
    string? ReferenceNumber,
    DateOnly DistributionDate,
    string Status,
    decimal Quantity,
    string SupplyName,
    string? CategoryName,
    string UnitOfMeasure,
    DateOnly? ReceivedAt,
    string? Remarks);

public sealed record DashboardSummaryDto(
    int ActiveFarmers,
    int ActiveFarms,
    decimal TotalLandAreaHectares,
    int Associations,
    int ActivePlantings,
    decimal TotalHarvestKilograms,
    int ActiveSupplies,
    int OutOfStockSupplies,
    int Distributions,
    IReadOnlyList<CountByNameDto> FarmersByMunicipality,
    IReadOnlyList<CountByNameDto> FarmersByAssociation);

public sealed record CountByNameDto(string Name, int Count);

public sealed record ProductionReportDto(
    int Year,
    int Month,
    string Municipality,
    string CropName,
    int HarvestRecordCount,
    int FarmerCount,
    decimal TotalHarvestKilograms);

public sealed record DistributionReportDto(
    int Year,
    int Month,
    string SupplyCategory,
    int DistributionCount,
    int FarmerCount,
    decimal TotalQuantity,
    decimal TotalValue);
