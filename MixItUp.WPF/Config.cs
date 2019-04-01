using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using MixItUp.WPF.Enums;
using MixItUp.WPF.Util;
using MixItUp.WPF.Util.Logging;

namespace MixItUp.WPF
{
    public class Config
    {
        #region Settings

        private static Config _config;

        public static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\mixer-mixitup";

#if (!SQUIRREL)
        [DefaultValue(".")]
        public string DataDirPath = ".";

        //updating from <= 0.5.1: 
        //SaveConfigInAppData and SaveDataInAppData are set to SaveInAppData AFTER the config isloaded
        //=> Need to be null to avoid creating new config in appdata if config is stored locally.
        [DefaultValue(true)]
        public bool? SaveConfigInAppData;

        [DefaultValue(true)]
        public bool? SaveDataInAppData = null;

        [DefaultValue(true)]
        public bool SaveInAppData = true;
#endif

        [DefaultValue("Blue")]
        public string AccentName = "Blue";

        [DefaultValue(MetroTheme.BaseLight)]
        public MetroTheme AppTheme = MetroTheme.BaseLight;

        [DefaultValue(false)]
        public bool AdditionalOverlayTooltips = false;

        [DefaultValue(false)]
        public bool OverlaySetTooltips = false;

        [DefaultValue(false)]
        public bool AdvancedOptions = false;

        [DefaultValue(true)]
        public bool AlwaysOverwriteLogConfig = true;

        [DefaultValue(true)]
        public bool CheckForUpdates = true;

        [DefaultValue(true)]
        public bool ClearLogFileAfterGame = true;

        [DefaultValue(false)]
        public bool CloseToTray = false;
        
        [DefaultValue("")]
        public string CreatedByVersion = "";

        [DefaultValue(null)]
        public DateTime? CustomDisplayedTimeFrame = null;

        [DefaultValue(-1)]
        public int CustomHeight = -1;

        [DefaultValue(-1)]
        public int CustomWidth = -1;

        [DefaultValue(DateFormat.DayMonthYear)]
        public DateFormat SelectedDateFormat = DateFormat.DayMonthYear;

        [DefaultValue(false)]
        public bool Debug = false;

        [DefaultValue(true)]
        public bool GoogleAnalytics = true;

        [DefaultValue(@"C:\Program Files (x86)\mix-it-up")]
        public string MixItUpDirectory = @"C:\Program Files (x86)\mix-it-up";

        [DefaultValue("Logs")]
        public string MixItUpLogsDirectoryName = "Logs";

        [DefaultValue("Mix-It-Up")]
        public string MixItUpWindowName = "Mix-It-up";

        [DefaultValue("00000000-0000-0000-0000-000000000000")]
        public string Id = Guid.Empty.ToString();

        [DefaultValue(new ConfigWarning[] { })]
        public ConfigWarning[] IgnoredConfigWarnings = { };

        [DefaultValue(Language.enUS)]
        public Language Localization = Language.enUS;

        [DefaultValue(false)]
        public bool LogConfigConsolePrinting = false;

        [DefaultValue(0)]
        public int LogLevel = 0;

        [DefaultValue(false)]
        public bool MinimizeToTray = false;

        [XmlArray(ElementName = "SelectedTags")]
        [XmlArrayItem(ElementName = "Tag")]
        public List<string> SelectedTags = new List<string>();

        [DefaultValue(false)]
        public bool StartMinimized = false;

        [DefaultValue(false)]
        public bool StartWithWindows = false;

        [DefaultValue(true)]
        public bool StatsAutoRefresh = true;

        [DefaultValue(false)]
        public bool StatsClassOverviewIsExpanded = false;

        [DefaultValue(620)]
        public int WindowHeight = 620;

        [DefaultValue(550)]
        public int WindowWidth = 550;

        [DefaultValue("#696969")]
        public string WindowsBackgroundHex = "#696969";

        [DefaultValue(false)]
        public bool WindowsTopmost = false;

        #endregion

        #region deprecated

        [DefaultValue("BaseLight")]
        public string ThemeName = "BaseLight";

        #endregion

        #region Properties

        [Obsolete]
        public string HomeDir
        {
            get
            {
#if(SQUIRREL)
                return AppDataPath+ "\\";
#else
                return Instance.SaveInAppData ? AppDataPath + "\\" : string.Empty;
#endif
            }
        }

        public string BackupDir => Path.Combine(DataDir, "Backups");

        public string ConfigPath => Instance.ConfigDir + "config.xml";

        public string ConfigDir
        {
            get
            {
#if (SQUIRREL)
				return AppDataPath + "\\";
#else
                return Instance.SaveConfigInAppData == false ? string.Empty : AppDataPath + "\\";
#endif
            }
        }

        public string DataDir
        {
            get
            {
#if (SQUIRREL)
				return AppDataPath + "\\";
#else
                return Instance.SaveDataInAppData == false ? DataDirPath + "\\" : AppDataPath + "\\";
#endif
            }
        }

        public static Config Instance
        {
            get
            {
                if (_config != null)
                    return _config;
                _config = new Config();
                _config.ResetAll();
                _config.SelectedTags = new List<string>();
                return _config;
            }
        }

        #endregion

        #region Misc

        public event Action<ConfigWarning> OnConfigWarning;

        private Config()
        {
        }

        public void CheckConfigWarnings()
        {
            var configWarnings = Enum.GetValues(typeof(ConfigWarning)).OfType<ConfigWarning>();
            var fields = GetType().GetFields();
            foreach (var warning in configWarnings)
            {
                var prop = fields.First(x => x.Name == warning.ToString());
                var defaultValue = (DefaultValueAttribute)prop.GetCustomAttributes(typeof(DefaultValueAttribute), false).First();
                var value = prop.GetValue(this);
                if (!value.Equals(defaultValue.Value))
                {
                    var ignored = IgnoredConfigWarnings.Contains(warning);
                    Log.Warn($"{warning}={value}, default={defaultValue.Value} ignored={ignored}");
                    if (!ignored)
                        OnConfigWarning?.Invoke(warning);
                }
            }
        }

        public static void Save() => XmlManager<Config>.Save(Instance.ConfigPath, Instance);

        public static void SaveBackup(bool deleteOriginal = false)
        {
            var configPath = Instance.ConfigPath;

            if (!File.Exists(configPath))
                return;

            File.Copy(configPath, configPath + DateTime.Now.ToFileTime());

            if (deleteOriginal)
                File.Delete(configPath);
        }

        public static void Load()
        {
            var foundConfig = false;
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                var config = Path.Combine(AppDataPath, "config.xml");
#if (SQUIRREL)
				if(File.Exists(config))
				{
					_config = XmlManager<Config>.Load(config);
					foundConfig = true;
				}
#else
                if (File.Exists("config.xml"))
                {
                    _config = XmlManager<Config>.Load("config.xml");
                    foundConfig = true;
                }
                else if (File.Exists(config))
                {
                    _config = XmlManager<Config>.Load(config);
                    foundConfig = true;
                }
                else if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)))
                    //save locally if appdata doesn't exist (when e.g. not on C)
                    Instance.SaveConfigInAppData = false;
#endif
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                try
                {
                    if (File.Exists("config.xml"))
                    {
                        File.Move("config.xml", Helper.GetValidFilePath(".", "config_corrupted", "xml"));
                    }
                    else if (File.Exists(AppDataPath + @"\config.xml"))
                    {
                        File.Move(AppDataPath + @"\config.xml", Helper.GetValidFilePath(AppDataPath, "config_corrupted", "xml"));
                    }
                }
                catch (Exception ex1)
                {
                    Log.Error(ex1);
                }
                _config = BackupManager.TryRestore<Config>("config.xml");
            }

            if (!foundConfig)
            {
                if (Instance.ConfigDir != string.Empty)
                    Directory.CreateDirectory(Instance.ConfigDir);
                Save();
            }
#if (!SQUIRREL)
            else if (Instance.SaveConfigInAppData != null)
            {
                if (Instance.SaveConfigInAppData.Value) //check if config needs to be moved
                {
                    if (File.Exists("config.xml"))
                    {
                        Directory.CreateDirectory(Instance.ConfigDir);
                        SaveBackup(true); //backup in case the file already exists
                        File.Move("config.xml", Instance.ConfigPath);
                        Log.Info("Moved config to appdata");
                    }
                }
                else if (File.Exists(AppDataPath + @"\config.xml"))
                {
                    SaveBackup(true); //backup in case the file already exists
                    File.Move(AppDataPath + @"\config.xml", Instance.ConfigPath);
                    Log.Info("Moved config to local");
                }
            }
#endif
            if (Instance.Id == Guid.Empty.ToString())
            {
                Instance.Id = Guid.NewGuid().ToString();
                Save();
            }
        }

        public void ResetAll()
        {
            foreach (var field in GetType().GetFields())
            {
                var attr = (DefaultValueAttribute)field.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault();
                if (attr != null)
                    field.SetValue(this, attr.Value);
            }
        }

        public void Reset(string name)
        {
            var proper = GetType().GetFields().First(x => x.Name == name);
            var attr = (DefaultValueAttribute)proper.GetCustomAttributes(typeof(DefaultValueAttribute), false).First();
            proper.SetValue(this, attr.Value);
        }

        [AttributeUsage(AttributeTargets.All, Inherited = false)]
        private sealed class DefaultValueAttribute : Attribute
        {
            // This is a positional argument
            public DefaultValueAttribute(object value)
            {
                Value = value;
            }

            public object Value { get; }
        }

#endregion

    }
}
