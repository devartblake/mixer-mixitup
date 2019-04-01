using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MixItUp.WPF.Util.Themes
{
    public class ThemeManager
    {
        private static string CustomThemeDir => Path.Combine(Config.AppDataPath, @"Themes\Bars");
        private const string ThemeDir = @"Images\Themes\Bars";
        private const string ThemeRegex = @"[a-zA-Z]+";

        public static List<Theme> Themes = new List<Theme>();

        public static Theme CurrentTheme { get; private set; }

        public static void Run()
        {
            LoadThemes(CustomThemeDir);
            LoadThemes(ThemeDir);        
        }

        private static void LoadThemes(string dir)
        {
            var dirInfo = new DirectoryInfo(dir);
            if (!dirInfo.Exists)
                return;
            foreach(var di in dirInfo.GetDirectories())
            {

            }
        }

        public static Theme FindTheme(string name)
            => string.IsNullOrWhiteSpace(name) ? null : Themes.FirstOrDefault(x => x.Name.ToLowerInvariant() == name.ToLowerInvariant());


    }
}
