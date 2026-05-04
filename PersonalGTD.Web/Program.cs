using Microsoft.AspNetCore.Components.Web;
using PersonalGTD.Shared.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PersonalGTD.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IContextService, ContextService>();
builder.Services.AddScoped<ReviewStateService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<NotificationService>();

// Configuration Supabase (lue depuis wwwroot/appsettings.json)
var supabaseUrl = builder.Configuration["Supabase:Url"] 
    ?? "https://jbolffzgbwqystewrxng.supabase.co";
var supabaseKey = builder.Configuration["Supabase:Key"]
    ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Impib2xmZnpnYndxeXN0ZXdyeG5nIiwicm9sZSI6ImFub24iLCJpYXQiOjE3Nzc2Mzk1NzksImV4cCI6MjA5MzIxNTU3OX0.R2bK_kq7YmHLB5HG3Cl2xnuG6hCheXxKqeXoyqIhPbA";
var supabaseOptions = new Supabase.SupabaseOptions { AutoConnectRealtime = true };
builder.Services.AddSingleton(provider => new Supabase.Client(supabaseUrl, supabaseKey, supabaseOptions));

await builder.Build().RunAsync();
