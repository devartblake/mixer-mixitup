﻿using Mixer.Base.Interactive;
using Mixer.Base.Model.Interactive;
using Mixer.Base.Model.OAuth;
using MixItUp.Base;
using MixItUp.Base.Actions;
using MixItUp.Base.Commands;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Import;
using MixItUp.Base.ViewModel.User;
using MixItUp.Desktop.Database;
using MixItUp.WPF.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace MixItUp.WPF.Windows.Wizard
{
    public class SoundwaveProfileItem
    {
        public string Name { get; set; }
        public bool AddProfile { get; set; }
        public bool CanBeImported { get; set; }
    }

    /// <summary>
    /// Interaction logic for NewUserWizardWindow.xaml
    /// </summary>
    public partial class NewUserWizardWindow : LoadingWindowBase
    {
        private const string SoundwaveInteractiveGameName = "Soundwave Interactive Soundboard";
        private const string SoundwaveInteractiveCooldownGroupName = "Soundwave Buttons";

        private static readonly string SoundwaveInteractiveAppDataSettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Soundwave Interactive");
        private static readonly string FirebotAppDataSettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Firebot\\firebot-data\\user-settings");

        private string directoryPath;

        private IEnumerable<InteractiveGameListingModel> interactiveGames;

        private ScorpBotData scorpBotData;

        private SoundwaveSettings soundwaveData;
        private List<SoundwaveProfileItem> soundwaveProfiles;

        public NewUserWizardWindow()
        {
            InitializeComponent();

            this.Initialize(this.StatusBar);
        }

        protected override async Task OnLoaded()
        {
            this.directoryPath = AppDomain.CurrentDomain.BaseDirectory;
            this.XSplitExtensionPathTextBox.Text = Path.Combine(this.directoryPath, "XSplit\\Mix It Up.html");
            this.IntroPageGrid.Visibility = System.Windows.Visibility.Visible;

            this.interactiveGames = await ChannelSession.Connection.GetOwnedInteractiveGames(ChannelSession.Channel);

            await this.GatherSoundwaveSettings();
            if (this.soundwaveData != null)
            {
                this.soundwaveProfiles = this.soundwaveData.Profiles.Keys.Select(p => new SoundwaveProfileItem() { Name = p, AddProfile = false, CanBeImported = !this.interactiveGames.Any(g => g.name.Equals(p)) }).ToList();
                this.SoundwaveInteractiveProfilesDataGrid.ItemsSource = this.soundwaveProfiles;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            MainWindow window = new MainWindow();
            window.Show();

            base.OnClosing(e);
        }

        private void BackButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.ExternalServicesPageGrid.Visibility == System.Windows.Visibility.Visible)
            {
                this.ExternalServicesPageGrid.Visibility = System.Windows.Visibility.Collapsed;
                this.IntroPageGrid.Visibility = System.Windows.Visibility.Visible;
            }
            else if (this.ImportScorpBotSettingsPageGrid.Visibility == System.Windows.Visibility.Visible)
            {
                this.ImportScorpBotSettingsPageGrid.Visibility = System.Windows.Visibility.Collapsed;
                this.ExternalServicesPageGrid.Visibility = System.Windows.Visibility.Visible;
            }
            else if (this.ImportSoundwaveInteractiveSettingsGrid.Visibility == System.Windows.Visibility.Visible)
            {
                this.ImportSoundwaveInteractiveSettingsGrid.Visibility = System.Windows.Visibility.Collapsed;
                this.ImportScorpBotSettingsPageGrid.Visibility = System.Windows.Visibility.Visible;
            }
            else if (this.SetupCompletePageGrid.Visibility == System.Windows.Visibility.Visible)
            {
                this.SetupCompletePageGrid.Visibility = System.Windows.Visibility.Collapsed;
                if (this.soundwaveData != null)
                {
                    this.ImportSoundwaveInteractiveSettingsGrid.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    this.ImportScorpBotSettingsPageGrid.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        private async void NextButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await this.RunAsyncOperation(async () =>
            {
                if (this.IntroPageGrid.Visibility == System.Windows.Visibility.Visible)
                {
                    this.BackButton.IsEnabled = true;
                    this.IntroPageGrid.Visibility = System.Windows.Visibility.Collapsed;
                    this.ExternalServicesPageGrid.Visibility = System.Windows.Visibility.Visible;
                }
                else if (this.ExternalServicesPageGrid.Visibility == System.Windows.Visibility.Visible)
                {
                    this.ExternalServicesPageGrid.Visibility = System.Windows.Visibility.Collapsed;
                    this.ImportScorpBotSettingsPageGrid.Visibility = System.Windows.Visibility.Visible;
                }
                else if (this.ImportScorpBotSettingsPageGrid.Visibility == System.Windows.Visibility.Visible)
                {
                    this.StatusMessageTextBlock.Text = "Gathering ScorpBot Data...";
                    if (!string.IsNullOrEmpty(this.ScorpBotDirectoryTextBox.Text))
                    {
                        this.scorpBotData = await this.GatherScorpBotData(this.ScorpBotDirectoryTextBox.Text);
                        if (this.scorpBotData == null)
                        {
                            await MessageBoxHelper.ShowMessageDialog("Failed to import ScorpBot settings, please ensure that you have selected the correct directory. If this continues to fail, please contact Mix it Up support for assitance.");
                            return;
                        }
                    }

                    this.ImportScorpBotSettingsPageGrid.Visibility = System.Windows.Visibility.Collapsed;
                    if (this.soundwaveData != null)
                    {
                        this.ImportSoundwaveInteractiveSettingsGrid.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        this.SetupCompletePageGrid.Visibility = System.Windows.Visibility.Visible;
                    }
                }
                else if (this.ImportSoundwaveInteractiveSettingsGrid.Visibility == System.Windows.Visibility.Visible)
                {
                    this.ImportSoundwaveInteractiveSettingsGrid.Visibility = System.Windows.Visibility.Collapsed;
                    this.SetupCompletePageGrid.Visibility = System.Windows.Visibility.Visible;
                }
                else if (this.SetupCompletePageGrid.Visibility == System.Windows.Visibility.Visible)
                {
                    await this.RunAsyncOperation(async () =>
                    {
                        this.StatusMessageTextBlock.Text = "Importing data, this may take a few moments...";
                        await this.FinalizeNewUser();
                        this.Close();
                    });
                }
            });
            this.StatusMessageTextBlock.Text = string.Empty;
        }

        private async void BotLogInButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            bool result = await this.RunAsyncOperation(async () =>
            {
                return await ChannelSession.ConnectBot((OAuthShortCodeModel shortCode) =>
                {
                    this.BotLoginShortCodeTextBox.Text = shortCode.code;

                    Process.Start("https://mixer.com/oauth/shortcode?approval_prompt=force&code=" + shortCode.code);
                });
            });

            if (result)
            {
                this.BotLoggedInNameTextBlock.Text = ChannelSession.BotUser.username;
                if (!string.IsNullOrEmpty(ChannelSession.BotUser.avatarUrl))
                {
                    this.BotProfileAvatar.SetImageUrl(ChannelSession.BotUser.avatarUrl);
                }

                this.BotLogInGrid.Visibility = System.Windows.Visibility.Hidden;
                this.BotLoggedInGrid.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void InstallOBSStudioPlugin_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string pluginPath = Path.Combine(this.directoryPath, "OBS\\obs-websocket-4.2.0-Windows-Installer.exe");
            if (File.Exists(pluginPath))
            {
                Process.Start(pluginPath);
            }
        }

        private async void ConnectToOBSStudioButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ChannelSession.Settings.OBSStudioServerIP = ChannelSession.DefaultOBSStudioConnection;
            bool result = await this.RunAsyncOperation(async () =>
            {
                return await ChannelSession.Services.InitializeOBSWebsocket();
            });

            if (result)
            {
                this.OBSStudioConnectedSuccessfulTextBlock.Visibility = System.Windows.Visibility.Visible;
                this.ConnectToOBSStudioButton.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                await MessageBoxHelper.ShowMessageDialog("Could not connect to OBS Studio. Please make sure OBS Studio is running, the obs-websocket plugin is installed, and the connection and password are set to their default settings. If you wish to connect with a specific port and password, you'll need to do this out of the wizard.");
            }
        }

        private async void ConnectToXSplitButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            bool result = await this.RunAsyncOperation(async () =>
            {
                return await ChannelSession.Services.InitializeXSplitServer();
            });

            if (result)
            {
                ChannelSession.Settings.EnableXSplitConnection = true;
                this.XSplitConnectedSuccessfulTextBlock.Visibility = System.Windows.Visibility.Visible;
                this.ConnectToXSplitButton.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                await this.RunAsyncOperation(async () =>
                {
                    await ChannelSession.Services.DisconnectXSplitServer();
                });
                await MessageBoxHelper.ShowMessageDialog("Could not connect to XSplit. Please make sure XSplit is running, the Mix It Up plugin is installed, and is running");
            }
        }

        private void ScorpBotDirectoryBrowseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string folderPath = ChannelSession.Services.FileService.ShowOpenFolderDialog();
            if (!string.IsNullOrEmpty(folderPath))
            {
                this.ScorpBotDirectoryTextBox.Text = folderPath;
            }
        }

        private void SoundwaveProfileCheckBox_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            SoundwaveProfileItem profile = (SoundwaveProfileItem)checkBox.DataContext;
            this.soundwaveProfiles.First(p => p.Name.Equals(profile.Name)).AddProfile = checkBox.IsChecked.GetValueOrDefault();
        }

        private async Task<ScorpBotData> GatherScorpBotData(string folderPath)
        {
            try
            {
                ScorpBotData scorpBotData = new ScorpBotData();

                string dataPath = Path.Combine(folderPath, "Data");
                string settingsFilePath = Path.Combine(dataPath, "settings.ini");
                if (Directory.Exists(dataPath) && File.Exists(settingsFilePath))
                {
                    IEnumerable<string> lines = File.ReadAllLines(settingsFilePath);

                    string currentGroup = null;
                    foreach (var line in lines)
                    {
                        if (line.Contains("="))
                        {
                            string[] splits = line.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                            scorpBotData.Settings[currentGroup].Add(splits[0], splits[1]);
                        }
                        else
                        {
                            currentGroup = line.Replace("[", "").Replace("]", "").ToLower();
                            scorpBotData.Settings.Add(currentGroup, new Dictionary<string, string>());
                        }
                    }

                    string databasePath = Path.Combine(dataPath, "Database");
                    if (Directory.Exists(databasePath))
                    {
                        SQLiteDatabaseWrapper databaseWrapper = new SQLiteDatabaseWrapper(databasePath);

                        databaseWrapper.DatabaseFilePath = Path.Combine(databasePath, "CommandsDB.sqlite");
                        await databaseWrapper.RunReadCommand("SELECT * FROM RegCommand",
                        (reader) =>
                        {
                            if (ScorpBotCommand.IsACommand(reader))
                            {
                                scorpBotData.Commands.Add(new ScorpBotCommand(reader));
                            }
                        });

                        databaseWrapper.DatabaseFilePath = Path.Combine(databasePath, "FilteredWordsDB.sqlite");
                        await databaseWrapper.RunReadCommand("SELECT * FROM Word",
                        (reader) =>
                        {
                            scorpBotData.BannedWords.Add((string)reader["word"]);
                        });

                        databaseWrapper.DatabaseFilePath = Path.Combine(databasePath, "QuotesDB.sqlite");
                        await databaseWrapper.RunReadCommand("SELECT * FROM Quotes",
                        (reader) =>
                        {
                            scorpBotData.Quotes.Add((string)reader["quote_text"]);
                        });

                        databaseWrapper.DatabaseFilePath = Path.Combine(databasePath, "RankDB.sqlite");
                        await databaseWrapper.RunReadCommand("SELECT * FROM Rank",
                        (reader) =>
                        {
                            scorpBotData.Ranks.Add(new ScorpBotRank(reader));
                        });

                        databaseWrapper.DatabaseFilePath = Path.Combine(databasePath, "Viewers3DB.sqlite");
                        await databaseWrapper.RunReadCommand("SELECT * FROM Viewer",
                        (reader) =>
                        {
                            scorpBotData.Viewers.Add(new ScorpBotViewer(reader));
                        });
                    }
                }

                return scorpBotData;
            }
            catch (Exception ex) { Logger.Log(ex); }
            return null;
        }

        private async Task GatherSoundwaveSettings()
        {
            if (Directory.Exists(SoundwaveInteractiveAppDataSettingsFolder))
            {
                string interactiveFilePath = Path.Combine(SoundwaveInteractiveAppDataSettingsFolder, "interactive.json");
                string profilesFilePath = Path.Combine(SoundwaveInteractiveAppDataSettingsFolder, "profiles.json");
                string soundsFilePath = Path.Combine(SoundwaveInteractiveAppDataSettingsFolder, "sounds.json");
                if (File.Exists(interactiveFilePath) && File.Exists(profilesFilePath) && File.Exists(soundsFilePath))
                {
                    JObject interactive = await SerializerHelper.DeserializeFromFile<JObject>(interactiveFilePath);
                    JObject profiles = await SerializerHelper.DeserializeFromFile<JObject>(profilesFilePath);
                    JObject sounds = await SerializerHelper.DeserializeFromFile<JObject>(soundsFilePath);

                    this.soundwaveData = new SoundwaveSettings(interactive, profiles, sounds);
                }
            }
        }

        private async Task FinalizeNewUser()
        {
            if (this.scorpBotData != null)
            {
                ChannelSession.Settings.RankAcquisition.Name = this.scorpBotData.Settings["currency"]["name"];
                ChannelSession.Settings.RankAcquisition.AcquireInterval = int.Parse(this.scorpBotData.Settings["currency"]["onlinepayinterval"]);
                ChannelSession.Settings.RankAcquisition.AcquireAmount = int.Parse(this.scorpBotData.Settings["currency"]["activeuserbonus"]);
                ChannelSession.Settings.RankAcquisition.Enabled = true;

                ChannelSession.Settings.CurrencyAcquisition.Name = this.scorpBotData.Settings["currency2"]["name"];
                ChannelSession.Settings.CurrencyAcquisition.AcquireInterval = int.Parse(this.scorpBotData.Settings["currency2"]["onlinepayinterval"]);
                ChannelSession.Settings.CurrencyAcquisition.AcquireAmount = int.Parse(this.scorpBotData.Settings["currency"]["activeuserbonus"]);
                ChannelSession.Settings.CurrencyAcquisition.Enabled = true;

                foreach (ScorpBotViewer viewer in this.scorpBotData.Viewers)
                {
                    ChannelSession.Settings.UserData[viewer.ID] = new UserDataViewModel(viewer);
                }

                foreach (ScorpBotCommand command in this.scorpBotData.Commands)
                {
                    ChannelSession.Settings.ChatCommands.Add(new ChatCommand(command));
                }

                foreach (string quote in this.scorpBotData.Quotes)
                {
                    ChannelSession.Settings.Quotes.Add(quote);
                }

                if (ChannelSession.Settings.Quotes.Count > 0)
                {
                    ChannelSession.Settings.QuotesEnabled = true;
                }

                foreach (string bannedWord in this.scorpBotData.BannedWords)
                {
                    ChannelSession.Settings.BannedWords.Add(bannedWord);
                }

                foreach (ScorpBotRank rank in this.scorpBotData.Ranks)
                {
                    ChannelSession.Settings.Ranks.Add(new UserRankViewModel(rank.Name, rank.Amount));
                }
            }

            if (this.soundwaveData != null && this.soundwaveProfiles != null && this.soundwaveProfiles.Count(p => p.AddProfile) > 0)
            {
                if (this.soundwaveData.StaticCooldown)
                {
                    ChannelSession.Settings.InteractiveCooldownGroups.Add(SoundwaveInteractiveCooldownGroupName, this.soundwaveData.StaticCooldownAmount / 1000);
                }

                InteractiveGameListingModel soundwaveGame = this.interactiveGames.FirstOrDefault(g => g.name.Equals(SoundwaveInteractiveGameName));
                if (soundwaveGame != null)
                {
                    InteractiveGameVersionModel soundwaveGameVersion = await ChannelSession.Connection.GetInteractiveGameVersion(soundwaveGame.versions.First());
                    InteractiveSceneModel soundwaveGameScene = soundwaveGameVersion.controls.scenes.First();

                    foreach (string profile in this.soundwaveProfiles.Where(p => p.AddProfile).Select(p => p.Name))
                    {
                        // Add code logic to create Interactive Game on Mixer that is a copy of the Soundwave Interactive game, but with buttons filed in with name and not disabled
                        InteractiveSceneModel profileScene = InteractiveGameHelper.CreateDefaultScene();
                        InteractiveGameListingModel profileGame = await ChannelSession.Connection.CreateInteractiveGame(ChannelSession.Channel, ChannelSession.User, profile, profileScene);
                        InteractiveGameVersionModel profileGameVersion = await ChannelSession.Connection.GetInteractiveGameVersion(profileGame.versions.First());
                        profileScene = profileGameVersion.controls.scenes.First();

                        for (int i = 0; i < this.soundwaveData.Profiles[profile].Count(); i++)
                        {
                            SoundwaveButton soundwaveButton = this.soundwaveData.Profiles[profile][i];
                            InteractiveButtonControlModel soundwaveControl = (InteractiveButtonControlModel)soundwaveGameScene.allControls.FirstOrDefault(c => c.controlID.Equals(i.ToString()));

                            InteractiveButtonControlModel button = InteractiveGameHelper.CreateButton(soundwaveButton.name, soundwaveButton.name, soundwaveButton.sparks);
                            button.position = soundwaveControl.position;

                            InteractiveCommand command = new InteractiveCommand(profileGame, profileScene, button, InteractiveButtonCommandTriggerType.MouseDown);
                            command.IndividualCooldown = soundwaveButton.cooldown;
                            if (this.soundwaveData.StaticCooldown)
                            {
                                command.CooldownGroup = SoundwaveInteractiveCooldownGroupName;
                            }

                            SoundAction action = new SoundAction(soundwaveButton.path, soundwaveButton.volume);
                            command.Actions.Add(action);

                            ChannelSession.Settings.InteractiveControls.Add(command);
                            profileScene.buttons.Add(button);
                        }

                        await ChannelSession.Connection.UpdateInteractiveGameVersion(profileGameVersion);
                    }
                }
            }

            await ChannelSession.SaveSettings();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
    }
}