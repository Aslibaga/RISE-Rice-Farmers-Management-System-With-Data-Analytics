namespace RISE.API.Models;

public class Farmer
{
    public int FarmerId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? MiddleName { get; set; }

    public string DisplayName => BuildDisplayName(FirstName, LastName, MiddleName, FullName);

    public string ContactNumber { get; set; } = string.Empty;

    public string? Barangay { get; set; }

    public string? Municipality { get; set; }

    public string? Province { get; set; }

    public bool IsActive { get; set; } = true;

    public int OwnerUserId { get; set; }

    public ICollection<Distribution> Distributions { get; set; } = new List<Distribution>();

    /// <summary>
    /// Builds the display name in "Last, First [Middle]" order. When there is no last name,
    /// the first (+middle) name is used; falls back to the legacy <see cref="FullName"/>.
    /// </summary>
    public static string BuildDisplayName(
        string? firstName, string? lastName, string? middleName, string? fallback = null)
    {
        var last = lastName?.Trim() ?? string.Empty;
        var first = firstName?.Trim() ?? string.Empty;
        var middle = middleName?.Trim() ?? string.Empty;

        if (!string.IsNullOrEmpty(last))
        {
            if (string.IsNullOrEmpty(first))
            {
                return last;
            }
            return string.IsNullOrEmpty(middle)
                ? $"{last}, {first}"
                : $"{last}, {first} {middle}";
        }

        var withoutLast = string.Join(" ",
            new[] { first, middle }.Where(part => !string.IsNullOrEmpty(part)));
        return string.IsNullOrEmpty(withoutLast)
            ? fallback?.Trim() ?? string.Empty
            : withoutLast;
    }
}