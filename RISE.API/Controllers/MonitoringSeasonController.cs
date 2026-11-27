using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RISE.API.Data;
using RISE.API.Dtos;
using RISE.API.Models;
using RISE.API.Services;

namespace RISE.API.Controllers;

[ApiController]
[Route("api/monitoring-season")]
[Authorize(Policy = "AnyAuthenticated")]
public sealed class MonitoringSeasonController : ControllerBase
{
    private int OwnerId => OwnerScope.UserId(User);

    private readonly RiseDbContext _context;
    private readonly RISE.API.Services.NotificationService _notifications;

    public MonitoringSeasonController(RiseDbContext context, RISE.API.Services.NotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    private async Task<string> ActiveSeasonAsync(CancellationToken ct) =>
        (await _context.AppSettings.AsNoTracking()
            .Where(setting => setting.Key == "ActiveSeason")
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(ct)) ?? "2026-Wet";

    private async Task<string> SettingAsync(string key, string fallback, CancellationToken ct) =>
        (await _context.AppSettings.AsNoTracking().Where(setting => setting.Key == key)
            .Select(setting => setting.Value).FirstOrDefaultAsync(ct)) ?? fallback;

    private async Task<int> StartMonthAsync(CancellationToken ct) =>
        int.TryParse(await SettingAsync("SeasonStartMonth", "6", ct), out var month) && month >= 1 && month <= 12 ? month : 6;

    private async Task<int> StartYearAsync(CancellationToken ct) =>
        int.TryParse(await SettingAsync("SeasonStartYear", DateTime.Today.Year.ToString(), ct), out var year) ? year : DateTime.Today.Year;

    private async Task<DateTime?> StartedAtAsync(CancellationToken ct)
    {
        var value = await SettingAsync("SeasonStartedAtUtc", "", ct);
        return DateTime.TryParse(value, out var parsed) ? parsed : null;
    }

    private async Task<MonitoringSeasonDto> SeasonDtoAsync(string active, CancellationToken ct) =>
        new(active, await SeasonListAsync(ct), await StartMonthAsync(ct), await StartYearAsync(ct), await StartedAtAsync(ct));

    [HttpGet]
    public async Task<ActionResult<MonitoringSeasonDto>> Get(CancellationToken ct)
    {
        var active = await ActiveSeasonAsync(ct);
        return Ok(await SeasonDtoAsync(active, ct));
    }

    private async Task SetSettingAsync(string key, string value, CancellationToken ct)
    {
        var setting = await _context.AppSettings.FirstOrDefaultAsync(item => item.Key == key, ct);
        if (setting is null)
        {
            _context.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }
    }

    [HttpPut]
    public async Task<IActionResult> Set([FromBody] SetSeasonRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Season))
            return BadRequest("Season is required.");
        var season = request.Season.Trim();
        if (season.Length > 50)
            return BadRequest("Season is too long.");

        var oldSeason = await ActiveSeasonAsync(ct);
        if (string.Equals(oldSeason, season, StringComparison.Ordinal))
            return Ok(await SeasonDtoAsync(season, ct));

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var alreadyArchived = await _context.UtilizationSeasonRecords
            .AnyAsync(record => record.SeasonKey == oldSeason && record.DistributionItem.Distribution.Farmer.OwnerUserId == OwnerId, ct);
        if (!alreadyArchived)
        {
            var openRecords = await _context.UtilizationMonitorings.AsNoTracking()
                .Include(u => u.DistributionItem).ThenInclude(i => i.Distribution).ThenInclude(d => d.Farmer)
                .Where(u => u.DistributionItem.Distribution.Farmer.OwnerUserId == OwnerId)
                .ToListAsync(ct);
            foreach (var record in openRecords)
            {
                _context.UtilizationSeasonRecords.Add(new UtilizationSeasonRecord
                {
                    SeasonKey = oldSeason,
                    DistributionItemId = record.DistributionItemId,
                    QuantityUsed = record.QuantityUsed,
                    QuantityStillStocked = record.QuantityStillStocked,
                    QuantityDisposed = record.QuantityDisposed,
                    MonitoringDate = record.MonitoringDate,
                    Remarks = record.Remarks,
                    ArchivedAtUtc = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync(ct);
            _context.UtilizationMonitorings.RemoveRange(openRecords);
        }
        var seasons = (await SeasonListAsync(ct)).ToList();
        foreach (var key in new[] { oldSeason, season })
            if (!seasons.Contains(key)) seasons.Add(key);
        await SetSettingAsync("SeasonHistory", string.Join(",", seasons.OrderBy(value => value)), ct);
        await SetSettingAsync("ActiveSeason", season, ct);
        if (request.StartMonth.HasValue) await SetSettingAsync("SeasonStartMonth", request.StartMonth.Value.ToString(), ct);
        if (request.StartYear.HasValue) await SetSettingAsync("SeasonStartYear", request.StartYear.Value.ToString(), ct);
        await SetSettingAsync("SeasonStartedAtUtc", DateTime.UtcNow.ToString("o"), ct);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        await _notifications.PublishAsync("ImportantAlert", "New season created",
            $"{season} has started. The previous season {oldSeason} was archived and its records remain viewable.", ct);

        return Ok(await SeasonDtoAsync(season, ct));
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<SeasonHistoryDto>>> History([FromQuery] string? season, CancellationToken ct)
    {
        var query = _context.UtilizationSeasonRecords.AsNoTracking()
            .Where(record => record.DistributionItem.Distribution.Farmer.OwnerUserId == OwnerId)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(season))
            query = query.Where(record => record.SeasonKey == season.Trim());

        var rows = await query
            .Select(record => new SeasonHistoryDto(
                record.DistributionItemId,
                record.SeasonKey,
                record.DistributionItem.Distribution.Farmer.FullName,
                record.DistributionItem.Supply.Name,
                record.DistributionItem.Supply.Category == null ? "—" : record.DistributionItem.Supply.Category.Name,
                record.DistributionItem.Supply.UnitOfMeasure,
                record.DistributionItem.Quantity,
                record.QuantityUsed,
                record.QuantityStillStocked,
                record.QuantityDisposed,
                record.MonitoringDate,
                record.Remarks))
            .ToListAsync(ct);
        return Ok(rows.OrderByDescending(row => row.MonitoringDate).ToList());
    }

    private async Task<IReadOnlyList<string>> SeasonListAsync(CancellationToken ct)
    {
        var active = await ActiveSeasonAsync(ct);
        var stored = await SettingAsync("SeasonHistory", "", ct);
        var archived = await _context.UtilizationSeasonRecords.AsNoTracking()
            .Where(record => record.DistributionItem.Distribution.Farmer.OwnerUserId == OwnerId)
            .Select(record => record.SeasonKey).Distinct().ToListAsync(ct);
        return stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Concat(new[] { active }).Concat(archived)
            .Distinct().OrderBy(value => value).ToList();
    }
}
