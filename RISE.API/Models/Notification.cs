namespace RISE.API.Models;

/// <summary>
/// A notification delivered to a user. Type maps to one of the five
/// notification preferences stored on the User entity.
/// </summary>
public class Notification
{
    public int NotificationId { get; set; }
    public int? UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public User? User { get; set; }
}
