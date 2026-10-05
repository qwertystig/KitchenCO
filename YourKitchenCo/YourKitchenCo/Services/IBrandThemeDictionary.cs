namespace YourKitchenCo.Services;

/// <summary>
/// Marker interface implemented by every brand colour-override
/// ResourceDictionary (Resources/Styles/Themes/*.xaml) so
/// <see cref="BrandThemeService"/> can find and remove whichever one is
/// currently merged into Application.Resources before merging in a new one,
/// without having to track that state anywhere else.
/// </summary>
public interface IBrandThemeDictionary
{
}
