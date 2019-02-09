﻿using Mixer.Base.Model.Interactive;
using Mixer.Base.Model.User;
using MixItUp.Base;
using MixItUp.Base.MixerAPI;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using MixItUp.WPF.Controls.Users;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MixItUp.WPF.Controls.Interactive
{
    public enum DropMapTypeEnum
    {
        Fortnite,
        PUBG,
        RealmRoyale,
        BlackOps4,
        ApexLegends
    }

    public abstract class DropMapInterativeGameControl : CustomInteractiveGameControl
    {
        private const string MaxTimeSettingProperty = "MaxTime";
        private const string SparkCostSettingProperty = "SparkCost";

        protected DropMapTypeEnum dropMapType;

        protected int maxTime = 30;
        protected int sparkCost = 0;

        protected InteractiveConnectedSceneModel scene;
        protected InteractiveConnectedButtonControlModel positionButton;
        protected InteractiveConnectedButtonControlModel winnerButton;

        private Dictionary<uint, UserProfileAvatarControl> userAvatars = new Dictionary<uint, UserProfileAvatarControl>();
        private Dictionary<uint, Point> userPoints = new Dictionary<uint, Point>();

        private Canvas canvas;
        private Image image;

        public DropMapInterativeGameControl(InteractiveGameModel game, InteractiveGameVersionModel version) : base(game, version) { }

        public void Initialize(DropMapTypeEnum dropMapType, Image image, Canvas canvas)
        {
            this.dropMapType = dropMapType;
            this.image = image;
            this.canvas = canvas;

            JObject settings = this.GetCustomSettings();
            if (settings.ContainsKey(MaxTimeSettingProperty))
            {
                this.maxTime = settings[MaxTimeSettingProperty].ToObject<int>();
            }
            if (settings.ContainsKey(SparkCostSettingProperty))
            {
                this.sparkCost = settings[SparkCostSettingProperty].ToObject<int>();
            }
        }

        protected abstract void UpdateTimerUI(int timeLeft);

        protected abstract string ComputeLocation(Point point);

        protected abstract Task UpdateWinnerUI(uint winner, string username, string location);

        protected void SaveDropMapSettings()
        {
            JObject settings = this.GetCustomSettings();
            settings[MaxTimeSettingProperty] = this.maxTime;
            settings[SparkCostSettingProperty] = this.sparkCost;
            this.SaveCustomSettings(settings);
        }

        protected override async Task<bool> GameConnectedInternal()
        {
            this.SaveDropMapSettings();

            InteractiveConnectedSceneGroupCollectionModel sceneGroups = await ChannelSession.Interactive.GetScenes();
            if (sceneGroups != null)
            {
                this.scene = sceneGroups.scenes.FirstOrDefault();
                if (this.scene != null)
                {
                    this.positionButton = this.scene.buttons.FirstOrDefault(c => c.controlID.Equals("position"));
                    this.winnerButton = this.scene.buttons.FirstOrDefault(c => c.controlID.Equals("winner"));
                }
            }

            if (this.positionButton == null || this.winnerButton == null)
            {
                Logger.Log("Could no get position or winner buttons");
                return false;
            }

            if (this.sparkCost > 0)
            {
                this.positionButton.cost = this.sparkCost;
                await ChannelSession.Interactive.UpdateControls(this.scene, new List<InteractiveControlModel>() { this.positionButton });

                await ChannelSession.Interactive.RefreshCachedControls();
            }

            this.userAvatars.Clear();
            this.userPoints.Clear();
            this.canvas.Children.Clear();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
            {
                if (this.scene != null && this.winnerButton != null)
                {
                    InteractiveConnectedButtonControlModel control = null;

                    for (int i = 0; i < (maxTime + 1); i++)
                    {
                        await Task.Delay(1000);

                        int timeLeft = maxTime - i;

                        this.Dispatcher.Invoke(() =>
                        {
                            this.UpdateTimerUI(timeLeft);
                        });

                        control = new InteractiveConnectedButtonControlModel() { controlID = this.winnerButton.controlID };
                        control.meta["timeleft"] = timeLeft;
                        await ChannelSession.Interactive.UpdateControls(this.scene, new List<InteractiveControlModel>() { control });
                    }

                    if (this.userPoints.Count > 0)
                    {
                        var users = this.userPoints.Keys.ToList();
                        int index = RandomHelper.GenerateRandomNumber(users.Count);
                        uint winner = users[index];
                        Point point = this.userPoints[winner];
                        UserProfileAvatarControl avatar = this.userAvatars[winner];

                        UserModel user = await ChannelSession.Connection.GetUser(winner);

                        string username = (user != null) ? user.username : "Unknown";
                        string location = this.ComputeLocation(point);

                        this.Dispatcher.InvokeAsync(async () =>
                        {
                            await this.UpdateWinnerUI(winner, username, location);
                            this.canvas.Children.Clear();
                            this.canvas.Children.Add(avatar);
                        });

                        control = new InteractiveConnectedButtonControlModel() { controlID = this.winnerButton.controlID };
                        control.meta["userID"] = winner;
                        control.meta["username"] = username;
                        control.meta["location"] = location;
                        await ChannelSession.Interactive.UpdateControls(this.scene, new List<InteractiveControlModel>() { control });

                        await ChannelSession.Chat.SendMessage(string.Format("Winner: @{0}, Drop Location: {1}", username, location));
                    }
                }
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return false;
        }

        protected void OnUIResize()
        {
            foreach (var userPoint in this.userPoints)
            {
                UserViewModel user = new UserViewModel(userPoint.Key, null);
                this.PositionUserPointOnCanvas(user, userPoint.Value);
            }
        }

        protected override async Task OnInteractiveControlUsed(UserViewModel user, InteractiveGiveInputModel input, InteractiveConnectedControlCommand command)
        {
            try
            {
                if (input.input.controlID.Equals("position") && user != null && input.input.meta.ContainsKey("x") && input.input.meta.ContainsKey("y"))
                {
                    if (this.scene != null && this.positionButton != null && input.input.meta["x"] != null && input.input.meta["y"] != null)
                    {
                        Point point = new Point() { X = (double)input.input.meta["x"], Y = (double)input.input.meta["y"] };

                        this.userPoints[user.ID] = point;

                        InteractiveConnectedButtonControlModel control = new InteractiveConnectedButtonControlModel() { controlID = this.positionButton.controlID };
                        control.meta["userID"] = user.ID;
                        control.meta["x"] = point.X;
                        control.meta["y"] = point.Y;
                        await ChannelSession.Interactive.UpdateControls(this.scene, new List<InteractiveControlModel>() { control });

                        await this.Dispatcher.InvokeAsync(async () =>
                        {
                            
                            if (!this.userAvatars.ContainsKey(user.ID))
                            {
                                UserProfileAvatarControl avatarControl = new UserProfileAvatarControl();
                                await avatarControl.SetUserAvatarUrl(user);
                                avatarControl.SetSize(20);
                                this.userAvatars[user.ID] = avatarControl;

                                this.canvas.Children.Add(avatarControl);
                            }

                            this.PositionUserPointOnCanvas(user, point);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }


        private void PositionUserPointOnCanvas(UserViewModel user, Point point)
        {
            UserProfileAvatarControl avatarControl = this.userAvatars[user.ID];

            double canvasX = ((point.X / 100.0) * this.image.ActualWidth);
            double canvasY = ((point.Y / 100.0) * this.image.ActualHeight);

            Canvas.SetLeft(avatarControl, canvasX - 10);
            Canvas.SetTop(avatarControl, canvasY - 10);
        }
    }
}
