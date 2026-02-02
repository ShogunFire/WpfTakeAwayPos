namespace RestaurantPOS.Services.Interfaces
{
    public interface IThemeService
    {
        void SetTheme(string themeName);
        string GetCurrentTheme();
    }
}
