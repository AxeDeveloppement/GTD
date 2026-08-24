using Android.App;
using global::Android.Runtime;

namespace PersonalGTD.Android;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
    : base(handle, ownership)
    {
        try
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { global::Android.Util.Log.Error("MainApplication", "AppDomain unhandled: " + (e.ExceptionObject?.ToString() ?? "null")); } catch { }
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                try { global::Android.Util.Log.Error("MainApplication", "UnobservedTaskException: " + e.Exception?.ToString()); } catch { }
            };
        }
        catch
        {
            // Ne pas laisser l'enregistrement des handlers échouer
        }
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
