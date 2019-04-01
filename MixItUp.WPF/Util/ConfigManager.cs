using MixItUp.WPF.Util.Logging;
using System;
using System.IO;
using System.Linq;

namespace MixItUp.WPF.Util
{
    public static class ConfigManager
    {
        public static Version UpdatedVersion { get; private set; }
        public static Version PreviousVersion { get; private set; }

        public static void Run()
        {
            PreviousVersion = string.IsNullOrEmpty(Config.Instance.CreatedByVersion) ? null : new Version(Config.Instance.CreatedByVersion);
            var currentVersion = Helper.GetCurrentVersion();
            if(currentVersion != null)
            {
                // Assign current version to the config instance so that it will be saved when the config
                // is rewritten to disk, thereby telling us what version of the application created it
                Config.Instance.CreatedByVersion = currentVersion.ToString();
            }
            ConvertLegacyConfig(currentVersion, PreviousVersion);

            if (Config.Instance.SelectedTags.Count == 0)
                Config.Instance.SelectedTags.Add("All");

#if (!SQUIRREL)
            if (!Directory.Exists(Config.Instance.DataDir))
                Config.Instance.Reset(nameof(Config.DataDirPath));
#endif
        }

        // Logic for dealing with legacy config file semantics
        // Use difference of versions to determine what should be done
        private static void ConvertLegacyConfig(Version currentVersion, Version configVersion)
        {
            var converted = false;

            var v0_3_21 = new Version(0, 3, 21, 0);

            if (converted)
            {
                Log.Info("changed config values");
                Config.Save();
            }

            if (configVersion != null && currentVersion > configVersion)
                UpdatedVersion = currentVersion;
        }
    }
}
