using System.ComponentModel.DataAnnotations;

namespace RISE.API.Dtos;





public sealed record SupplyDto(
    int SupplyId,
    int SupplyCategoryId,
    string CategoryName,
    string Name,
    string UnitOfMeasure,
    decimal StockQuantity,
    bool IsActive,
    decimal UnitCost,
    DateTime? ExpirationDate);

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

    public DateTime? ExpirationDate { get; set; }
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



public sealed record SupplyOptionsDto(
    IReadOnlyList<LookupDto> Categories);

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

    public DateOnly? TargetMonitoringDate { get; set; }
}

public sealed record DistributionItemDto(
    int DistributionItemId,
    int SupplyId,
    string SupplyName,
    string UnitOfMeasure,
    decimal Quantity,
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
    int ActiveSupplies,
    int OutOfStockSupplies,
    int Distributions,
    IReadOnlyList<CountByNameDto> FarmersByMunicipality,
    IReadOnlyList<CountByNameDto> FarmersByAssociation);

public sealed record CountByNameDto(string Name, int Count);


public sealed record DistributionReportDto(
    int Year,
    int Month,
    string SupplyCategory,
    int DistributionCount,
    int FarmerCount,
    decimal TotalQuantity);
