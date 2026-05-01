using Microsoft.AspNetCore.Components.Web;
using PersonalGTD.Shared.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PersonalGTD.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<ITaskService, TaskService>();

await builder.Build().RunAsync();
