using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using MixItUp.WPF.Util.Logging;
using Newtonsoft.Json;
using MediaColor = System.Windows.Media.Color;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace MixItUp.WPF.Util
{
    public static class Helper
    {
        public static double DpiScalingX = 1.0, DpiScalingY = 1.0;

        public static readonly Dictionary<string, string> LanguageDict = new Dictionary<string, string>
        {
            {"English", "enUS"},
            {"English (Great Britain)", "enGB" }
        };

        public static readonly List<string> LatinLanguages = new List<string>
        {
            "enUS",
            "enGB"
        };

        private static bool? _mixItUpDirExists;

        public static bool MixItUpDirExists
        {
            get
            {
                if (!_mixItUpDirExists.HasValue)
                    _mixItUpDirExists = FindMixItUpDir();
                return _mixItUpDirExists.Value;
            }
        }

        public static Version GetCurrentVersion() => Assembly.GetExecutingAssembly().GetName().Version;

        public static bool IsHex(IEnumerable<char> chars)
            => chars.All(c => ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')));

        public static double DrawProbability(int copies, int deck, int draw)
            => 1 - (BinomialCoefficient(deck - copies, draw) / BinomialCoefficient(deck, draw));

        public static double BinomialCoefficient(int n, int k)
        {
            double result = 1;
            for (var i = 1; i <= k; i++)
            {
                result *= n - (k - i);
                result /= i;
            }
            return result;
        }

        public static string ShowSaveFileDialog(string filename, string ext)
        {
            var defaultExt = $"*.{ext}";
            var saveFileDialog = new SaveFileDialog
            {
                FileName = filename,
                DefaultExt = defaultExt,
                Filter = $"{ext.ToUpper()} ({defaultExt})|{defaultExt}"
            };
            return saveFileDialog.ShowDialog() == true ? saveFileDialog.FileName : null;
        }

        public static string GetValidFilePath(string dir, string name, string extension)
        {
            var validDir = RemoveInvalidPathChars(dir);
            if (!Directory.Exists(validDir))
                Directory.CreateDirectory(validDir);

            if (!extension.StartsWith("."))
                extension = "." + extension;

            var path = validDir + "\\" + RemoveInvalidFileNameChars(name);
            if (File.Exists(path + extension))
            {
                var num = 1;
                while (File.Exists(path + "_" + num + extension))
                    num++;
                path += "_" + num;
            }

            return path + extension;
        }

        public static string RemoveInvalidPathChars(string s) => RemoveChars(s, Path.GetInvalidPathChars());
        public static string RemoveInvalidFileNameChars(string s) => RemoveChars(s, Path.GetInvalidFileNameChars());
        public static string RemoveChars(string s, char[] c) => new Regex($"[{Regex.Escape(new string(c))}]").Replace(s, "");

        //http://stackoverflow.com/questions/23927702/move-a-folder-from-one-drive-to-another-in-c-sharp
        public static void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            var files = Directory.GetFiles(sourceFolder);
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                var dest = Path.Combine(destFolder, name);
                File.Copy(file, dest);
            }
            var folders = Directory.GetDirectories(sourceFolder);
            foreach (var folder in folders)
            {
                var name = Path.GetFileName(folder);
                var dest = Path.Combine(destFolder, name);
                CopyFolder(folder, dest);
            }
        }

        //http://stackoverflow.com/questions/3769457/how-can-i-remove-accents-on-a-string
        public static string RemoveDiacritics(string src, bool compatNorm)
        {
            var sb = new StringBuilder();
            foreach (var c in src.Normalize(compatNorm ? NormalizationForm.FormKD : NormalizationForm.FormD))
            {
                switch (CharUnicodeInfo.GetUnicodeCategory(c))
                {
                    case UnicodeCategory.NonSpacingMark:
                    case UnicodeCategory.SpacingCombiningMark:
                    case UnicodeCategory.EnclosingMark:
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        public static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
        }

        public static DateTime FromUnixTime(long unixTime)
            => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Add(TimeSpan.FromSeconds(unixTime)).ToLocalTime();

        public static DateTime FromUnixTime(string unixTime)
            => long.TryParse(unixTime, out var time) ? FromUnixTime(time) : DateTime.Now;

        private static bool FindMixItUpDir()
        {
            if (string.IsNullOrEmpty(Config.Instance.MixItUpDirectory)
               || !File.Exists(Config.Instance.MixItUpDirectory + @"\MixItUp.exe"))
            {
                using (var hsDirKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MixItUp"))
                {
                    if (hsDirKey == null)
                        return false;
                    var hsDir = (string)hsDirKey.GetValue("InstallLocation");

                    //verify the install location actually is correct (possibly moved?)
                    if (!File.Exists(hsDir + @"\MixItUp.exe"))
                        return false;
                    Config.Instance.MixItUpDirectory = hsDir;
                    Config.Save();
                }
            }
            return true;
        }

        public static double GetScaledXPos(double left, int width, double ratio) => (width * ratio * left) + (width * (1 - ratio) / 2);
             
        public static MetroWindow GetParentWindow(DependencyObject current) => GetVisualParent<MetroWindow>(current);

        public static T GetVisualParent<T>(DependencyObject current)
        {
            var parent = VisualTreeHelper.GetParent(current);
            while (parent != null && !(parent is T))
                parent = VisualTreeHelper.GetParent(parent);
            return (T)(object)parent;
        }

        public static bool IsWindows10()
        {
            try
            {
                var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                return reg != null && ((string)reg.GetValue("ProductName")).Contains("Windows 10");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return false;
            }
        }
        public static bool IsWindows8()
        {
            try
            {
                var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                return reg != null && ((string)reg.GetValue("ProductName")).Contains("Windows 8");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return false;
            }
        }

        public static bool TryOpenUrl(string url, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "")
        {
            try
            {
                Log.Info("[Helper.TryOpenUrl] " + url, memberName, sourceFilePath);
                Process.Start(url);
                return true;
            }
            catch (Exception e)
            {
                Log.Error("[Helper.TryOpenUrl] " + e, memberName, sourceFilePath);
                return false;
            }
        }

    }
}
