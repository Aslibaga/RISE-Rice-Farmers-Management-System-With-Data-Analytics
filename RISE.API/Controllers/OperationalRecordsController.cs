using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RISE.API.Data;
using RISE.API.Dtos;
using RISE.API.Models;
using RISE.API.Services;

namespace RISE.API.Controllers;

[ApiController]
[Authorize(Policy = "AnyAuthenticated")]
public sealed class OperationalRecordsController : ControllerBase
{
    private int OwnerId => OwnerScope.UserId(User);

    private readonly RiseDbContext _context;
    private readonly ILogger<OperationalRecordsController> _logger;

    public OperationalRecordsController(
        RiseDbContext context,
        ILogger<OperationalRecordsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("api/supplies")]
    public async Task<ActionResult<IReadOnlyList<SupplyDto>>> GetSupplies(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken)
    {
        var query = _context.Supplies.AsNoTracking().Where(supply => supply.OwnerUserId == OwnerId).AsQueryable();
        if (!includeArchived)
        {
            query = query.Where(supply => supply.IsActive);
        }
        if (categoryId.HasValue)
        {
            query = query.Where(supply => supply.SupplyCategoryId == categoryId.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(supply =>
                supply.Name.Contains(search) ||
                supply.Category.Name.Contains(search));
        }

        return Ok(await query.OrderBy(supply => supply.Name)
            .Select(supply => new SupplyDto(
                supply.SupplyId, supply.SupplyCategoryId, supply.Category.Name,
                supply.Name, supply.UnitOfMeasure, supply.StockQuantity, supply.IsActive, supply.UnitCost,
                supply.ExpirationDate))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("api/supplies/options")]
    public async Task<ActionResult<SupplyOptionsDto>> GetSupplyOptions(
        CancellationToken cancellationToken)
    {
        var categories = await _context.SupplyCategories.AsNoTracking()
            .Where(item => item.OwnerUserId == OwnerId)
            .OrderBy(item => item.Name)
            .Select(item => new LookupDto(item.SupplyCategoryId, item.Name))
            .ToListAsync(cancellationToken);
        return Ok(new SupplyOptionsDto(categories));
    }

    [HttpGet("api/supply-categories")]
    public async Task<ActionResult<IReadOnlyList<SupplyCategoryDto>>> GetSupplyCategories(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = _context.SupplyCategories.AsNoTracking().Where(category => category.OwnerUserId == OwnerId).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(category => category.Name.Contains(search));
        }
        return Ok(await query.OrderBy(category => category.Name)
            .Select(category => new SupplyCategoryDto(
                category.SupplyCategoryId,
                category.Name,
                category.Description,
                category.Supplies.Count(supply => supply.OwnerUserId == OwnerId)))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("api/supply-categories/{id:int}")]
    public async Task<ActionResult<SupplyCategoryDto>> GetSupplyCategory(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _context.SupplyCategories.AsNoTracking()
            .Where(item => item.SupplyCategoryId == id && item.OwnerUserId == OwnerId)
            .Select(item => new SupplyCategoryDto(
                item.SupplyCategoryId, item.Name, item.Description, item.Supplies.Count(supply => supply.OwnerUserId == OwnerId)))
            .FirstOrDefaultAsync(cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpGet("api/supplies/{id:int}")]
    public async Task<ActionResult<SupplyDto>> GetSupply(
        int id,
        CancellationToken cancellationToken)
    {
        var supply = await _context.Supplies.AsNoTracking()
            .Where(item => item.SupplyId == id && item.OwnerUserId == OwnerId)
            .Select(item => new SupplyDto(
                item.SupplyId, item.SupplyCategoryId, item.Category.Name,
                item.Name, item.UnitOfMeasure, item.StockQuantity, item.IsActive, item.UnitCost,
                item.ExpirationDate))
            .FirstOrDefaultAsync(cancellationToken);
        return supply is null ? NotFound() : Ok(supply);
    }

    [HttpPost("api/supplies")]
    public async Task<ActionResult<SupplyDto>> CreateSupply(
        SupplyWriteDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Supply name is required.");
        }
        if (string.IsNullOrWhiteSpace(request.UnitOfMeasure))
        {
            return BadRequest("Unit of measure is required.");
        }

        // Resolve category by name (create if it does not exist yet)
        var categoryName = request.CategoryName?.Trim();
        int categoryId;
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var category = await _context.SupplyCategories
                .FirstOrDefaultAsync(c => c.Name == categoryName && c.OwnerUserId == OwnerId, cancellationToken);
            if (category is null)
            {
                category = new SupplyCategory { Name = categoryName, OwnerUserId = OwnerId };
                _context.SupplyCategories.Add(category);
                await _context.SaveChangesAsync(cancellationToken);
            }
            categoryId = category.SupplyCategoryId;
        }
        else if (request.SupplyCategoryId.HasValue)
        {
            categoryId = request.SupplyCategoryId.Value;
        }
        else
        {
            return BadRequest("Category is required.");
        }

        var supply = new Supply
        {
            SupplyCategoryId = categoryId,
            Name = request.Name.Trim(),
            UnitOfMeasure = request.UnitOfMeasure.Trim(),
            UnitCost = request.UnitCost,
            StockQuantity = request.StockQuantity ?? 0m,
            ExpirationDate = request.ExpirationDate,
            IsActive = true,
            OwnerUserId = OwnerId
        };
        _context.Supplies.Add(supply);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not create supply {SupplyName}.", request.Name);
            return Conflict("The supply could not be saved.");
        }
        var response = await GetSupply(supply.SupplyId, cancellationToken);
        return CreatedAtAction(nameof(GetSupply), new { id = supply.SupplyId }, response.Value);
    }

    [HttpPut("api/supplies/{id:int}")]
    public async Task<IActionResult> UpdateSupply(
        int id,
        SupplyWriteDto request,
        CancellationToken cancellationToken)
    {
        var supply = await _context.Supplies.FirstOrDefaultAsync(
            item => item.SupplyId == id && item.OwnerUserId == OwnerId && item.IsActive, cancellationToken);
        if (supply is null)
        {
            return NotFound();
        }

        // Resolve category by name (create if it does not exist yet)
        var categoryName = request.CategoryName?.Trim();
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var category = await _context.SupplyCategories
                .FirstOrDefaultAsync(c => c.Name == categoryName && c.OwnerUserId == OwnerId, cancellationToken);
            if (category is null)
            {
                category = new SupplyCategory { Name = categoryName, OwnerUserId = OwnerId };
                _context.SupplyCategories.Add(category);
                await _context.SaveChangesAsync(cancellationToken);
            }
            supply.SupplyCategoryId = category.SupplyCategoryId;
        }
        else if (request.SupplyCategoryId.HasValue)
        {
            supply.SupplyCategoryId = request.SupplyCategoryId.Value;
        }

        supply.Name = request.Name.Trim();
        supply.UnitOfMeasure = request.UnitOfMeasure.Trim();
        supply.UnitCost = request.UnitCost;
        // Stock is managed through the Add Stock action; preserve it when the
        // edit payload omits the quantity.
        if (request.StockQuantity.HasValue)
        {
            supply.StockQuantity = request.StockQuantity.Value;
        }
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not update supply {SupplyId}.", id);
            return Conflict("The supply could not be updated. Check that its code is unique.");
        }
        return NoContent();
    }

    [HttpDelete("api/supplies/{id:int}")]
    public async Task<IActionResult> ArchiveSupply(
        int id,
        CancellationToken cancellationToken)
    {
        var supply = await _context.Supplies.FirstOrDefaultAsync(
            item => item.SupplyId == id && item.OwnerUserId == OwnerId, cancellationToken);
        if (supply is null)
        {
            return NotFound();
        }
        supply.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("api/supplies/{id:int}/restore")]
    public async Task<IActionResult> RestoreSupply(
        int id,
        CancellationToken cancellationToken)
    {
        var supply = await _context.Supplies.FirstOrDefaultAsync(
            item => item.SupplyId == id && item.OwnerUserId == OwnerId, cancellationToken);
        if (supply is null)
        {
            return NotFound();
        }
        supply.IsActive = true;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("api/supplies/{id:int}/add-stock")]
    public async Task<IActionResult> AddStock(
        int id,
        AddStockRequest request,
        CancellationToken cancellationToken)
    {
        var supply = await _context.Supplies.FirstOrDefaultAsync(
            item => item.SupplyId == id && item.OwnerUserId == OwnerId, cancellationToken);
        if (supply is null)
        {
            return NotFound();
        }
        supply.StockQuantity += request.StockQuantity;
        if (supply.StockQuantity < 0) supply.StockQuantity = 0;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("api/audit")]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAuditLogs(
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsNoTracking().Where(log => log.UserId == OwnerId).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(log =>
                log.Action.Contains(search) ||
                log.EntityName.Contains(search) ||
                (log.EntityId != null && log.EntityId.Contains(search)));
        }
        if (from.HasValue)
        {
            query = query.Where(log => log.OccurredAtUtc >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(log => log.OccurredAtUtc <= to.Value);
        }

        return Ok(await query.OrderByDescending(log => log.OccurredAtUtc)
            .Take(500)
            .Select(log => new AuditLogDto(
                log.AuditLogId, log.UserId,
                log.User == null ? null : log.User.DisplayName,
                log.Action, log.EntityName, log.EntityId, log.OccurredAtUtc,
                log.OldValuesJson, log.NewValuesJson))
            .ToListAsync(cancellationToken));
    }

    private static string? ValidatePlantingDates(
        DateOnly plantingDate,
        DateOnly? expectedHarvestDate) =>
        expectedHarvestDate.HasValue && expectedHarvestDate.Value < plantingDate
            ? "Expected harvest date must not be earlier than the planting date."
            : null;

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
