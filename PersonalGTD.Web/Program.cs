using Microsoft.AspNetCore.Components.Web;
using PersonalGTD.Shared.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PersonalGTD.Web;
using System;

Console.WriteLine("🚀 Démarrage de l'application Blazor WASM...");

var builder = WebAssemblyHostBuilder.CreateDefault(args);

try 
{
    Console.WriteLine("⚙️ Configuration des composants racines...");
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");

    Console.WriteLine("💉 Injection des services...");
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

    builder.Services.AddScoped<ITaskService, TaskService>();
    builder.Services.AddScoped<IProjectService, ProjectService>();
    builder.Services.AddScoped<IContextService, ContextService>();
    builder.Services.AddScoped<ReviewStateService>();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<NotificationService>();

    Console.WriteLine("🔗 Configuration de Supabase...");
    var supabaseUrl = builder.Configuration["Supabase:Url"] 
        ?? "https://jbolffzgbwqystewrxng.supabase.co";
    var supabaseKey = builder.Configuration["Supabase:Key"]
        ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Impib2xmZnpnYndxeXN0ZXdyeG5nIiwicm9sZSI6ImFub24iLCJpYXQiOjE3Nzc2Mzk1NzksImV4cCI6MjA5MzIxNTU3OX0.R2bK_kq7YmHLB5HG3Cl2xnuG6hCheXxKqeXoyqIhPbA";
    
    var supabaseOptions = new Supabase.SupabaseOptions 
    { 
        AutoConnectRealtime = false,
        AutoRefreshToken = true
    };
    
    builder.Services.AddSingleton(provider => {
        Console.WriteLine("✨ Création du client Supabase Singleton...");
        return new Supabase.Client(supabaseUrl, supabaseKey, supabaseOptions);
    });

    Console.WriteLine("🏗️ Build de l'application...");
    var host = builder.Build();

    Console.WriteLine("🟢 Application prête, lancement du RunAsync...");
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine("❌ ERREUR FATALE LORS DU DÉMARRAGE :");
    Console.WriteLine(ex.ToString());
    
    // Tenter d'afficher l'erreur dans le DOM si possible
    try {
        // En WASM, on peut parfois utiliser JS directement ici
    } catch { }
    
    throw;
}
