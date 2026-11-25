using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RISE.API.Data;
using RISE.API.Dtos;
using RISE.API.Models;

namespace RISE.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Policy = "AnyAuthenticated")]
public sealed class NotificationsController : ControllerBase
{
    private readonly RiseDbContext _context;

    public NotificationsController(RiseDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> Get(CancellationToken ct)
    {
        var userId = CurrentUserId();
        var stored = await _context.Notifications.AsNoTracking()
            .Where(item => item.UserId == null || item.UserId == userId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new NotificationDto(item.NotificationId, item.Type, item.Title,
                item.Message, item.IsRead, item.CreatedAtUtc))
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var overdue = await _context.DistributionItems.AsNoTracking()
            .Where(item => item.Distribution.Status == "Distributed" && item.UtilizationMonitoring == null)
            .Select(item => new
            {
                item.DistributionItemId,
                Farmer = item.Distribution.Farmer.FullName,
                Supply = item.Supply.Name,
                DistributionDate = item.Distribution.DistributionDate
            })
            .ToListAsync(ct);

        var dynamicItems = overdue
            .Select(item => new { item.DistributionItemId, item.Farmer, item.Supply, Due = item.DistributionDate.AddMonths(3) })
            .Where(item => item.Due <= today.AddMonths(1))
            .Select(item => item.Due < today
                ? new NotificationDto(-item.DistributionItemId, "ImportantAlert", "Overdue monitoring",
                    $"{item.Supply} for {item.Farmer} was due on {item.Due:yyyy-MM-dd}.", false, DateTime.Today)
                : new NotificationDto(-item.DistributionItemId, "SystemAnnouncement", "Monitoring due soon",
                    $"{item.Supply} for {item.Farmer} should be monitored by {item.Due:yyyy-MM-dd}.", false, DateTime.Today))
            .ToList();

        return Ok(dynamicItems.Concat(stored).OrderByDescending(item => item.CreatedAtUtc).ToList());
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        var userId = CurrentUserId();
        var item = await _context.Notifications
            .FirstOrDefaultAsync(x => x.NotificationId == id && (x.UserId == null || x.UserId == userId), ct);
        if (item is null) return NotFound();
        item.IsRead = true;
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var userId = CurrentUserId();
        var items = await _context.Notifications
            .Where(x => x.UserId == null || x.UserId == userId)
            .ToListAsync(ct);
        foreach (var item in items) item.IsRead = true;
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}
