﻿using Mixer.Base.Util;
using MixItUp.Base.Actions;
using MixItUp.Base.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MixItUp.WPF.Controls.Actions
{
    /// <summary>
    /// Interaction logic for SongRequestActionControl.xaml
    /// </summary>
    public partial class SongRequestActionControl : ActionControlBase
    {
        private SongRequestAction action;

        public SongRequestActionControl(ActionContainerControl containerControl) : base(containerControl) { InitializeComponent(); }

        public SongRequestActionControl(ActionContainerControl containerControl, SongRequestAction action) : this(containerControl) { this.action = action; }

        public override Task OnLoaded()
        {
            this.SongRequestActionTypeComboBox.ItemsSource = EnumHelper.GetEnumNames<SongRequestActionTypeEnum>().OrderBy(s => s);
            this.SongRequestServiceComboBox.ItemsSource = EnumHelper.GetEnumNames<SongRequestServiceTypeEnum>().OrderBy(s => s);

            this.SongRequestServiceComboBox.SelectedItem = EnumHelper.GetEnumName(SongRequestServiceTypeEnum.All);

            if (this.action != null)
            {
                this.SongRequestActionTypeComboBox.SelectedItem = EnumHelper.GetEnumName(action.SongRequestType);
                this.SongRequestServiceComboBox.SelectedItem = EnumHelper.GetEnumName(action.SpecificService);
            }
            return Task.FromResult(0);
        }

        public override ActionBase GetAction()
        {
            if (this.SongRequestActionTypeComboBox.SelectedIndex >= 0)
            {
                SongRequestActionTypeEnum actionType = EnumHelper.GetEnumValueFromString<SongRequestActionTypeEnum>((string)this.SongRequestActionTypeComboBox.SelectedItem);
                SongRequestServiceTypeEnum serviceType = SongRequestServiceTypeEnum.All;
                if (actionType == SongRequestActionTypeEnum.SearchSongsAndPickFirstResult || actionType == SongRequestActionTypeEnum.SearchSongsAndUseArtistSelect)
                {
                    serviceType = EnumHelper.GetEnumValueFromString<SongRequestServiceTypeEnum>((string)this.SongRequestServiceComboBox.SelectedItem);
                }
                return new SongRequestAction(actionType, serviceType);
            }
            return null;
        }

        private void SongRequestActionTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (this.SongRequestActionTypeComboBox.SelectedIndex >= 0)
            {
                SongRequestActionTypeEnum actionType = EnumHelper.GetEnumValueFromString<SongRequestActionTypeEnum>((string)this.SongRequestActionTypeComboBox.SelectedItem);
                this.SongRequestServiceComboBox.Visibility = (actionType == SongRequestActionTypeEnum.SearchSongsAndPickFirstResult || actionType == SongRequestActionTypeEnum.SearchSongsAndUseArtistSelect)
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
