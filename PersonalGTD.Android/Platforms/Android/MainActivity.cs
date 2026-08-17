using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;
using Microsoft.Extensions.DependencyInjection;
using AndroidX.Work;

namespace PersonalGTD.Android;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private const int NotificationPermissionRequestId = 0;
    private static readonly string[] NotificationPermission = { global::Android.Manifest.Permission.PostNotifications };

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

        RequestNotificationPermissionIfNeeded();
        HandleIntent(Intent);
        ScheduleNotificationWorker();
	}

    /// <summary>
    /// Demande la permission POST_NOTIFICATIONS sur Android 13+ si non accordée.
    /// </summary>
    private void RequestNotificationPermissionIfNeeded()
    {
        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Tiramisu)
        {
            if (CheckSelfPermission(global::Android.Manifest.Permission.PostNotifications) != global::Android.Content.PM.Permission.Granted)
            {
                RequestPermissions(NotificationPermission, NotificationPermissionRequestId);
            }
        }
    }

    /// <summary>
    /// Callback pour gérer le résultat de la demande de permission notifications.
    /// </summary>
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == NotificationPermissionRequestId && permissions.Length > 0)
        {
            if (permissions[0] == global::Android.Manifest.Permission.PostNotifications)
            {
                if (grantResults.Length > 0 && grantResults[0] != Permission.Granted)
                {
                    global::Android.Util.Log.Warn("MainActivity", "Permission notifications refusée par l'utilisateur.");
                    global::Android.Widget.Toast.MakeText(this, "Les notifications sont nécessaires pour recevoir des rappels de tâches.", global::Android.Widget.ToastLength.Long).Show();
                }
                else
                {
                    global::Android.Util.Log.Info("MainActivity", "Permission notifications accordée.");
                }
            }
        }
    }

    private void ScheduleNotificationWorker()
    {
        try
        {
            NetworkType networkType = NetworkType.Connected;
            var constraints = new Constraints.Builder()
                .SetRequiredNetworkType(networkType)
                .Build();

            var workRequest = PeriodicWorkRequest.Builder.From<NotificationWorker>(TimeSpan.FromHours(1))
                .SetConstraints(constraints)
                .Build();
            // Replace annule le travail existant et le remplace, garantissant une mise à jour propre
            ExistingPeriodicWorkPolicy policy = ExistingPeriodicWorkPolicy.Replace;
            WorkManager.GetInstance(this).EnqueueUniquePeriodicWork(
                "GTDNotificationWork",
                policy,
                workRequest);
            global::Android.Util.Log.Info("MainActivity", "Notification worker scheduled/updated successfully (1h period).");
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("MainActivity", $"Failed to schedule worker: {ex.Message ?? "Unknown error"}");
        }
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleIntent(intent);
    }

    private void HandleIntent(Intent? intent)
    {
        if (intent?.DataString != null && intent.DataString.StartsWith("gtdapp://auth"))
        {
            // Vérification de sécurité : s'assurer que l'intent provient bien de notre application
            if (intent.Package == PackageName)
            {
                var authService = IPlatformApplication.Current?.Services.GetService<PersonalGTD.Shared.Services.AuthService>();
                if (authService != null)
                {
                    _ = authService.ProcessDeepLink(intent.DataString);
                }
            }
            else
            {
                global::Android.Util.Log.Warn("MainActivity", $"Intent deep link ignoré : source non fiable (package={intent.Package ?? "null"}).");
            }
        }
    }
}
