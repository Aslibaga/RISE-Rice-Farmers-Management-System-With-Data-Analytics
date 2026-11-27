using Microsoft.EntityFrameworkCore;
using RISE.API.Data;
using RISE.API.Models;

namespace RISE.API.Services;

/// <summary>
/// Publishes notifications only to users who enabled the matching preference.
/// </summary>
public sealed class NotificationService
{
    private readonly RiseDbContext _db;

    public NotificationService(RiseDbContext db)
    {
        _db = db;
    }

    public async Task PublishAsync(string type, string title, string message, CancellationToken ct = default, int? ownerUserId = null)
    {
        var users = await _db.Users.AsNoTracking()
            .Where(user => user.IsActive && (!ownerUserId.HasValue || user.UserId == ownerUserId.Value))
            .ToListAsync(ct);
        foreach (var user in users)
        {
            var enabled = type switch
            {
                "SystemAnnouncement" => user.NotifySystemAnnouncements,
                "FarmerRecordUpdate" => user.NotifyFarmerRecordUpdates,
                "NewFarmerRegistration" => user.NotifyNewFarmerRegistration,
                "ReportGeneration" => user.NotifyReportGenerationUpdates,
                "ImportantAlert" => user.NotifyImportantSystemAlerts,
                _ => false
            };
            if (!enabled) continue;
            _db.Notifications.Add(new Notification
            {
                UserId = user.UserId,
                Type = type,
                Title = title,
                Message = message,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(ct);
    }
}
