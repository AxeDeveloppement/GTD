using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Work;
namespace PersonalGTD.Android;
using AndroidX.Core.View;
using global::Android.Views;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private const int NotificationPermissionRequestId = 0;

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
        
        if (Window == null) return;

    // 1. Force l'OS à réserver l'espace pour les barres système (désactive l'Edge-to-Edge)
    WindowCompat.SetDecorFitsSystemWindows(Window, true);

    // 2. Interdit expressément à l'application de monter dans la zone du poinçon caméra
    if (Build.VERSION.SdkInt >= BuildVersionCodes.P) // Android 9+
    {
        Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.Never;
    }

    // 3. (Optionnel) Définit la couleur de fond de la barre d'état si nécessaire
    // Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#0f172a"));
        
#if DEBUG
        global::Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
        RequestNotificationPermissionIfNeeded();
        HandleIntent(Intent);
        ScheduleNotificationWorker();
	}

    /// <summary>
    /// Configure les barres système (statut et navigation) de façon adaptée à la
    /// version d'Android, afin que l'application s'insère dans le bon espace :
    ///
    /// - Android 10+ (API 29+) : mode edge-to-edge. Le WebView s'étend sous la barre
    ///   de statut et la barre de navigation. Les barres passent en transparent pour
    ///   laisser apparaître le fond sombre de l'app (#0f172a) au lieu du fond violet
    ///   du splash, et les icônes système sont éclaircies (blanches). Le contenu est
    ///   ensuite décalé via les insets safe-area côté CSS (env(safe-area-inset-*)).
    ///
    /// - Android 9 et antérieurs : le mode edge-to-edge via WindowCompat n'est pas
    ///   fiable. On garde le contenu sous les barres (fitsSystemWindows par défaut)
    ///   et on colore la barre de statut avec le fond de l'app pour éviter le flash
    ///   violet du splash.
    ///
    /// NB : le préfixe global:: est obligatoire car le namespace PersonalGTD.Android
    /// masque le namespace Android.
    /// </summary>
    private void ConfigureSystemBars()
    {
        try
        {
            var appBackground = global::Android.Graphics.Color.ParseColor("#0f172a");

            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                // Edge-to-edge : le contenu (WebView) s'étend sous les barres système.
                // WindowCompat (AndroidX.Core) gère proprement les différences de version.
                global::AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(Window, false);

                // Barres transparentes : le fond sombre de l'app (#0f172a) apparaît
                // derrière la barre de statut et la barre de navigation, au lieu du
                // fond violet du splash (#512BD4).
                Window.SetStatusBarColor(global::Android.Graphics.Color.Transparent);
                Window.SetNavigationBarColor(global::Android.Graphics.Color.Transparent);

                // Icônes claires (blanches) pour la barre de statut et de navigation,
                // car le fond de l'app est sombre.
                // WindowInsetsController et WindowInsetsControllerAppearance
                // sont disponibles à partir d'Android 11 (API 30).
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    var controller = Window.InsetsController;
                    if (controller != null)
                    {
                        var appearance = global::Android.Views.WindowInsetsControllerAppearance.LightStatusBars
                                       | global::Android.Views.WindowInsetsControllerAppearance.LightNavigationBars;
                        var appearanceInt = (int)appearance;
                        controller.SetSystemBarsAppearance(appearanceInt, appearanceInt);
                    }
                }
            }
            else
            {
                // Android 9 et antérieurs : on conserve le contenu sous les barres
                // (fitsSystemWindows par défaut) et on colore la barre de statut avec
                // le fond de l'app pour éviter le flash violet du splash.
                Window.SetStatusBarColor(appBackground);
                Window.SetNavigationBarColor(appBackground);
            }
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Warn("MainActivity", $"System bars config failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Demande la permission POST_NOTIFICATIONS sur Android 13+ si non accordée.
    /// </summary>
    private void RequestNotificationPermissionIfNeeded()
    {
        // Vérifier au moment de l'exécution que l'API Android >= 33
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            const string postNotificationsPermission = "android.permission.POST_NOTIFICATIONS";
            if (CheckSelfPermission(postNotificationsPermission) != global::Android.Content.PM.Permission.Granted)
            {
                RequestPermissions(new[] { postNotificationsPermission }, NotificationPermissionRequestId);
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
            if (permissions[0] == "android.permission.POST_NOTIFICATIONS")
            {
                if (grantResults.Length > 0 && grantResults[0] != Permission.Granted)
                {
                    global::Android.Util.Log.Warn("MainActivity", "Permission notifications refusée par l'utilisateur.");
                    global::Android.Widget.Toast.MakeText(this, "Les notifications sont nécessaires pour recevoir des rappels de tâches.", global::Android.Widget.ToastLength.Long)?.Show();
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
            // NB : les bindings AndroidX.Work exposent NetworkType.Connected et
            // ExistingPeriodicWorkPolicy.CancelAndReenqueue comme propriétés annotées nullables,
            // alors que les paramètres attendus sont non-nullable (warnings CS8604) — d'où l'opérateur null-forgiving "!".
            var constraints = new Constraints.Builder()
                .SetRequiredNetworkType(NetworkType.Connected!)
                .Build();

            var workRequest = PeriodicWorkRequest.Builder.From<NotificationWorker>(TimeSpan.FromHours(1))
                .SetConstraints(constraints)
                .Build();
            // CancelAndReenqueue annule le travail existant et le remplace, garantissant une mise à jour propre
            WorkManager.GetInstance(this).EnqueueUniquePeriodicWork(
                "GTDNotificationWork",
                ExistingPeriodicWorkPolicy.CancelAndReenqueue!,
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
        try
        {
            var data = intent?.DataString;
            if (data?.StartsWith("gtdapp://auth", StringComparison.OrdinalIgnoreCase) == true)
            {
                // NB : intent.Package est null pour les deep links ouverts depuis un navigateur externe
                // (Chrome, Google). La sécurité repose sur le fait que seul notre manifeste déclare
                // le scheme "gtdapp" + host "auth".
                try
                {
                    global::Android.Util.Log.Info("MainActivity", $"Deep link OAuth reçu : {data}");

                    // IMPORTANT : AuthService est un service Scoped qui dépend d'IJSRuntime,
                    // uniquement résolvable dans le scope du circuit Blazor. Le résoudre ici depuis
                    // le provider racine (IPlatformApplication.Current.Services) lève une exception
                    // (captée silencieusement) et le deep link était perdu.
                    // On enfile donc l'URL dans la file statique thread-safe, consommée par
                    // AuthListener.razor avec l'instance AuthService du circuit.
                    PersonalGTD.Shared.Services.DeepLinkQueue.Enqueue(data);
                }
                catch (System.Exception inner)
                {
                    try { global::Android.Util.Log.Error("MainActivity", inner.ToString()); } catch { }
                }
            }
        }
        catch (System.Exception ex)
        {
            try { global::Android.Util.Log.Error("MainActivity", ex.ToString()); } catch { }
        }
    }

}
