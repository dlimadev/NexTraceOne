using System.Text.Json;

namespace NexTraceOne.Cli.Services;

public class NotificationsService
{
    private readonly ApiService _apiService;

    public NotificationsService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<NotificationSummary>> ListNotificationsAsync(bool unreadOnly = false)
    {
        var endpoint = "/api/v1/notifications";
        if (unreadOnly)
            endpoint += "?unread=true";

        var response = await _apiService.GetAsync<NotificationsListResponse>(endpoint);
        return response.Notifications;
    }

    public async Task MarkAsReadAsync(string notificationId)
    {
        await _apiService.PutAsync($"/api/v1/notifications/{notificationId}/read", new { });
    }

    public async Task SendTestNotificationAsync(string message)
    {
        await _apiService.PostAsync<object, object>(
            "/api/v1/notifications/test",
            new { message });
    }
}

// Models
public class NotificationsListResponse
{
    public List<NotificationSummary> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
}

public class NotificationSummary
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
