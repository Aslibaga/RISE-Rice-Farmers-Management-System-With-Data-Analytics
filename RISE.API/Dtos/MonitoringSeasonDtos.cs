namespace RISE.API.Dtos;

public sealed record MonitoringSeasonDto(string ActiveSeason, IReadOnlyList<string> Seasons, int StartMonth, int StartYear, DateTime? StartedAtUtc);

public sealed class SetSeasonRequest
{
    public string Season { get; set; } = string.Empty;
    public int? StartMonth { get; set; }
    public int? StartYear { get; set; }
}

public sealed record SeasonHistoryDto(
    int DistributionItemId,
    string SeasonKey,
    string FarmerName,
    string ItemName,
    string CategoryName,
    string Unit,
    decimal ReceivedQuantity,
    decimal? QuantityUsed,
    decimal? QuantityStillStocked,
    decimal? QuantityNotYetUsed,
    decimal? QuantityDisposed,
    DateOnly MonitoringDate,
    string? Remarks);
