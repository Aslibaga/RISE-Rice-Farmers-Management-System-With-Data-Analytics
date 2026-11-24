using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RISE.API.Data;
using RISE.API.Dtos;
using RISE.API.Models;

namespace RISE.API.Controllers;

[ApiController]
[Route("api/utilization")]
[Authorize(Policy = "AnyAuthenticated")]
public sealed class UtilizationController : ControllerBase
{
    private readonly RiseDbContext _db;
    private readonly ILogger<UtilizationController> _logger;
    private readonly IConfiguration _configuration;

    public UtilizationController(RiseDbContext db, ILogger<UtilizationController> logger, IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("settings")]
    public IActionResult GetSettings() =>
        Ok(new { targetMonths = _configuration.GetValue<int>("Monitoring:TargetMonths", 3) });

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UtilizationDto>>> Get(
        [FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] string? barangay, CancellationToken ct)
    {
        var query = _db.DistributionItems.AsNoTracking()
            .Where(item => item.Distribution.Status == "Distributed");
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(item => item.Distribution.Farmer.FullName.Contains(search) ||
                item.Supply.Name.Contains(search) ||
                item.Distribution.ReferenceNumber!.Contains(search));
        if (!string.IsNullOrWhiteSpace(barangay))
            query = query.Where(item => item.Distribution.Farmer.Barangay != null &&
                item.Distribution.Farmer.Barangay.Contains(barangay));

        var rows = await query
            .OrderBy(item => item.Distribution.Farmer.FullName)
            .ThenBy(item => item.Supply.Name)
            .ThenBy(item => item.Distribution.DistributionDate)
            .Select(item => new
            {
                item.DistributionItemId, item.DistributionId,
                item.Distribution.FarmerId, FarmerName = item.Distribution.Farmer.FullName,
                FirstName = item.Distribution.Farmer.FirstName,
                LastName = item.Distribution.Farmer.LastName,
                MiddleName = item.Distribution.Farmer.MiddleName,
                item.Supply.Name, Unit = item.Supply.UnitOfMeasure, Received = item.Quantity,
                Category = item.Supply.Category == null ? null : item.Supply.Category.Name,
                DistributionDate = item.Distribution.DistributionDate,
                TargetMonitoringDate = item.TargetMonitoringDate,
                Monitoring = item.UtilizationMonitoring
            }).ToListAsync(ct);

        var result = rows.Select(row => {
            var m = row.Monitoring;
            var reported = (m?.QuantityUsed ?? 0) + (m?.QuantityStillStocked ?? 0) +
                (m?.QuantityNotYetUsed ?? 0) + (m?.QuantityDisposed ?? 0);
            return new UtilizationDto(m?.UtilizationMonitoringId ?? 0,
                row.DistributionItemId, row.DistributionId, row.FarmerId, row.FarmerName,
                row.FirstName, row.LastName, row.MiddleName,
                row.Name, row.Unit, row.Received, m?.QuantityUsed, m?.QuantityStillStocked,
                m?.QuantityNotYetUsed, m?.QuantityDisposed,
                Math.Max(0, row.Received - reported), m?.MonitoringDate ?? DateOnly.MinValue,
                m?.Remarks, row.Category,
                row.TargetMonitoringDate ?? row.DistributionDate.AddMonths(_configuration.GetValue<int>("Monitoring:TargetMonths", 3)));
        }).ToList();

        if (!string.IsNullOrWhiteSpace(status))
            result = result.Where(x => status == "Not monitored" ? x.UtilizationMonitoringId == 0 :
                status == "Incomplete" ? x.UtilizationMonitoringId != 0 && x.UnreportedQuantity > 0 :
                x.UtilizationMonitoringId != 0 && x.UnreportedQuantity == 0).ToList();
        return Ok(result);
    }

    [HttpPut("{distributionItemId:int}")]
    public async Task<IActionResult> Save(int distributionItemId, UtilizationWriteDto request,
        CancellationToken ct)
    {
        var item = await _db.DistributionItems.Include(x => x.Distribution)
            .FirstOrDefaultAsync(x => x.DistributionItemId == distributionItemId, ct);
        if (item == null) return NotFound("Distribution item not found.");
        if (item.Distribution.Status != "Distributed")
            return Conflict("Utilization can only be recorded for assistance marked as Received.");

        var values = new[] { request.QuantityUsed, request.QuantityStillStocked,
            request.QuantityNotYetUsed, request.QuantityDisposed };
        if (values.All(x => !x.HasValue))
            return BadRequest("Enter at least one utilization quantity.");
        var total = values.Sum(x => x ?? 0m);
        if (total > item.Quantity)
            return BadRequest($"Utilization quantities ({total}) cannot exceed received quantity ({item.Quantity}).");
        if (values.Any(x => x.HasValue && x.Value < 0m))
            return BadRequest("Utilization quantities cannot be negative.");

        var record = await _db.UtilizationMonitorings
            .FirstOrDefaultAsync(x => x.DistributionItemId == distributionItemId, ct);
        if (record == null)
        {
            record = new UtilizationMonitoring { DistributionItemId = distributionItemId };
            _db.UtilizationMonitorings.Add(record);
        }
        record.QuantityUsed = request.QuantityUsed;
        record.QuantityStillStocked = request.QuantityStillStocked;
        record.QuantityNotYetUsed = request.QuantityNotYetUsed;
        record.QuantityDisposed = request.QuantityDisposed;
        record.MonitoringDate = request.MonitoringDate == default
            ? DateOnly.FromDateTime(DateTime.Today) : request.MonitoringDate;
        record.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Unable to save utilization for item {ItemId}", distributionItemId);
            return Problem(statusCode: 500, title: "Could not save utilization monitoring.");
        }
        return NoContent();
    }
}
