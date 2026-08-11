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
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Tiramisu)
        {
            if (CheckSelfPermission(global::Android.Manifest.Permission.PostNotifications) != global::Android.Content.PM.Permission.Granted)
            {
                RequestPermissions(new string[] { global::Android.Manifest.Permission.PostNotifications }, 0);
            }
        }

        HandleIntent(Intent);
        ScheduleNotificationWorker();
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
            ExistingPeriodicWorkPolicy policy = ExistingPeriodicWorkPolicy.Update;
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
            var authService = IPlatformApplication.Current?.Services.GetService<PersonalGTD.Shared.Services.AuthService>();
            if (authService != null)
            {
                _ = authService.ProcessDeepLink(intent.DataString);
            }
        }
    }
}
