using AgroTrack.Enums;

namespace AgroTrack.Services.Interfaces;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    event EventHandler<AppTheme>? ThemeChanged;

    void SetTheme(AppTheme theme);
    void ToggleTheme();
}
