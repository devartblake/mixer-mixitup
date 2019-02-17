﻿using MixItUp.Base.ViewModel.Controls.MainControls;
using MixItUp.Base.ViewModel.Remote;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MixItUp.WPF.Controls.MainControls
{
    /// <summary>
    /// Interaction logic for RemoteControl.xaml
    /// </summary>
    public partial class RemoteControl : MainControlBase
    {
        private RemoteMainControlViewModel viewModel;

        public RemoteControl()
        {
            InitializeComponent();
        }

        protected override async Task InitializeInternal()
        {
            this.DataContext = this.viewModel = new RemoteMainControlViewModel();

            this.viewModel.RefreshProfiles();

            await base.InitializeInternal();
        }

        private void ProfilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                this.viewModel.ProfileSelected((RemoteProfileViewModel)e.AddedItems[0]);
            }
        }
    }
}