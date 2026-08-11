using Microsoft.JSInterop;
using PersonalGTD.Shared.Models;

namespace PersonalGTD.Shared.Services;

public class NotificationService
{
    private readonly IJSRuntime _js;
    private readonly ISessionStorage _sessionStorage;
    private readonly ITaskService _taskService;
    private readonly AuthService _authService;

    public bool HasAgendaNotifications { get; private set; }
    public event Action? OnStateChanged;

    public NotificationService(IJSRuntime js, ISessionStorage sessionStorage, ITaskService taskService, AuthService authService)
    {
        _js = js;
        _sessionStorage = sessionStorage;
        _taskService = taskService;
        _authService = authService;
    }

    public async Task RequestPermissionAsync()
    {
        await _js.InvokeAsync<string>("notificationHelper.requestPermission");
    }

    public async Task CheckAndNotifyAsync()
    {
        if (_authService.CurrentUser == null) return;

        var tasks = await _taskService.GetItemsAsync();
        var today = DateTime.Now.Date;

        var overdueTasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date < today && t.Status != GtdStatus.Done).ToList();
        var todayTasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == today && t.Status != GtdStatus.Done).ToList();

        if (overdueTasks.Any())
        {
            HasAgendaNotifications = true;
            var title = "Tâches en retard !";
            var body = $"Vous avez {overdueTasks.Count} tâche(s) en retard.";
            await NotifyOnceAsync("agenda-overdue", title, body);
        }

        if (todayTasks.Any())
        {
            HasAgendaNotifications = true;
            var title = "Tâches du jour";
            var body = $"Vous avez {todayTasks.Count} tâche(s) à faire aujourd'hui.";
            await NotifyOnceAsync("agenda-today", title, body);
        }

        if (HasAgendaNotifications)
        {
            OnStateChanged?.Invoke();
        }
    }

    public void ClearAgendaNotifications()
    {
        if (HasAgendaNotifications)
        {
            HasAgendaNotifications = false;
            OnStateChanged?.Invoke();
        }
    }

    private async Task NotifyOnceAsync(string key, string title, string body)
    {
        var user = _authService.CurrentUser;
        var storageKey = $"gtd-notified-{user}-{key}-{DateTime.Now:yyyy-MM-dd}";

        try
        {
            var alreadyNotified = await _sessionStorage.GetItemAsync(storageKey);
            if (string.IsNullOrEmpty(alreadyNotified))
            {
                await _js.InvokeVoidAsync("notificationHelper.showNotification", title, body, key);
                await _sessionStorage.SetItemAsync(storageKey, "true");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationService] NotifyOnceAsync error: {ex.Message}");
        }
    }
}
