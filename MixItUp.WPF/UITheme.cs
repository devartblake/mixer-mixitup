using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using MixItUp.WPF.Util.Logging;
using MahApps.Metro;
using Point = System.Windows.Point;
using Application = System.Windows.Application;
using MixItUp.WPF.Util;

namespace MixItUp.WPF
{
    public static class UITheme
    {
        private const string WindowAccentName = "Windows Accent";
        private const string DefaultAccentName = "Green";
        private static Color _currentWindowsAccent = SystemParameters.WindowGlassColor;

        public static AppTheme CurrentTheme => ThemeManager.AppThemes.FirstOrDefault(t => t.Name == Config.Instance.AppTheme.ToString()) ?? ThemeManager.DetectAppStyle().Item1;
        public static Accent CurrentAccent => ThemeManager.Accents.FirstOrDefault(a => a.Name == Config.Instance.AccentName) ?? ThemeManager.GetAccent(DefaultAccentName);

        public static async Task InitializeTheme()
        {
            UpdateIconColors();
            if (Helper.IsWindows8() || Helper.IsWindows10())
                await CreateWindowsAccentStyle();
            else if(Config.Instance.AccentName == WindowAccentName)
            {
                // In case if somehow user will get "Windows Accent" on Windows which not support this.
                // (For example move whole HDT on diffrent machine instead of fresh install)
                Config.Instance.AccentName = DefaultAccentName;
                Config.Save();
            }
            ThemeManager.ChangeAppStyle(Application.Current, CurrentAccent, CurrentTheme);
        }

        public static async Task UpdateTheme()
        {
            if (Config.Instance.AccentName == WindowAccentName)
                await CreateWindowsAccentStyle();

            ThemeManager.ChangeAppStyle(Application.Current, CurrentAccent, CurrentTheme);
            UpdateIconColors();
        }

        public static async Task UpdateAccent()
        {
            if (Config.Instance.AccentName == WindowAccentName)
                await CreateWindowsAccentStyle();

            ThemeManager.ChangeAppStyle(Application.Current, CurrentAccent, CurrentTheme);
        }

        private static Task CreateWindowsAccentStyle()
        {
            throw new NotImplementedException();
        }

        private static void UpdateIconColors()
        {
            throw new NotImplementedException();
        }
    }
}
