using Microsoft.Extensions.Logging;
using PersonalGTD.Shared.Services;
using PersonalGTD.Shared;

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

		builder.Services.AddScoped<ITaskService, TaskService>();
		builder.Services.AddScoped<IProjectService, ProjectService>();
		builder.Services.AddScoped<IContextService, ContextService>();
		builder.Services.AddScoped<ReviewStateService>();
		builder.Services.AddScoped<AuthService>();
		builder.Services.AddScoped<ISessionStorage, MobileSessionStorage>();
		builder.Services.AddScoped<NotificationService>();

		// Configuration Supabase
		// AutoConnectRealtime désactivé sur mobile pour éviter le blocage au démarrage si le WebSocket échoue
		var supabaseOptions = new Supabase.SupabaseOptions { AutoConnectRealtime = false };
		builder.Services.AddSingleton(provider => new Supabase.Client(SupabaseConfig.Url, SupabaseConfig.Key, supabaseOptions));

		return builder.Build();
	}
}
