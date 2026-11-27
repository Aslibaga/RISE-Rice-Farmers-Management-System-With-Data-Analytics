using System.ComponentModel.DataAnnotations;

namespace RISE.API.Dtos;

public sealed record UtilizationDto(
    int UtilizationMonitoringId,
    int DistributionItemId,
    int DistributionId,
    int FarmerId,
    string FarmerName,
    string? FirstName,
    string? LastName,
    string? MiddleName,
    string ItemName,
    string Unit,
    decimal ReceivedQuantity,
    decimal? QuantityUsed,
    decimal? QuantityStillStocked,
    decimal? QuantityDisposed,
    decimal UnreportedQuantity,
    DateOnly MonitoringDate,
    string? Remarks,
    string? CategoryName,
    DateOnly? TargetMonitoringDate);

public sealed class UtilizationWriteDto
{
    [Range(typeof(decimal), "0", "999999999999999.999")]
    public decimal? QuantityUsed { get; set; }
    [Range(typeof(decimal), "0", "999999999999999.999")]
    public decimal? QuantityStillStocked { get; set; }
    [Range(typeof(decimal), "0", "999999999999999.999")]
    public decimal? QuantityDisposed { get; set; }
    public DateOnly MonitoringDate { get; set; }
    [StringLength(2000)]
    public string? Remarks { get; set; }
}
