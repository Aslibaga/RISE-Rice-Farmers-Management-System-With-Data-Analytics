using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RISE.API.Data;
using RISE.API.Dtos;
using RISE.API.Models;

namespace RISE.API.Controllers;

[ApiController]
[Route("api/distributions")]
[Authorize(Policy = "AnyAuthenticated")]
public sealed class DistributionsController : ControllerBase
{
    private readonly RiseDbContext _context;
    private readonly ILogger<DistributionsController> _logger;

    public DistributionsController(
        RiseDbContext context,
        ILogger<DistributionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DistributionDto>>> GetDistributions(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? farmerId, [FromQuery] string? barangay, [FromQuery] string? assistanceType,
        CancellationToken cancellationToken)
    {
        var query = _context.Distributions.AsNoTracking().AsQueryable();
        if (farmerId.HasValue) query = query.Where(d => d.FarmerId == farmerId.Value);
        if (!string.IsNullOrWhiteSpace(barangay))
            query = query.Where(d => d.Farmer.Barangay != null && d.Farmer.Barangay.Contains(barangay));
        if (!string.IsNullOrWhiteSpace(assistanceType))
            query = query.Where(d => d.Items.Any(i => i.Supply.Name.Contains(assistanceType)));
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(distribution =>
                distribution.Farmer.FullName.Contains(search) ||
                (distribution.ReferenceNumber != null &&
                 distribution.ReferenceNumber.Contains(search)) ||
                (distribution.AssistanceProgram != null &&
                 distribution.AssistanceProgram.Name.Contains(search)) ||
                distribution.Items.Any(item => item.Supply.Name.Contains(search)));
        }
        if (status == "Pending")
            query = query.Where(d => d.Status == "Draft" || d.Status == "Approved");
        else if (status == "Received")
            query = query.Where(d => d.Status == "Distributed");
        else if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(distribution => distribution.Status == status);
        if (from.HasValue)
        {
            query = query.Where(distribution => distribution.DistributionDate >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(distribution => distribution.DistributionDate <= to.Value);
        }

        var distributions = await query
            .OrderByDescending(distribution => distribution.DistributionDate)
            .Select(distribution => new DistributionDto(
                distribution.DistributionId,
                distribution.FarmerId,
                distribution.Farmer.FullName,
                distribution.Farmer.Barangay,
                distribution.Farmer.Municipality,
                distribution.AssistanceProgramId,
                distribution.AssistanceProgram == null ? null : distribution.AssistanceProgram.Name,
                distribution.ReferenceNumber,
                distribution.DistributionDate,
                distribution.Status,
                distribution.Notes,
                distribution.CreatedAtUtc,
                distribution.Items.OrderBy(item => item.DistributionItemId)
                    .Select(item => new DistributionItemDto(
                        item.DistributionItemId,
                        item.SupplyId,
                        item.Supply.Name,
                        item.Supply.UnitOfMeasure,
                        item.Quantity,
                        item.UnitCost,
                        item.LotNumber, item.TargetMonitoringDate))
                    .ToList()))
            .ToListAsync(cancellationToken);
        return Ok(distributions);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DistributionDto>> GetDistribution(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _context.Distributions.AsNoTracking()
            .Where(distribution => distribution.DistributionId == id)
            .Select(distribution => new DistributionDto(
                distribution.DistributionId,
                distribution.FarmerId,
                distribution.Farmer.FullName,
                distribution.Farmer.Barangay,
                distribution.Farmer.Municipality,
                distribution.AssistanceProgramId,
                distribution.AssistanceProgram == null ? null : distribution.AssistanceProgram.Name,
                distribution.ReferenceNumber,
                distribution.DistributionDate,
                distribution.Status,
                distribution.Notes,
                distribution.CreatedAtUtc,
                distribution.Items.OrderBy(item => item.DistributionItemId)
                    .Select(item => new DistributionItemDto(
                        item.DistributionItemId,
                        item.SupplyId,
                        item.Supply.Name,
                        item.Supply.UnitOfMeasure,
                        item.Quantity,
                        item.UnitCost,
                        item.LotNumber, item.TargetMonitoringDate))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-farmer")]
    public async Task<ActionResult<IReadOnlyList<FarmerDistributionSummaryDto>>> GetFarmerDistributionSummaries(
        [FromQuery] string? search,
        [FromQuery] string? barangay,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        var farmerIdSearch = ParseFarmerIdSearch(search);
        var farmers = _context.Farmers
            .AsNoTracking()
            .Where(f => f.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            farmers = farmers.Where(f => f.FullName.Contains(search) ||
                (f.Barangay != null && f.Barangay.Contains(search)) ||
                (farmerIdSearch.HasValue && f.FarmerId == farmerIdSearch.Value));
        }
        if (!string.IsNullOrWhiteSpace(barangay))
        {
            farmers = farmers.Where(f => f.Barangay != null && f.Barangay.Contains(barangay));
        }

        var allFarmers = await farmers.ToListAsync(cancellationToken);

        var query = _context.Distributions
            .Include(d => d.Items).ThenInclude(i => i.Supply).ThenInclude(s => s.Category)
            .AsNoTracking()
            .Where(d => d.Farmer.IsActive)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d =>
                d.Farmer.FullName.Contains(search) ||
                (d.Farmer.Barangay != null && d.Farmer.Barangay.Contains(search)) ||
                (farmerIdSearch.HasValue && d.FarmerId == farmerIdSearch.Value));
        }
        if (!string.IsNullOrWhiteSpace(barangay))
        {
            query = query.Where(d => d.Farmer.Barangay != null && d.Farmer.Barangay.Contains(barangay));
        }

        var distributions = await query
            .OrderBy(d => d.FarmerId)
            .ThenBy(d => d.DistributionDate)
            .ToListAsync(cancellationToken);

        var distributionsByFarmer = distributions.GroupBy(d => d.FarmerId).ToDictionary(g => g.Key, g => g.ToList());

        var summaries = allFarmers
            .Select(f =>
            {
                var list = distributionsByFarmer.TryGetValue(f.FarmerId, out var farmerDists)
                    ? farmerDists
                    : new List<Distribution>();
                return new FarmerDistributionSummaryDto(
                    f.FarmerId,
                    f.FullName,
                    f.FirstName,
                    f.LastName,
                    f.MiddleName,
                    f.Barangay,
                    f.Municipality,
                    f.Province,
                    GetFarmerDistributionStatus(list),
                    list.Count,
                    list.Sum(d => d.Items.Sum(i => i.Quantity)),
                    list.Count > 0 ? list.Max(d => d.DistributionDate) : DateOnly.MinValue,
                    list.SelectMany(d => d.Items).Select(i => i.SupplyId).Distinct().Count());
            })
            .ToList();

        summaries = sort switch
        {
            "newest" => summaries
                .OrderByDescending(s => s.LastDistributionDate)
                .ThenByDescending(s => s.FarmerId)
                .ToList(),
            "oldest" => summaries
                .OrderBy(s => s.LastDistributionDate)
                .ThenBy(s => s.FarmerId)
                .ToList(),
            _ => summaries.OrderBy(s => s.FarmerName).ToList()
        };

        return Ok(summaries);
    }

    private static string GetFarmerDistributionStatus(List<Distribution> distributions)
    {
        if (distributions.Count == 0) return "Pending";
        return distributions.Any(d => d.Status == "Distributed")
            ? "Distributed"
            : distributions.Any(d => d.Status == "Approved") ? "Approved" : "Pending";
    }

    /// <summary>
    /// Accepts "FRM-001", "FRM-1", or "1" and returns the numeric farmer id.
    /// </summary>
    private static int? ParseFarmerIdSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return null;
        var value = search.Trim();
        if (value.StartsWith("FRM-", StringComparison.OrdinalIgnoreCase))
        {
            value = value[4..];
        }
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }

    [HttpGet("by-farmer/{farmerId:int}")]
    public async Task<ActionResult<FarmerDistributionSummaryWithItemsDto>> GetFarmerDistributionHistory(
        int farmerId,
        CancellationToken cancellationToken)
    {
        var farmer = await _context.Farmers
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FarmerId == farmerId && f.IsActive, cancellationToken);
        if (farmer is null) return NotFound();

        var distributions = await _context.Distributions
            .Include(d => d.Items).ThenInclude(i => i.Supply).ThenInclude(s => s.Category)
            .AsNoTracking()
            .Where(d => d.FarmerId == farmerId)
            .OrderByDescending(d => d.DistributionDate)
            .ThenByDescending(d => d.DistributionId)
            .ToListAsync(cancellationToken);

        var totalItems = distributions.Sum(d => d.Items.Sum(i => i.Quantity));
        var lastDistribution = distributions.Count > 0
            ? distributions.Max(d => d.DistributionDate)
            : DateOnly.FromDateTime(DateTime.UtcNow);

        // Build per-item history rows: one row per distribution item
        var itemIds = distributions.SelectMany(d => d.Items).Select(i => i.DistributionItemId).ToArray();
        var monitoringDates = await _context.UtilizationMonitorings
            .AsNoTracking()
            .Where(u => itemIds.Contains(u.DistributionItemId))
            .GroupBy(u => u.DistributionItemId)
            .ToDictionaryAsync(g => g.Key, g => g.Min(u => u.MonitoringDate), cancellationToken);

        var historyItems = new List<DistributionHistoryItemDto>();
        foreach (var distribution in distributions)
        {
            foreach (var item in distribution.Items.OrderBy(i => i.DistributionItemId))
            {
                DateOnly? received = null;
                if (monitoringDates.TryGetValue(item.DistributionItemId, out var monitoringDate))
                {
                    received = monitoringDate;
                }
                historyItems.Add(new DistributionHistoryItemDto(
                    distribution.DistributionId,
                    item.SupplyId,
                    distribution.ReferenceNumber,
                    distribution.DistributionDate,
                    distribution.Status,
                    item.Quantity,
                    item.Supply.Name,
                    item.Supply.Category?.Name,
                    item.Supply.UnitOfMeasure,
                    received,
                    distribution.Notes));
            }
        }

        var result = new FarmerDistributionSummaryWithItemsDto(
            farmer.FarmerId,
            farmer.FullName,
            farmer.Barangay,
            farmer.Municipality,
            farmer.Province,
            farmer.ContactNumber,
            distributions.Count,
            totalItems,
            lastDistribution,
            distributions.SelectMany(d => d.Items).Select(i => i.SupplyId).Distinct().Count(),
            historyItems);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<DistributionDto>> CreateDistribution(
        DistributionWriteDto request,
        CancellationToken cancellationToken)
    {
        var invalidReferences = await ValidateReferencesAsync(request, cancellationToken);
        if (invalidReferences is not null)
        {
            return BadRequest(invalidReferences);
        }

        var distribution = new Distribution
        {
            FarmerId = request.FarmerId,
            AssistanceProgramId = request.AssistanceProgramId,
            DistributionDate = request.DistributionDate,
            Status = "Distributed",
            Notes = Clean(request.Notes),
            ReferenceNumber = $"RISE-{Guid.NewGuid():N}".ToUpperInvariant()[..17],
            Items = request.Items.Select(item => new DistributionItem
            {
                SupplyId = item.SupplyId,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                LotNumber = Clean(item.LotNumber),
                TargetMonitoringDate = item.TargetMonitoringDate
            }).ToList()
        };
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _context.Distributions.Add(distribution);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not create distribution.");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "The distribution could not be saved.");
        }

        // Atomically deduct stock only when enough is available. The predicate in the
        // UPDATE prevents two concurrent distributions from overselling the same supply.
        foreach (var item in distribution.Items)
        {
            var deducted = await _context.Supplies
                .Where(supply => supply.SupplyId == item.SupplyId &&
                                 supply.IsActive &&
                                 supply.StockQuantity >= item.Quantity)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    supply => supply.StockQuantity,
                    supply => supply.StockQuantity - item.Quantity), cancellationToken);
            if (deducted == 0)
            {
                var supplyName = await _context.Supplies
                    .Where(supply => supply.SupplyId == item.SupplyId)
                    .Select(supply => supply.Name)
                    .FirstOrDefaultAsync(cancellationToken);
                return Conflict($"Insufficient stock for {supplyName ?? "the selected item"}.");
            }
        }

        await WriteAuditAsync(distribution, "Create+Receive", null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var result = await GetDistribution(distribution.DistributionId, cancellationToken);
        return CreatedAtAction(
            nameof(GetDistribution),
            new { id = distribution.DistributionId },
            result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateDistribution(
        int id,
        DistributionWriteDto request,
        CancellationToken cancellationToken)
    {
        var distribution = await _context.Distributions
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.DistributionId == id, cancellationToken);
        if (distribution is null)
        {
            return NotFound();
        }
        if (distribution.Status != "Draft")
        {
            return Conflict("Only draft distributions can be edited.");
        }
        var invalidReferences = await ValidateReferencesAsync(request, cancellationToken);
        if (invalidReferences is not null)
        {
            return BadRequest(invalidReferences);
        }

        var oldValues = JsonSerializer.Serialize(new
        {
            distribution.FarmerId,
            distribution.AssistanceProgramId,
            distribution.DistributionDate,
            distribution.Notes,
            ItemCount = distribution.Items.Count
        });
        distribution.FarmerId = request.FarmerId;
        distribution.AssistanceProgramId = request.AssistanceProgramId;
        distribution.DistributionDate = request.DistributionDate;
        distribution.Notes = Clean(request.Notes);
        _context.DistributionItems.RemoveRange(distribution.Items);
        distribution.Items = request.Items.Select(item => new DistributionItem
        {
            DistributionId = id,
            SupplyId = item.SupplyId,
            Quantity = item.Quantity,
            UnitCost = item.UnitCost,
            LotNumber = Clean(item.LotNumber),
            TargetMonitoringDate = item.TargetMonitoringDate
        }).ToList();
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not update distribution {DistributionId}.", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "The distribution could not be updated.");
        }
        await WriteAuditAsync(distribution, "Update", oldValues, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDistribution(
        int id,
        CancellationToken cancellationToken)
    {
        var distribution = await _context.Distributions
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.DistributionId == id, cancellationToken);
        if (distribution is null)
        {
            return NotFound();
        }
        if (distribution.Status != "Draft" && distribution.Status != "Cancelled")
        {
            return Conflict("Only draft or cancelled distributions can be deleted.");
        }
        _context.DistributionItems.RemoveRange(distribution.Items);
        _context.Distributions.Remove(distribution);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not delete distribution {DistributionId}.", id);
            return Conflict("This distribution has related records and cannot be deleted.");
        }
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Action = "Delete",
            EntityName = "Distribution",
            EntityId = id.ToString(),
            OldValuesJson = JsonSerializer.Serialize(new { distribution.Status })
        }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> ApproveDistribution(
        int id,
        CancellationToken cancellationToken)
    {
        var distribution = await _context.Distributions
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.DistributionId == id, cancellationToken);
        if (distribution is null)
        {
            return NotFound();
        }
        if (distribution.Status != "Draft")
        {
            return Conflict("Only draft distributions can be approved.");
        }
        if (distribution.Items.Count == 0)
        {
            return Conflict("A distribution must contain at least one item.");
        }

        distribution.Status = "Approved";
        await _context.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(distribution, "Approve", "{\"status\":\"Draft\"}", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/distribute")]
    public async Task<IActionResult> CompleteDistribution(
        int id,
        CancellationToken cancellationToken)
    {
        var distribution = await _context.Distributions
            .Include(item => item.Items)
            .ThenInclude(item => item.Supply)
            .FirstOrDefaultAsync(item => item.DistributionId == id, cancellationToken);
        if (distribution is null) return NotFound();
        if (distribution.Status != "Draft" && distribution.Status != "Approved")
            return Conflict("Only pending distributions can be marked as received.");
        if (distribution.Items.Count == 0)
            return Conflict("A distribution must contain at least one item.");

        var previousStatus = distribution.Status;
        distribution.Status = "Distributed";

        // Deduct stock from supplies for each distribution item
        foreach (var item in distribution.Items)
        {
            var supply = item.Supply;
            if (supply != null && supply.IsActive)
            {
                supply.StockQuantity = Math.Max(0, supply.StockQuantity - item.Quantity);
            }
        }

        _context.AuditLogs.Add(new AuditLog
        {
            Action = "Receive",
            EntityName = "Distribution",
            EntityId = distribution.DistributionId.ToString(),
            OldValuesJson = JsonSerializer.Serialize(new { Status = previousStatus }),
            NewValuesJson = JsonSerializer.Serialize(new { distribution.Status })
        });
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelDistribution(
        int id,
        CancellationToken cancellationToken)
    {
        var distribution = await _context.Distributions.FirstOrDefaultAsync(
            item => item.DistributionId == id, cancellationToken);
        if (distribution is null)
        {
            return NotFound();
        }
        if (distribution.Status == "Distributed" || distribution.Status == "Cancelled")
        {
            return Conflict("Distributed or already cancelled records cannot be cancelled.");
        }
        var previousStatus = distribution.Status;
        distribution.Status = "Cancelled";
        await _context.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(
            distribution, "Cancel",
            JsonSerializer.Serialize(new { Status = previousStatus }),
            cancellationToken);
        return NoContent();
    }

    private async Task<string?> ValidateReferencesAsync(
        DistributionWriteDto request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Farmers.AnyAsync(
                farmer => farmer.FarmerId == request.FarmerId && farmer.IsActive,
                cancellationToken))
        {
            return "Select an active farmer.";
        }
        if (request.AssistanceProgramId.HasValue &&
            !await _context.AssistancePrograms.AnyAsync(
                program => program.AssistanceProgramId == request.AssistanceProgramId &&
                    program.IsActive,
                cancellationToken))
        {
            return "Select an active assistance program.";
        }
        if (request.Items.GroupBy(item => item.SupplyId).Any(group => group.Count() > 1))
        {
            return "Each supply can only appear once in a distribution.";
        }
        var supplyIds = request.Items.Select(item => item.SupplyId).Distinct().ToArray();
        var activeSupplyCount = await _context.Supplies.CountAsync(
            supply => supply.IsActive && supplyIds.Contains(supply.SupplyId),
            cancellationToken);
        return activeSupplyCount == supplyIds.Length
            ? null
            : "Select active supplies for every distribution item.";
    }

    private async Task WriteAuditAsync(
        Distribution distribution,
        string action,
        string? oldValues,
        CancellationToken cancellationToken)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityName = "Distribution",
            EntityId = distribution.DistributionId.ToString(),
            OldValuesJson = oldValues,
            NewValuesJson = JsonSerializer.Serialize(new
            {
                distribution.Status,
                distribution.DistributionDate,
                distribution.FarmerId
            })
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
