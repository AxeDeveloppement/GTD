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
            global::Android.Util.Log.Info("NotificationWorker", "Worker started checking for tasks...");

            // Check if user stopped reminding for today
            var stopDate = Preferences.Default.Get<string>("stop_reminding_date", "");
            if (stopDate == DateTime.Now.ToString("yyyy-MM-dd"))
            {
                global::Android.Util.Log.Info("NotificationWorker", "User requested to stop reminding for today. Skipping notification.");
                return Result.InvokeSuccess();
            }

            // Récupérer la session persistée via Preferences (partagée entre UI et Service)
            var sessionJson = Preferences.Default.Get<string?>("supabase_session", null);
            if (string.IsNullOrEmpty(sessionJson)) 
            {
                global::Android.Util.Log.Warn("NotificationWorker", "No session found in Preferences. User might not be logged in.");
                return Result.InvokeSuccess();
            }

            var options = new SupabaseOptions { AutoConnectRealtime = false };
            var client = new Client(SupabaseConfig.Url, SupabaseConfig.Key, options);
            
            // Initialisation synchrone pour le worker
            client.InitializeAsync().Wait();

            var session = JsonSerializer.Deserialize<Supabase.Gotrue.Session>(sessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (session == null || string.IsNullOrEmpty(session.AccessToken)) 
            {
                global::Android.Util.Log.Warn("NotificationWorker", "Session is invalid or empty.");
                return Result.InvokeSuccess();
            }

            client.Auth.SetSession(session.AccessToken, session.RefreshToken ?? "").Wait();

            // Récupérer les tâches
            var response = client.From<GtdItem>().Get().Result;
            var tasks = response.Models;

            var today = DateTime.Now.Date;
            var overdue = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date < today && t.Status != GtdStatus.Done && t.Status != GtdStatus.Abandoned).ToList();
            var todayTasks = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == today && t.Status != GtdStatus.Done && t.Status != GtdStatus.Abandoned).ToList();

            global::Android.Util.Log.Info("NotificationWorker", $"Found {tasks.Count} total tasks. Overdue: {overdue.Count}, Today: {todayTasks.Count}");

            if (overdue.Any() || todayTasks.Any())
            {
                ShowNotification(overdue.Count, todayTasks.Count);
            }

            return Result.InvokeSuccess();
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("NotificationWorker", $"Error in Worker: {ex.Message}");
            global::Android.Util.Log.Error("NotificationWorker", ex.StackTrace);
            return Result.InvokeRetry();
        }
    }

    private void ShowNotification(int overdueCount, int todayCount)
    {
        var context = ApplicationContext;
        var intent = new Intent(context, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        
        var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        string channelId = "gtd_notifications_v2"; // Nouvelle version du channel pour forcer la prise en compte des paramètres
        var notificationManager = (global::Android.App.NotificationManager)context.GetSystemService(Context.NotificationService)!;

        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
        {
            var channel = new global::Android.App.NotificationChannel(channelId, "Rappels GTD", global::Android.App.NotificationImportance.High)
            {
                Description = "Notifications pour les tâches du jour et en retard",
                LockscreenVisibility = NotificationVisibility.Public
            };
            channel.EnableVibration(true);
            notificationManager.CreateNotificationChannel(channel);
        }

        var remindLaterIntent = new Intent(context, typeof(NotificationActionReceiver));
        remindLaterIntent.SetAction(NotificationActionReceiver.ActionRemindLater);
        var remindLaterPending = PendingIntent.GetBroadcast(context, 0, remindLaterIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var stopRemindingIntent = new Intent(context, typeof(NotificationActionReceiver));
        stopRemindingIntent.SetAction(NotificationActionReceiver.ActionStopReminding);
        var stopRemindingPending = PendingIntent.GetBroadcast(context, 1, stopRemindingIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var builder = new global::Android.App.Notification.Builder(context, channelId)
            .SetContentTitle("Mon GTD")
            .SetSmallIcon(global::Android.Resource.Mipmap.SymDefAppIcon)
            .SetAutoCancel(true)
            .SetContentIntent(pendingIntent)
            .SetVisibility(NotificationVisibility.Public)
            .SetPriority((int)NotificationPriority.High)
            .AddAction(0, "Rappeler dans 1h", remindLaterPending)
            .AddAction(0, "Ne plus rappeler", stopRemindingPending);

        if (overdueCount > 0 && todayCount > 0)
            builder.SetContentText($"{overdueCount} tâches en retard et {todayCount} aujourd'hui.");
        else if (overdueCount > 0)
            builder.SetContentText($"{overdueCount} tâches en retard !");
        else
            builder.SetContentText($"{todayCount} tâches pour aujourd'hui.");

        notificationManager.Notify(1001, builder.Build());
        global::Android.Util.Log.Info("NotificationWorker", "Notification sent successfully.");
    }
}
