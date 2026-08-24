using Android.App;
using Android.Content;
using Android.OS;

namespace PersonalGTD.Android;

[Activity(NoHistory = true, LaunchMode = global::Android.Content.PM.LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { global::Android.Content.Intent.ActionView },
              Categories = new[] { global::Android.Content.Intent.CategoryDefault, global::Android.Content.Intent.CategoryBrowsable },
              DataScheme = "gtdapp",
              DataHost = "auth")]
public class AuthCallbackActivity : global::Android.App.Activity
{
    protected override void OnCreate(global::Android.OS.Bundle? savedInstanceState)
    {
        try
        {
            base.OnCreate(savedInstanceState);

            if (Intent?.DataString != null)
            {
                // Rediriger vers MainActivity avec les données du lien
                var intent = new global::Android.Content.Intent(this, typeof(MainActivity));
                intent.SetData(Intent.Data);
                intent.AddFlags(global::Android.Content.ActivityFlags.ClearTop | global::Android.Content.ActivityFlags.SingleTop);
                StartActivity(intent);
            }
        }
        catch (System.Exception ex)
        {
            try { global::Android.Util.Log.Error("AuthCallbackActivity", ex.ToString()); } catch { }
        }
        finally
        {
            Finish();
        }
    }
}
