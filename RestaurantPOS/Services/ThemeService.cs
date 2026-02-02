using RestaurantPOS.Services.Interfaces;
using System;
using System.Linq;
using System.Windows;

namespace RestaurantPOS.Services
{
    public class ThemeService : IThemeService
    {
        private string _currentTheme = "LightTheme";
        private readonly string _themeBasePath = "Resources/Themes/";

        public ThemeService()
        {
            _currentTheme = "LightTheme";
        }

        public void SetTheme(string themeName)
        {
            if (_currentTheme == themeName)
                return;

            var themePath = $"{_themeBasePath}{themeName}.xaml";
            
            try
            {
                var resourceDictionary = new ResourceDictionary
                {
                    Source = new System.Uri(themePath, System.UriKind.Relative)
                };

                // Remove old theme
                var oldTheme = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source?.OriginalString.Contains(_themeBasePath) == true);
                
                if (oldTheme != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(oldTheme);
                }

                // Add new theme
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
                _currentTheme = themeName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading theme {themeName}: {ex.Message}");
            }
        }

        public string GetCurrentTheme()
        {
            return _currentTheme;
        }
    }
}
