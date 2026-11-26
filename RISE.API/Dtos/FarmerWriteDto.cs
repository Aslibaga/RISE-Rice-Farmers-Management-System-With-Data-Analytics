using System.ComponentModel.DataAnnotations;

namespace RISE.API.Dtos;

public sealed class FarmerWriteDto
{
    [StringLength(150)]
    public string? FullName { get; set; }

    [Required, StringLength(60)]
    public string? LastName { get; set; }

    [StringLength(60)]
    public string? FirstName { get; set; }

    [StringLength(60)]
    public string? MiddleName { get; set; }

    [Required]
    [StringLength(30, MinimumLength = 7)]
    public string ContactNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Province { get; set; }

    [StringLength(100)]
    public string? Barangay { get; set; }

    [StringLength(100)]
    public string? Municipality { get; set; }
}
