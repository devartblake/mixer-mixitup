using MixItUp.WPF.Util;
using MixItUp.WPF.Util.Logging;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MixItUp.WPF
{
    public class TrayIcon
    {
        public NotifyIcon NotifyIcon { get; }
        public MenuItem MenuItemExit { get; }
        public MenuItem MenuItemShow { get; }
        //public MenuItem MenuItemStartMIU { get; }

        public TrayIcon()
        {
            NotifyIcon = new NotifyIcon
            {
                Visible = true,
                ContextMenu = new ContextMenu(),
                Text = "Mix-It-Up"
            };

            var iconFile = new FileInfo("Images/Mix-It-Up.ico");
            if (iconFile.Exists)
                NotifyIcon.Icon = new Icon(iconFile.FullName);
            else
                Log.Error($"Can't find tray icon at \"{iconFile.FullName}\"");

            //MenuItemStartMIU = new MenuItem(LocUtil.Get("TrayIcon_MenuItemStartMIU"), (sender, args) => MIURunner.StartMIU().Forget());
            //NotifyIcon.ContextMenu.MenuItems.Add(MenuItemStartMIU);
            //MIURunner.StartingMIU += starting => MenuItemStartMIU.Enabled = !starting;

            MenuItemShow = new MenuItem(LocUtil.Get("TrayIcon_MenuItemShow"), (sender, args) => Core.MainWindow.ActivateWindow());
            NotifyIcon.ContextMenu.MenuItems.Add(MenuItemShow);

            MenuItemExit = new MenuItem(LocUtil.Get("TrayIcon_MenuItemExit"), (sender, args) =>
            {
                Core.MainWindow.ExitRequestedFromTray = true;
                Core.MainWindow.Close();
            });
            NotifyIcon.ContextMenu.MenuItems.Add(MenuItemExit);

            NotifyIcon.MouseClick += (sender, args) =>
            {
                if (args.Button == MouseButtons.Left)
                    Core.MainWindow.ActivateWindow();
            };

            NotifyIcon.BalloonTipClicked += (sender1, e) => { Core.MainWindow.ActivateWindow(); };
        }

        public void ShowMessage(string text, string title = "Mix It Up", int duration = 5, ToolTipIcon icon = ToolTipIcon.Info)
            => NotifyIcon.ShowBalloonTip(duration, title, text, icon);
    }
}