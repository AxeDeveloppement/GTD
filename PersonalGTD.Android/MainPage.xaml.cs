using Microsoft.AspNetCore.Components.WebView.Maui;

namespace PersonalGTD.Android;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
	       InitializeComponent();

	       blazorWebView.RootComponents.Add(new Microsoft.AspNetCore.Components.WebView.Maui.RootComponent
	       {
	           Selector = "#app",
	           ComponentType = typeof(Components.Routes)
	       });
	}
}





