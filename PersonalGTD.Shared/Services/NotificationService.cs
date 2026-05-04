using Microsoft.JSInterop;
using PersonalGTD.Shared.Models;
using System.Text.Json;

namespace PersonalGTD.Shared.Services;

public class NotificationService
{
    private readonly IJSRuntime _js;
    private readonly ITaskService _taskService;
    private readonly AuthService _authService;

    public bool HasAgendaNotifications { get; private set; }
    public event Action? OnStateChanged;

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

        // Notification de test systématique pour valider le clic
        await _js.InvokeVoidAsync("notificationHelper.showNotification", "Connexion réussie", "Cliquez ici pour tester l'accès à l'agenda.", "agenda-test");

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
        // On force l'affichage pour le test actuel
        await _js.InvokeVoidAsync("notificationHelper.showNotification", title, body, key);
        
        /* Version de production :
        var user = _authService.CurrentUser;
        var storageKey = $"gtd-notified-{user}-{key}-{DateTime.Now:yyyy-MM-dd}";
        
        var alreadyNotified = await _js.InvokeAsync<string>("localStorage.getItem", storageKey);
        if (string.IsNullOrEmpty(alreadyNotified))
        {
            await _js.InvokeVoidAsync("notificationHelper.showNotification", title, body, key);
            await _js.InvokeVoidAsync("localStorage.setItem", storageKey, "true");
        }
        */
    }
}
