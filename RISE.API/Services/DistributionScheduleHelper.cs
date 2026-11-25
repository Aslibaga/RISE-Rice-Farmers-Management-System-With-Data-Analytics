namespace RISE.API.Services;

/// <summary>
/// Centralized classification helpers for Distribution Schedule Monitoring.
/// Adjust season month boundaries here only; no other module is affected.
/// </summary>
public static class DistributionScheduleHelper
{
    public const int WetStartMonth = 6;   // June
    public const int WetEndMonth = 11;    // November

    public static string GetCroppingSeason(DateOnly date) =>
        date.Month >= WetStartMonth && date.Month <= WetEndMonth ? "Wet" : "Dry";

    public static bool MatchesCroppingSeason(DateOnly date, string? season) =>
        string.IsNullOrWhiteSpace(season) ||
        string.Equals(GetCroppingSeason(date), season, StringComparison.OrdinalIgnoreCase);

    public static string ClassifyAssistanceType(string? categoryName, string? supplyName)
    {
        var text = $"{categoryName} {supplyName}";
        if (text.Contains("seed", StringComparison.OrdinalIgnoreCase)) return "Seeds";
        if (text.Contains("fertil", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("urea", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("14-14-14", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("14x14", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("npk", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("ammonium", StringComparison.OrdinalIgnoreCase)) return "Fertilizer";
        return "Other";
    }
}
