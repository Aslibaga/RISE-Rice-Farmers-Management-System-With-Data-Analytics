namespace RISE.API.Dtos;

/// <summary>
/// Read-only projection for Distribution Schedule Monitoring.
/// One record per distribution transaction.
/// </summary>
public sealed record DistributionScheduleDto(
    int DistributionId,
    string FarmerName,
    DateOnly DistributionDate,
    string AssistanceType,
    string CroppingSeason,
    decimal QuantityDistributed,
    int FarmerBeneficiaries,
    string BarangayCovered,
    string DistributionStatus,
    DateTime CreatedAtUtc);
