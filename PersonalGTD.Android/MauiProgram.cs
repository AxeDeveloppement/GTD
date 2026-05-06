using Microsoft.Extensions.Logging;
using PersonalGTD.Shared.Services;

namespace PersonalGTD.Android;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebView, Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebViewHandler>();
            })
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		builder.Services.AddScoped<ITaskService, TaskService>();
		builder.Services.AddScoped<IProjectService, ProjectService>();
		builder.Services.AddScoped<IContextService, ContextService>();
		builder.Services.AddScoped<ReviewStateService>();
		builder.Services.AddScoped<AuthService>();
		builder.Services.AddScoped<NotificationService>();

		// Configuration Supabase
		var supabaseUrl = "https://jbolffzgbwqystewrxng.supabase.co"; // URL Supabase fournie par l'utilisateur
		var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Impib2xmZnpnYndxeXN0ZXdyeG5nIiwicm9sZSI6ImFub24iLCJpYXQiOjE3Nzc2Mzk1NzksImV4cCI6MjA5MzIxNTU3OX0.R2bK_kq7YmHLB5HG3Cl2xnuG6hCheXxKqeXoyqIhPbA";
		var supabaseOptions = new Supabase.SupabaseOptions { AutoConnectRealtime = true };
		builder.Services.AddSingleton(provider => new Supabase.Client(supabaseUrl, supabaseKey, supabaseOptions));

		return builder.Build();
	}
}


