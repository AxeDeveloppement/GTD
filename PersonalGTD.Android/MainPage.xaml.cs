namespace PersonalGTD.Android;

public partial class MainPage : ContentPage
{
    private int _appearingCount = 0;

    public MainPage()
    {
        Console.WriteLine("[DEBUG MainPage] Constructeur appelé");
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _appearingCount++;
        Console.WriteLine($"[DEBUG MainPage] OnAppearing appelé (fois: {_appearingCount})");
        
        if (blazorWebView != null)
        {
            Console.WriteLine($"[DEBUG MainPage] BlazorWebView existe, HostPage={blazorWebView.HostPage}");
        }
        else
        {
            Console.WriteLine("[DEBUG MainPage] blazorWebView est NULL");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Console.WriteLine($"[DEBUG MainPage] OnDisappearing appelé (total apparitions: {_appearingCount})");
    }

    // Note: ContentPage n'expose pas OnDestroyed() dans MAUI .NET 10
    // Le suivi de destruction est géré via OnDisappearing
}
