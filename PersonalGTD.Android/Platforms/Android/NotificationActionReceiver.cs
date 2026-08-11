using Android.App;
using Android.Content;
using AndroidX.Work;
using Microsoft.Maui.Storage;
using System;

namespace PersonalGTD.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class NotificationActionReceiver : BroadcastReceiver
{
    public const string ActionRemindLater = "com.personalgtd.REMIND_LATER";
    public const string ActionStopReminding = "com.personalgtd.STOP_REMINDING";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;

        // Dismiss the notification
        var notificationManager = (global::Android.App.NotificationManager)context.GetSystemService(Context.NotificationService)!;
        notificationManager.Cancel(1001);

        if (intent.Action == ActionRemindLater)
        {
            NetworkType networkType = NetworkType.Connected;
            var constraints = new Constraints.Builder()
                .SetRequiredNetworkType(networkType)
                .Build();

            var workRequest = OneTimeWorkRequest.Builder.From<NotificationWorker>()
                .SetConstraints(constraints)
                .SetInitialDelay(TimeSpan.FromHours(1))
                .Build();

            ExistingWorkPolicy policy = ExistingWorkPolicy.Replace;
            WorkManager.GetInstance(context).EnqueueUniqueWork(
                "GTDNotificationWork_Delayed",
                policy,
                workRequest);

            global::Android.Util.Log.Info("NotificationActionReceiver", "Scheduled reminder for 1 hour later.");
        }
        else if (intent.Action == ActionStopReminding)
        {
            Preferences.Default.Set("stop_reminding_date", DateTime.Now.ToString("yyyy-MM-dd"));
            global::Android.Util.Log.Info("NotificationActionReceiver", "Stopped reminding for today.");
        }
    }
}
