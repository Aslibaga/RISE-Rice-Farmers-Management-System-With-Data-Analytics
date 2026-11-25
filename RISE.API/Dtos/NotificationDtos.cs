namespace RISE.API.Dtos;

public sealed record NotificationDto(
    int NotificationId,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAtUtc);
