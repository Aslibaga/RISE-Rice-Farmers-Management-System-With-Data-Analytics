namespace RISE.API.Models;

/// <summary>
/// Stores archived utilization records from a completed cropping season.
/// Current-season records remain in UtilizationMonitorings.
/// </summary>
public class UtilizationSeasonRecord
{
    public int UtilizationSeasonRecordId { get; set; }
    public string SeasonKey { get; set; } = string.Empty;
    public int DistributionItemId { get; set; }
    public decimal? QuantityUsed { get; set; }
    public decimal? QuantityStillStocked { get; set; }
    public decimal? QuantityNotYetUsed { get; set; }
    public decimal? QuantityDisposed { get; set; }
    public DateOnly MonitoringDate { get; set; }
    public string? Remarks { get; set; }
    public DateTime ArchivedAtUtc { get; set; }
    public DistributionItem DistributionItem { get; set; } = null!;
}

/// <summary>
/// Simple key/value application settings (e.g. ActiveSeason).
/// </summary>
public class AppSetting
{
    public int AppSettingId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
