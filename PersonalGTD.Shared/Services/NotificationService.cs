using Microsoft.JSInterop;
using PersonalGTD.Shared.Models;
using System.Text.Json;

namespace PersonalGTD.Shared.Services;

public class NotificationService
{
    private readonly IJSRuntime _js;
    private readonly ITaskService _taskService;
    private readonly AuthService _authService;

    public NotificationService(IJSRuntime js, ITaskService taskService, AuthService authService)
    {
        _js = js;
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
            var title = "Tâches en retard !";
            var body = $"Vous avez {overdueTasks.Count} tâche(s) en retard.";
            await NotifyOnceAsync("overdue-alert", title, body);
        }

        if (todayTasks.Any())
        {
            var title = "Tâches du jour";
            var body = $"Vous avez {todayTasks.Count} tâche(s) à faire aujourd'hui.";
            await NotifyOnceAsync("today-alert", title, body);
        }
    }

    private async Task NotifyOnceAsync(string key, string title, string body)
    {
        var user = _authService.CurrentUser;
        var storageKey = $"gtd-notified-{user}-{key}-{DateTime.Now:yyyy-MM-dd}";
        
        var alreadyNotified = await _js.InvokeAsync<string>("localStorage.getItem", storageKey);
        if (string.IsNullOrEmpty(alreadyNotified))
        {
            await _js.InvokeVoidAsync("notificationHelper.showNotification", title, body, key);
            await _js.InvokeVoidAsync("localStorage.setItem", storageKey, "true");
        }
    }
}
