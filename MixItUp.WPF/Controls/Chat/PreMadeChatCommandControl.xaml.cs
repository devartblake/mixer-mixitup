﻿using Mixer.Base.Util;
using MixItUp.Base;
using MixItUp.Base.Commands;
using MixItUp.Base.ViewModel.User;
using MixItUp.Base.ViewModel.Chat;
using MixItUp.WPF.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MixItUp.Base.ViewModel.Requirement;

namespace MixItUp.WPF.Controls.Chat
{
    /// <summary>
    /// Interaction logic for PreMadeChatCommandControl.xaml
    /// </summary>
    public partial class PreMadeChatCommandControl : UserControl
    {
        private LoadingWindowBase window;
        private PreMadeChatCommand command;
        private PreMadeChatCommandSettings setting;

        public PreMadeChatCommandControl()
        {
            InitializeComponent();

            this.PermissionsComboBox.ItemsSource = RoleRequirementViewModel.BasicUserRoleAllowedValues;
        }

        public void Initialize(LoadingWindowBase window, PreMadeChatCommand command)
        {
            this.window = window;
            this.DataContext = this.command = command;

            this.PermissionsComboBox.SelectedItem = this.command.UserRoleRequirementString;
            this.CooldownTextBox.Text = this.command.Requirements.Cooldown.Amount.ToString();

            this.setting = ChannelSession.Settings.PreMadeChatCommandSettings.FirstOrDefault(c => c.Name.Equals(this.command.Name));
            if (this.setting == null)
            {
                this.setting = new PreMadeChatCommandSettings(this.command);
                ChannelSession.Settings.PreMadeChatCommandSettings.Add(this.setting);
            }
        }

        private void PermissionsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combobox = (ComboBox)sender;
            PreMadeChatCommand command = (PreMadeChatCommand)combobox.DataContext;

            command.Requirements.Role.MixerRole = EnumHelper.GetEnumValueFromString<MixerRoleEnum>((string)combobox.SelectedItem);

            this.UpdateSetting();
        }

        private void CooldownTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            PreMadeChatCommand command = (PreMadeChatCommand)textbox.DataContext;

            int cooldown = 0;
            if (!string.IsNullOrEmpty(textbox.Text) && int.TryParse(textbox.Text, out cooldown))
            {
                cooldown = Math.Max(cooldown, 0);
            }
            command.Requirements.Cooldown.Amount = cooldown;
            textbox.Text = command.Requirements.Cooldown.Amount.ToString();

            this.UpdateSetting();
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            ChatCommand command = (ChatCommand)button.DataContext;

            await this.window.RunAsyncOperation(async () =>
            {
                UserViewModel currentUser = await ChannelSession.GetCurrentUser();
                await command.Perform(currentUser, new List<string>() { "@" + currentUser.UserName });
            });
        }

        private void EnableDisableToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton button = (ToggleButton)sender;
            CommandBase command = (CommandBase)button.DataContext;
            command.IsEnabled = true;

            this.UpdateSetting();
        }

        private void EnableDisableToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton button = (ToggleButton)sender;
            CommandBase command = (CommandBase)button.DataContext;
            command.IsEnabled = false;

            this.UpdateSetting();
        }

        private void UpdateSetting()
        {
            if (this.setting != null)
            {
                this.setting.Permissions = command.Requirements.Role.MixerRole;
                this.setting.Cooldown = command.Requirements.Cooldown.Amount;
                this.setting.IsEnabled = command.IsEnabled;
            }
        }
    }
}
