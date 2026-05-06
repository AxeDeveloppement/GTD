using Microsoft.AspNetCore.Components.WebView.Maui;

namespace PersonalGTD.Android;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		try {
            InitializeComponent();
            
            blazorWebView.RootComponents.Add(new Microsoft.AspNetCore.Components.WebView.Maui.RootComponent
            {
                Selector = "#app",
                ComponentType = typeof(Components.Routes)
            });
        } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"CRASH INIT: {ex.Message}");
        }
	}

}





