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

        // Restore the person's chosen brand colour theme (Settings > App
        // Theme) before any page is constructed, so every StaticResource /
        // AppThemeBinding lookup during startup resolves against the right
        // palette from the very first frame. persist:false — we're just
        // re-applying what was already saved, not making a new choice.
        BrandThemeService.Apply(BrandThemeService.SavedTheme, persist: false);

        // Wired here (not via constructor injection in every ViewModel) —
        // see AlertService's own doc comment for why.
        AlertService.Instance = serviceProvider.GetRequiredService<IAlertService>();

        var loginPage = serviceProvider.GetRequiredService<Views.LoginPage>();
        MainPage = new NavigationPage(loginPage);
    }
}
