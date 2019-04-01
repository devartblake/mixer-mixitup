using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using MixItUp.WPF.Util.LogConfig;
using MixItUp.WPF.Util.Logging;
using MixItUp.WPF.Util.Themes;

namespace MixItUp.WPF
{
    public static class Core
    {
        internal const int UpdateDelay = 100;
        private static TrayIcon _trayIcon;
        private static DateTime _startUpTime;
        //private static readonly LogWatcherManager LogWatcherManager = new LogWatcherManager();
        public static Version Version { get; set; }
        public static MainWindow MainWindow { get; set; }

        public static bool Initialized { get; private set; }

        public static TrayIcon TrayIcon => _trayIcon ?? (_trayIcon = new TrayIcon());
        
        internal static bool CanShutdown { get; set; }

#pragma warning disable 1998
        public static async void Initialize()
#pragma warning restore 1998
        {
            // LocalizeDictionay.Instance.Culture = CultureInfo.GetCultureInfo("en-us");
            _startUpTime = DateTime.Now;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            Config.Load();

            Log.Initialize();
            ConfigManager.Run();
            LogConfigUpdater.Run().Forget();
            UITheme.InitializeTheme().Forget();
            ThemeManager.Run();
            MainWindow = new MainWindow();
            MainWindow.LoadConfigSettings();
            MainWindow.Show();

            Initialized = true;
        }
    }


}
