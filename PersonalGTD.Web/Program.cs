using Microsoft.AspNetCore.Components.Web;
using PersonalGTD.Shared.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PersonalGTD.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<ITaskService, TaskService>();
builder.Services.AddSingleton<IProjectService, ProjectService>();
builder.Services.AddSingleton<IContextService, ContextService>();
builder.Services.AddSingleton<ReviewStateService>();

// Configuration Supabase
var supabaseUrl = "https://jbolffzgbwqystewrxng.supabase.co"; // URL Supabase fournie par l'utilisateur
var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Impib2xmZnpnYndxeXN0ZXdyeG5nIiwicm9sZSI6ImFub24iLCJpYXQiOjE3Nzc2Mzk1NzksImV4cCI6MjA5MzIxNTU3OX0.R2bK_kq7YmHLB5HG3Cl2xnuG6hCheXxKqeXoyqIhPbA";
var supabaseOptions = new Supabase.SupabaseOptions { AutoConnectRealtime = true };
builder.Services.AddSingleton(provider => new Supabase.Client(supabaseUrl, supabaseKey, supabaseOptions));

await builder.Build().RunAsync();
