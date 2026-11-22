namespace RISE.API.Dtos;

public sealed record FarmerResponseDto(
    int FarmerId,
    string FullName,
    string? FirstName,
    string? LastName,
    string? MiddleName,
    string ContactNumber,
    string? Barangay,
    string? Municipality,
    string? Province,
    string? AssociationName,
    bool IsActive);
