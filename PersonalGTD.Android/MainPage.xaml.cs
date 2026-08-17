using Microsoft.AspNetCore.Components.WebView.Maui;
using PersonalGTD.Android.Components;

namespace PersonalGTD.Android;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		// Configuration programmatique du composant racine Blazor.
		// L'attribut XAML RootComponent n'existe pas sur BlazorWebView dans MAUI 10.
		// Il faut utiliser la collection RootComponents pour lier le composant au div #app de wwwroot/index.html.
		blazorWebView.RootComponents.Add(new RootComponent { Selector = "#app", ComponentType = typeof(Routes) });
	}
}





