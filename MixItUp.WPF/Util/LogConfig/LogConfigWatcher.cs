using MixItUp.WPF.Util.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MixItUp.WPF.Util.Extensions;
using static MixItUp.WPF.Util.LogConfig.LogConfigConstants;

namespace MixItUp.WPF.Util.LogConfig
{
    internal class LogConfigWatcher
    {
        private static FileSystemWatcher _fileWatcher;

        public static void Start()
        {
            try
            {
                _fileWatcher = new FileSystemWatcher(MixItUpAppData, LogConfigFile)
                {
                    EnableRaisingEvents = true
                };
                _fileWatcher.Changed += (sender, args) => LogConfigUpdater.Run().Forget();
                _fileWatcher.Deleted += (sender, args) => LogConfigUpdater.Run().Forget();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public static void Pause()
        {
            if (_fileWatcher != null)
                _fileWatcher.EnableRaisingEvents = false;
        }

        public static void Continue()
        {
            if (_fileWatcher != null)
                _fileWatcher.EnableRaisingEvents = true;
        }
    }
}
