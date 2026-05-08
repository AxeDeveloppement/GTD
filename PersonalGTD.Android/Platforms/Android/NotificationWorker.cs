using Android.App;
using Android.Content;
using AndroidX.Work;
using PersonalGTD.Shared;
using PersonalGTD.Shared.Models;
using PersonalGTD.Shared.Services;
using Supabase;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace PersonalGTD.Android;

public class NotificationWorker : Worker
{
    public NotificationWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
    {
    }

    public override Result DoWork()
    {
        try
        {
            // Récupérer la session persistée via Preferences (partagée entre UI et Service)
            var sessionJson = Preferences.Default.Get<string?>("supabase_session", null);
            if (string.IsNullOrEmpty(sessionJson)) return Result.InvokeSuccess();

            var options = new SupabaseOptions { AutoConnectRealtime = false };
            var client = new Client(SupabaseConfig.Url, SupabaseConfig.Key, options);
            
            // Initialisation synchrone pour le worker
            client.InitializeAsync().Wait();

            var session = JsonSerializer.Deserialize<Supabase.Gotrue.Session>(sessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (session == null || string.IsNullOrEmpty(session.AccessToken)) return Result.InvokeSuccess();

            client.Auth.SetSession(session.AccessToken, session.RefreshToken ?? "").Wait();

            // Récupérer les tâches
            var response = client.From<GtdItem>().Get().Result;
            var tasks = response.Models;

            var today = DateTime.Now.Date;
            var overdue = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date < today && t.Status != GtdStatus.Done).ToList();
            var todayTasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == today && t.Status != GtdStatus.Done).ToList();

            if (overdue.Any() || todayTasks.Any())
            {
                ShowNotification(overdue.Count, todayTasks.Count);
            }

            return Result.InvokeSuccess();
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("NotificationWorker", $"Error: {ex.Message}");
            return Result.InvokeRetry();
        }
    }

    private void ShowNotification(int overdueCount, int todayCount)
    {
        var intent = new Intent(ApplicationContext, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.ClearTop);
        var pendingIntent = PendingIntent.GetActivity(ApplicationContext, 0, intent, PendingIntentFlags.Immutable);

        var builder = new global::Android.App.Notification.Builder(ApplicationContext, "gtd_notifications")
            .SetContentTitle("Mon organiseur")
            .SetSmallIcon(global::Android.Resource.Mipmap.SymDefAppIcon)
            .SetAutoCancel(true)
            .SetContentIntent(pendingIntent);

        if (overdueCount > 0 && todayCount > 0)
            builder.SetContentText($"{overdueCount} tâches en retard et {todayCount} aujourd'hui.");
        else if (overdueCount > 0)
            builder.SetContentText($"{overdueCount} tâches en retard !");
        else
            builder.SetContentText($"{todayCount} tâches pour aujourd'hui.");

        var notificationManager = (global::Android.App.NotificationManager)ApplicationContext.GetSystemService(Context.NotificationService)!;
        
        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
        {
            var channel = new global::Android.App.NotificationChannel("gtd_notifications", "Rappels GTD", global::Android.App.NotificationImportance.Default);
            notificationManager.CreateNotificationChannel(channel);
        }

        notificationManager.Notify(1001, builder.Build());
    }
}
