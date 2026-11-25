using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RISE.API.Data;
using RISE.API.Dtos;

namespace RISE.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = "AnyAuthenticated")]
public sealed class ReportsController : ControllerBase
{
    private readonly RiseDbContext _context;

    public ReportsController(RiseDbContext context)
    {
        _context = context;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<object>> GetDashboard(CancellationToken cancellationToken)
    {
        var totalFarmers = await _context.Farmers.CountAsync(f => f.IsActive, cancellationToken);
        var receivedFarmerIds = _context.Distributions
            .Where(d => d.Status == "Distributed" && d.Farmer.IsActive)
            .Select(d => d.FarmerId).Distinct();
        var receivedFarmers = await receivedFarmerIds.CountAsync(cancellationToken);
        var distributedItems = _context.DistributionItems
            .Where(i => i.Distribution.Status == "Distributed");
        var distributed = await distributedItems.SumAsync(i => (decimal?)i.Quantity, cancellationToken) ?? 0;
        var used = await _context.UtilizationMonitorings.SumAsync(x => x.QuantityUsed ?? 0m, cancellationToken);
        var stocked = await _context.UtilizationMonitorings.SumAsync(x => x.QuantityStillStocked ?? 0m, cancellationToken);
        var notYetUsed = await _context.UtilizationMonitorings.SumAsync(x => x.QuantityNotYetUsed ?? 0m, cancellationToken);
        var disposed = await _context.UtilizationMonitorings.SumAsync(x => x.QuantityDisposed ?? 0m, cancellationToken);
        var unreported = await distributedItems
            .SumAsync(i => (decimal?)i.Quantity - (decimal?)(
                i.UtilizationMonitoring == null ? 0m :
                (i.UtilizationMonitoring.QuantityUsed ?? 0m) +
                (i.UtilizationMonitoring.QuantityStillStocked ?? 0m) +
                (i.UtilizationMonitoring.QuantityNotYetUsed ?? 0m) +
                (i.UtilizationMonitoring.QuantityDisposed ?? 0m)), cancellationToken) ?? 0m;

        var farmersByStatus = new[]
        {
            new { name = "Received", count = receivedFarmers },
            new { name = "Not yet received", count = Math.Max(0, totalFarmers - receivedFarmers) }
        };
        var byType = await distributedItems.GroupBy(i => i.Supply.Name)
            .Select(g => new { name = g.Key, quantity = g.Sum(i => i.Quantity) })
            .OrderByDescending(x => x.quantity).ToListAsync(cancellationToken);
        var byBarangay = await _context.Distributions.Where(d => d.Status == "Distributed")
            .GroupBy(d => d.Farmer.Barangay ?? "Unspecified")
            .Select(g => new { name = g.Key, count = g.Select(x => x.FarmerId).Distinct().Count() })
            .OrderByDescending(x => x.count).ToListAsync(cancellationToken);

        return Ok(new {
            totalFarmers, receivedFarmers, notReceivedFarmers = Math.Max(0, totalFarmers - receivedFarmers),
            totalDistributed = distributed, quantityUsed = used, quantityStillStocked = stocked,
            quantityNotYetUsed = notYetUsed, quantityDisposed = disposed,
            quantityUnreported = Math.Max(0, unreported),
            farmersByStatus, distributionByType = byType, distributionByBarangay = byBarangay,
            utilizationByStatus = new[] {
                new { name = "Used", quantity = used },
                new { name = "Still stocked", quantity = stocked },
                new { name = "Not yet used", quantity = notYetUsed },
                new { name = "Disposed", quantity = disposed },
                new { name = "Unreported", quantity = Math.Max(0, unreported) }
            }
        });
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetAssistanceSummary(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] string? barangay, [FromQuery] string? assistanceType,
        CancellationToken ct)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            return BadRequest("The start date must be on or before the end date.");
        var distributions = _context.Distributions.Where(d => d.Status == "Distributed");
        if (from.HasValue) distributions = distributions.Where(d => d.DistributionDate >= from.Value);
        if (to.HasValue) distributions = distributions.Where(d => d.DistributionDate <= to.Value);
        if (!string.IsNullOrWhiteSpace(barangay))
            distributions = distributions.Where(d => d.Farmer.Barangay != null && d.Farmer.Barangay.Contains(barangay));
        var items = distributions.SelectMany(d => d.Items);
        if (!string.IsNullOrWhiteSpace(assistanceType))
            items = items.Where(i => i.Supply.Name.Contains(assistanceType));
        var records = await items
            .OrderBy(i => i.Distribution.Farmer.FullName)
            .ThenBy(i => i.Supply.Name)
            .ThenBy(i => i.Distribution.DistributionDate)
            .Select(i => new {
            i.DistributionItemId, i.DistributionId, i.Distribution.DistributionDate,
            Farmer = i.Distribution.Farmer.FullName, Barangay = i.Distribution.Farmer.Barangay,
            Item = i.Supply.Name, Unit = i.Supply.UnitOfMeasure, i.Quantity,
            Used = i.UtilizationMonitoring == null ? (decimal?)null : i.UtilizationMonitoring.QuantityUsed,
            Stocked = i.UtilizationMonitoring == null ? (decimal?)null : i.UtilizationMonitoring.QuantityStillStocked,
            NotYetUsed = i.UtilizationMonitoring == null ? (decimal?)null : i.UtilizationMonitoring.QuantityNotYetUsed,
            Disposed = i.UtilizationMonitoring == null ? (decimal?)null : i.UtilizationMonitoring.QuantityDisposed
        }).ToListAsync(ct);
        return Ok(records);
    }

    [HttpGet("production")]
    public async Task<ActionResult<IReadOnlyList<ProductionReportDto>>> GetProductionReport(
        [FromQuery] int? year,
        [FromQuery] string? municipality,
        CancellationToken cancellationToken)
    {
        var query = _context.HarvestRecords.AsNoTracking().AsQueryable();
        if (year.HasValue)
        {
            query = query.Where(record => record.HarvestDate.Year == year.Value);
        }
        if (!string.IsNullOrWhiteSpace(municipality))
        {
            query = query.Where(record =>
                record.PlantingRecord.Farm.Municipality.Contains(municipality));
        }

        var reportRows = await query
            .Select(record => new
            {
                Year = record.HarvestDate.Year,
                Month = record.HarvestDate.Month,
                Municipality = record.PlantingRecord.Farm.Municipality,
                CropName = record.PlantingRecord.CropName,
                FarmerId = record.PlantingRecord.Farm.FarmerId,
                record.QuantityKilograms
            })
            .GroupBy(record => new
            {
                record.Year,
                record.Month,
                record.Municipality,
                record.CropName
            })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                group.Key.Municipality,
                group.Key.CropName,
                HarvestRecordCount = group.Count(),
                FarmerCount = group.Select(record => record.FarmerId).Distinct().Count(),
                TotalHarvestKilograms = group.Sum(record => record.QuantityKilograms)
            })
            .OrderByDescending(item => item.Year)
            .ThenByDescending(item => item.Month)
            .ThenBy(item => item.Municipality)
            .ThenBy(item => item.CropName)
            .ToListAsync(cancellationToken);
        var report = reportRows
            .Select(item => new ProductionReportDto(
                item.Year, item.Month, item.Municipality, item.CropName,
                item.HarvestRecordCount, item.FarmerCount, item.TotalHarvestKilograms))
            .ToList();
        return Ok(report);
    }

    [HttpGet("distributions")]
    public async Task<ActionResult<IReadOnlyList<DistributionReportDto>>> GetDistributionReport(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var query = _context.DistributionItems.AsNoTracking()
            .Where(item => item.Distribution.Status == "Distributed");
        if (year.HasValue)
        {
            query = query.Where(item => item.Distribution.DistributionDate.Year == year.Value);
        }

        var reportRows = await query
            .Select(item => new
            {
                Year = item.Distribution.DistributionDate.Year,
                Month = item.Distribution.DistributionDate.Month,
                Category = item.Supply.Category.Name,
                item.DistributionId,
                item.Distribution.FarmerId,
                item.Quantity,
                item.UnitCost
            })
            .GroupBy(item => new { item.Year, item.Month, item.Category })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                SupplyCategory = group.Key.Category,
                DistributionCount = group.Select(item => item.DistributionId).Distinct().Count(),
                FarmerCount = group.Select(item => item.FarmerId).Distinct().Count(),
                TotalQuantity = group.Sum(item => item.Quantity),
                TotalValue = group.Sum(item => item.Quantity * item.UnitCost)
            })
            .OrderByDescending(item => item.Year)
            .ThenByDescending(item => item.Month)
            .ThenBy(item => item.SupplyCategory)
            .ToListAsync(cancellationToken);
        var report = reportRows
            .Select(item => new DistributionReportDto(
                item.Year, item.Month, item.SupplyCategory,
                item.DistributionCount, item.FarmerCount,
                item.TotalQuantity, item.TotalValue))
            .ToList();
        return Ok(report);
    }

    [HttpGet("distribution-schedule")]
    public async Task<ActionResult<IReadOnlyList<DistributionScheduleDto>>> GetDistributionSchedule(
        [FromQuery] int? year,
        [FromQuery] string? season,
        [FromQuery] string? barangay,
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        var query = _context.Distributions
            .AsNoTracking()
            .Where(distribution => distribution.Farmer.IsActive)
            .AsQueryable();

        if (year.HasValue)
            query = query.Where(distribution => distribution.DistributionDate.Year == year.Value);
        if (!string.IsNullOrWhiteSpace(barangay))
            query = query.Where(distribution => distribution.Farmer.Barangay != null &&
                distribution.Farmer.Barangay.Contains(barangay));
        if (!string.IsNullOrWhiteSpace(season))
        {
            var isWet = season.Trim().Equals("Wet", StringComparison.OrdinalIgnoreCase);
            query = query.Where(distribution => isWet
                ? distribution.DistributionDate.Month >= RISE.API.Services.DistributionScheduleHelper.WetStartMonth &&
                  distribution.DistributionDate.Month <= RISE.API.Services.DistributionScheduleHelper.WetEndMonth
                : distribution.DistributionDate.Month < RISE.API.Services.DistributionScheduleHelper.WetStartMonth ||
                  distribution.DistributionDate.Month > RISE.API.Services.DistributionScheduleHelper.WetEndMonth);
        }

        var rows = await query
            .Select(distribution => new
            {
                distribution.DistributionId,
                FarmerName = distribution.Farmer.FullName,
                distribution.DistributionDate,
                distribution.Status,
                distribution.FarmerId,
                Barangay = distribution.Farmer.Barangay,
                distribution.CreatedAtUtc,
                Items = distribution.Items.Select(item => new
                {
                    item.Quantity,
                    Category = item.Supply.Category == null ? null : item.Supply.Category.Name,
                    item.Supply.Name
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var result = rows.Select(row =>
        {
            var categories = string.Join(" ", row.Items.Select(item => item.Category).Where(name => name != null));
            var supplyNames = string.Join(" ", row.Items.Select(item => item.Name));
            return new DistributionScheduleDto(
                row.DistributionId,
                row.FarmerName,
                row.DistributionDate,
                RISE.API.Services.DistributionScheduleHelper.ClassifyAssistanceType(categories, supplyNames),
                RISE.API.Services.DistributionScheduleHelper.GetCroppingSeason(row.DistributionDate),
                row.Items.Sum(item => item.Quantity),
                new[] { row.FarmerId }.Distinct().Count(),
                row.Barangay ?? "—",
                row.Status,
                row.CreatedAtUtc);
        }).ToList();

        if (!string.IsNullOrWhiteSpace(type))
            result = result.Where(row => string.Equals(row.AssistanceType, type, StringComparison.OrdinalIgnoreCase)).ToList();

        return Ok(result
            .OrderByDescending(row => row.CreatedAtUtc)
            .ThenByDescending(row => row.DistributionId)
            .ToList());
    }
}
