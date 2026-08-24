using System;
using Android.Content;
using Android.Util;
using AndroidX.Work;

namespace PersonalGTD.Android
{
    // Worker protégé par un try/catch pour éviter la propagation d'exceptions vers la couche Java
    public class NotificationWorker : Worker
    {
        public NotificationWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
        }

        public override Result DoWork()
        {
            try
            {
                // Log minimal pour tracer l'exécution
                Log.Info("NotificationWorker", "DoWork started.");

                // TODO: Restaurer ici la logique métier existante (envoi de notification, accès aux services partagés, etc.)

                return Result.InvokeSuccess();
            }
            catch (Exception ex)
            {
                try { Log.Error("NotificationWorker", ex.ToString()); } catch { }
                return Result.InvokeFailure();
            }
        }
    }
}
