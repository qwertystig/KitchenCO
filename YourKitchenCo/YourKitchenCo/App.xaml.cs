using Microsoft.Extensions.DependencyInjection;
using YourKitchenCo.Services;

namespace YourKitchenCo;

public partial class App : Application
{
    // IServiceProvider is injected (not LoginPage directly) so that
    // InitializeComponent() — which merges Colors.xaml/Styles.xaml into
    // Application.Resources — runs BEFORE LoginPage.xaml gets parsed.
    // Resolving LoginPage as a constructor parameter instead would build it
    // (and parse its XAML, which needs those StaticResources) before this
    // constructor body even starts, causing "StaticResource not found".
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        // Wired here (not via constructor injection in every ViewModel) —
        // see AlertService's own doc comment for why.
        AlertService.Instance = serviceProvider.GetRequiredService<IAlertService>();

        var loginPage = serviceProvider.GetRequiredService<Views.LoginPage>();
        MainPage = new NavigationPage(loginPage);
    }
}
