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
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton<ITaskService, TaskService>();
		builder.Services.AddSingleton<IProjectService, ProjectService>();
		builder.Services.AddSingleton<IContextService, ContextService>();

		// Configuration Supabase
		var supabaseUrl = "https://jbolffzgbwqystewrxng.supabase.co"; // URL Supabase fournie par l'utilisateur
		var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Impib2xmZnpnYndxeXN0ZXdyeG5nIiwicm9sZSI6ImFub24iLCJpYXQiOjE3Nzc2Mzk1NzksImV4cCI6MjA5MzIxNTU3OX0.R2bK_kq7YmHLB5HG3Cl2xnuG6hCheXxKqeXoyqIhPbA";
		var supabaseOptions = new Supabase.SupabaseOptions { AutoConnectRealtime = true };
		builder.Services.AddSingleton(provider => new Supabase.Client(supabaseUrl, supabaseKey, supabaseOptions));

		return builder.Build();
	}
}
