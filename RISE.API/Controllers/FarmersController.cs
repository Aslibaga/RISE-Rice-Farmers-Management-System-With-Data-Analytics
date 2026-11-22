using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RISE.API.Data;
using RISE.API.Dtos;
using RISE.API.Models;

namespace RISE.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = "AnyAuthenticated")]
public class FarmersController : ControllerBase
{
    private readonly RiseDbContext _context;
    private readonly ILogger<FarmersController> _logger;

    public FarmersController(
        RiseDbContext context,
        ILogger<FarmersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FarmerResponseDto>>> GetFarmers(
        [FromQuery] string? search,
        [FromQuery] string? name,
        [FromQuery] string? barangay,
        [FromQuery] string? municipality,
        [FromQuery] string? association,
        [FromQuery] string? sort,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Farmers.AsNoTracking().AsQueryable();

        if (!includeArchived)
        {
            query = query.Where(farmer => farmer.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(farmer =>
                farmer.FullName.Contains(search) ||
                farmer.FarmerId.ToString() == search ||
                (farmer.Barangay != null && farmer.Barangay.Contains(search)) ||
                farmer.Municipality!.Contains(search) ||
                (farmer.Association != null &&
                 farmer.Association.Name.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(farmer => farmer.FullName.Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(barangay))
        {
            query = query.Where(farmer =>
                farmer.Barangay != null && farmer.Barangay.Contains(barangay));
        }

        if (!string.IsNullOrWhiteSpace(municipality))
        {
            query = query.Where(farmer =>
                farmer.Municipality != null &&
                farmer.Municipality.Contains(municipality));
        }

        if (!string.IsNullOrWhiteSpace(association))
        {
            query = query.Where(farmer =>
                farmer.Association != null &&
                farmer.Association.Name.Contains(association));
        }

        query = sort switch
        {
            "newest" => query.OrderByDescending(farmer => farmer.FarmerId),
            "oldest" => query.OrderBy(farmer => farmer.FarmerId),
            _ => query.OrderBy(farmer => farmer.FullName)
        };

        var farmers = await query
            .Select(farmer => new FarmerResponseDto(
                farmer.FarmerId,
                farmer.FullName,
                farmer.FirstName,
                farmer.LastName,
                farmer.MiddleName,
                farmer.ContactNumber,
                farmer.Barangay,
                farmer.Municipality,
                farmer.Province,
                farmer.Association == null ? null : farmer.Association.Name,
                farmer.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(farmers);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<FarmerResponseDto>>> SearchFarmers(
        [FromQuery] string? q,
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(Array.Empty<FarmerResponseDto>());
        }

        var term = q.Trim();
        var searchId = term.StartsWith("FRM-", StringComparison.OrdinalIgnoreCase) ? term[4..] : term;
        var numericId = int.TryParse(searchId, out var parsedId) && parsedId > 0 ? parsedId : (int?)null;

        var farmers = await _context.Farmers
            .AsNoTracking()
            .Where(farmer => farmer.IsActive && (
                (farmer.FirstName != null && farmer.FirstName.Contains(term)) ||
                (farmer.LastName != null && farmer.LastName.Contains(term)) ||
                (farmer.MiddleName != null && farmer.MiddleName.Contains(term)) ||
                farmer.FullName.Contains(term) ||
                (numericId.HasValue && farmer.FarmerId == numericId.Value)))
            .OrderBy(farmer => farmer.FullName)
            .ThenBy(farmer => farmer.FarmerId)
            .Take(Math.Clamp(limit, 1, 20))
            .Select(farmer => new FarmerResponseDto(
                farmer.FarmerId,
                farmer.FullName,
                farmer.FirstName,
                farmer.LastName,
                farmer.MiddleName,
                farmer.ContactNumber,
                farmer.Barangay,
                farmer.Municipality,
                farmer.Province,
                farmer.Association == null ? null : farmer.Association.Name,
                farmer.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(farmers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FarmerResponseDto>> GetFarmer(
        int id,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Farmers.AsNoTracking()
            .Where(farmer => farmer.FarmerId == id);

        if (!includeArchived)
        {
            query = query.Where(farmer => farmer.IsActive);
        }

        var farmer = await query
            .Select(entity => new FarmerResponseDto(
                entity.FarmerId,
                entity.FullName,
                entity.FirstName,
                entity.LastName,
                entity.MiddleName,
                entity.ContactNumber,
                entity.Barangay,
                entity.Municipality,
                entity.Province,
                entity.Association == null ? null : entity.Association.Name,
                entity.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return farmer is null ? NotFound() : Ok(farmer);
    }

    [HttpPost]
    public async Task<ActionResult<FarmerResponseDto>> CreateFarmer(
        FarmerWriteDto request,
        CancellationToken cancellationToken)
    {
        var displayName = Farmer.BuildDisplayName(
            request.FirstName, request.LastName, request.MiddleName, request.FullName);

        var duplicate = await _context.Farmers.AsNoTracking().AnyAsync(
            farmer => farmer.IsActive &&
                farmer.FullName == displayName &&
                farmer.ContactNumber == request.ContactNumber.Trim(),
            cancellationToken);

        if (duplicate)
        {
            return Conflict(
                "An active farmer with the same full name and contact number already exists.");
        }

        var association = await FindOrCreateAssociationAsync(
            request.AssociationName,
            cancellationToken);
        var farmer = new Farmer
        {
            FullName = displayName,
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            MiddleName = request.MiddleName?.Trim(),
            ContactNumber = request.ContactNumber.Trim(),
            Province = request.Province?.Trim(),
            Barangay = CleanOptionalValue(request.Barangay),
            Municipality = CleanOptionalValue(request.Municipality),
            Association = association,
            IsActive = true
        };

        _context.Farmers.Add(farmer);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Failed to register farmer.");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "The farmer could not be registered.");
        }

        var response = ToResponse(farmer);
        return CreatedAtAction(nameof(GetFarmer), new { id = farmer.FarmerId }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<FarmerResponseDto>> UpdateFarmer(
        int id,
        FarmerWriteDto request,
        CancellationToken cancellationToken)
    {
        var farmer = await _context.Farmers
            .FirstOrDefaultAsync(
                entity => entity.FarmerId == id && entity.IsActive,
                cancellationToken);

        if (farmer is null)
        {
            return NotFound();
        }

        var displayName = Farmer.BuildDisplayName(
            request.FirstName, request.LastName, request.MiddleName, request.FullName);

        var duplicate = await _context.Farmers.AsNoTracking().AnyAsync(
            existing => existing.IsActive &&
                existing.FarmerId != id &&
                existing.FullName == displayName &&
                existing.ContactNumber == request.ContactNumber.Trim(),
            cancellationToken);

        if (duplicate)
        {
            return Conflict(
                "Another active farmer with the same full name and contact number already exists.");
        }

        if (!string.IsNullOrWhiteSpace(request.AssociationName))
        {
            farmer.Association = await FindOrCreateAssociationAsync(
                request.AssociationName, cancellationToken);
        }
        farmer.FullName = displayName;
        farmer.FirstName = request.FirstName?.Trim();
        farmer.LastName = request.LastName?.Trim();
        farmer.MiddleName = request.MiddleName?.Trim();
        farmer.Province = request.Province?.Trim();
        farmer.ContactNumber = request.ContactNumber.Trim();
        farmer.Barangay = CleanOptionalValue(request.Barangay);
        farmer.Municipality = CleanOptionalValue(request.Municipality);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Failed to update farmer {FarmerId}.", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "The farmer could not be updated.");
        }

        return Ok(ToResponse(farmer));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> ArchiveFarmer(
        int id,
        CancellationToken cancellationToken)
    {
        var farmer = await _context.Farmers
            .FirstOrDefaultAsync(entity => entity.FarmerId == id, cancellationToken);

        if (farmer is null)
        {
            return NotFound();
        }

        if (!farmer.IsActive)
        {
            return NoContent();
        }

        farmer.IsActive = false;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Failed to archive farmer {FarmerId}.", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "The farmer could not be archived.");
        }

        return NoContent();
    }

    [HttpPatch("{id:int}/restore")]
    public async Task<ActionResult<FarmerResponseDto>> RestoreFarmer(
        int id,
        CancellationToken cancellationToken)
    {
        var farmer = await _context.Farmers
            .Include(entity => entity.Association)
            .FirstOrDefaultAsync(entity => entity.FarmerId == id, cancellationToken);

        if (farmer is null)
        {
            return NotFound();
        }

        var duplicate = await _context.Farmers.AsNoTracking().AnyAsync(
            existing => existing.IsActive &&
                existing.FarmerId != id &&
                existing.FullName == farmer.FullName &&
                existing.ContactNumber == farmer.ContactNumber,
            cancellationToken);

        if (duplicate)
        {
            return Conflict(
                "This record cannot be restored because another active farmer has the same name and contact number.");
        }

        if (!farmer.IsActive)
        {
            farmer.IsActive = true;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(exception, "Failed to restore farmer {FarmerId}.", id);
                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "The farmer could not be restored.");
            }
        }

        return Ok(ToResponse(farmer));
    }

    private static FarmerResponseDto ToResponse(Farmer farmer) =>
        new(
            farmer.FarmerId,
            farmer.FullName,
            farmer.FirstName,
            farmer.LastName,
            farmer.MiddleName,
            farmer.ContactNumber,
            farmer.Barangay,
            farmer.Municipality,
            farmer.Province,
            farmer.Association?.Name,
            farmer.IsActive);

    private async Task<FarmerAssociation?> FindOrCreateAssociationAsync(
        string? name,
        CancellationToken cancellationToken)
    {
        var normalizedName = CleanOptionalValue(name);
        if (normalizedName is null)
        {
            return null;
        }

        var existingAssociation = await _context.FarmerAssociations
            .FirstOrDefaultAsync(
                association => association.Name == normalizedName,
                cancellationToken);

        return existingAssociation ?? new FarmerAssociation { Name = normalizedName };
    }

    private static string? CleanOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
