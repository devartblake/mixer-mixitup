﻿using Mixer.Base.Model.Interactive;
using MixItUp.Base;
using MixItUp.Base.MixerAPI;
using MixItUp.Base.ViewModel.User;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MixItUp.WPF.Controls.Interactive
{
    public class UserSubmittedImage
    {
        public UserViewModel User { get; set; }
        public string ImageData { get; set; }

        public BitmapImage ImageBitmap { get; private set; }

        public UserSubmittedImage(UserViewModel user, string imageData)
        {
            this.User = user;
            this.ImageData = imageData;

            string bitmapImageData = this.ImageData.Substring(this.ImageData.IndexOf(',') + 1);
            byte[] imageBinaryData = Convert.FromBase64String(bitmapImageData);

            this.ImageBitmap = new BitmapImage();
            this.ImageBitmap.BeginInit();
            this.ImageBitmap.StreamSource = new MemoryStream(imageBinaryData);
            this.ImageBitmap.EndInit();
        }
    }

    /// <summary>
    /// Interaction logic for MixerPaintInteractiveControl.xaml
    /// </summary>
    public partial class MixerPaintInteractiveControl : CustomInteractiveGameControl
    {
        private InteractiveConnectedSceneModel scene;
        private InteractiveConnectedButtonControlModel sendButton;
        private InteractiveConnectedButtonControlModel presentButton;

        private Dictionary<UserViewModel, UserSubmittedImage> userDrawings = new Dictionary<UserViewModel, UserSubmittedImage>();
        private ObservableCollection<UserSubmittedImage> userSubmittedImages = new ObservableCollection<UserSubmittedImage>();

        private UserSubmittedImage selectedUserImage = null;

        public MixerPaintInteractiveControl(InteractiveGameModel game, InteractiveGameVersionModel version)
            : base(game, version)
        {
            InitializeComponent();
        }

        protected override async Task OnLoaded()
        {
            this.SubmittedImagesList.ItemsSource = this.userSubmittedImages;

            await base.OnLoaded();
        }

        protected override async Task<bool> GameConnectedInternal()
        {
            InteractiveConnectedSceneGroupCollectionModel sceneGroups = await ChannelSession.Interactive.GetScenes();
            if (sceneGroups != null)
            {
                this.scene = sceneGroups.scenes.FirstOrDefault();
                if (this.scene != null)
                {
                    this.sendButton = this.scene.buttons.FirstOrDefault(c => c.controlID.Equals("send"));
                    this.presentButton = this.scene.buttons.FirstOrDefault(c => c.controlID.Equals("present"));
                    if (this.sendButton != null && this.presentButton != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected override async Task OnInteractiveControlUsed(UserViewModel user, InteractiveGiveInputModel input, InteractiveConnectedControlCommand command)
        {
            if (user != null && !user.IsAnonymous && input.input.meta.ContainsKey("image"))
            {
                if (this.userDrawings.ContainsKey(user))
                {
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.userSubmittedImages.Remove(this.userDrawings[user]);
                        return Task.FromResult(0);
                    });
                }

                this.userDrawings[user] = new UserSubmittedImage(user, input.input.meta["image"].ToString());
                await this.Dispatcher.InvokeAsync(() =>
                {
                    this.userSubmittedImages.Add(this.userDrawings[user]);
                    return Task.FromResult(0);
                });
            }
        }

        private void SubmittedImagesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                this.ShowButton.IsEnabled = true;
                this.selectedUserImage = (UserSubmittedImage)e.AddedItems[0];
                this.SelectedImageGrid.DataContext = this.selectedUserImage;
                this.SubmittedImagesList.SelectedIndex = -1;
            }
        }

        private async void ShowButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.selectedUserImage != null)
            {
                InteractiveConnectedButtonControlModel control = new InteractiveConnectedButtonControlModel() { controlID = this.presentButton.controlID };
                control.meta["username"] = this.selectedUserImage.User.UserName;
                control.meta["useravatar"] = this.selectedUserImage.User.AvatarLink;
                control.meta["image"] = this.selectedUserImage.ImageData;
                await ChannelSession.Interactive.UpdateControls(this.scene, new List<InteractiveControlModel>() { control });

                this.SelectedImageGrid.DataContext = null;
                this.ShowButton.IsEnabled = false;
            }
        }
    }
}
