using Android.App;
using Android.Content.PM;
using Android.OS;

namespace YourKitchenCo
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // This allows your app to draw behind the status bar
            AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(Window, false);
        }
    }
}
