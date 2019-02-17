﻿using Mixer.Base.Util;
using MixItUp.Base.Model.Overlay;
using MixItUp.Base.Util;
using MixItUp.WPF.Util;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MixItUp.WPF.Controls.Overlay
{
    /// <summary>
    /// Interaction logic for OverlayGameStatsControl.xaml
    /// </summary>
    public partial class OverlayGameStatsControl : OverlayItemControl
    {
        private OverlayGameStats item;

        public OverlayGameStatsControl()
        {
            InitializeComponent();
        }

        public OverlayGameStatsControl(OverlayGameStats item)
            : this()
        {
            this.item = item;
        }

        public override void SetItem(OverlayItemBase item)
        {
            this.item = (OverlayGameStats)item;

            this.GameComboBox.SelectedItem = this.item.Setup.Name;
            this.CategoryComboBox.SelectedItem = this.item.Setup.Category;

            this.UsernameTextBox.Text = this.item.Setup.Username;
            this.PlatformComboBox.SelectedItem = EnumHelper.GetEnumName(this.item.Setup.Platform);

            this.BorderColorComboBox.Text = this.item.BorderColor;
            if (ColorSchemes.HTMLColorSchemeDictionary.ContainsValue(this.item.BorderColor))
            {
                this.BorderColorComboBox.Text = ColorSchemes.HTMLColorSchemeDictionary.FirstOrDefault(c => c.Value.Equals(this.item.BorderColor)).Key;
            }

            this.BackgroundColorComboBox.Text = this.item.BackgroundColor;
            if (ColorSchemes.HTMLColorSchemeDictionary.ContainsValue(this.item.BackgroundColor))
            {
                this.BackgroundColorComboBox.Text = ColorSchemes.HTMLColorSchemeDictionary.FirstOrDefault(c => c.Value.Equals(this.item.BackgroundColor)).Key;
            }

            this.TextColorComboBox.Text = this.item.TextColor;
            if (ColorSchemes.HTMLColorSchemeDictionary.ContainsValue(this.item.TextColor))
            {
                this.TextColorComboBox.Text = ColorSchemes.HTMLColorSchemeDictionary.FirstOrDefault(c => c.Value.Equals(this.item.TextColor)).Key;
            }

            this.TextFontComboBox.Text = this.item.TextFont;
            this.TextSizeComboBox.Text = this.item.TextSize.ToString();

            this.HTMLText.Text = this.item.HTMLText;
        }

        public override OverlayItemBase GetItem()
        {
            if (this.GameComboBox.SelectedIndex < 0)
            {
                return null;
            }

            if (this.CategoryComboBox.Visibility == Visibility.Visible && this.CategoryComboBox.SelectedIndex < 0)
            {
                return null;
            }

            if (string.IsNullOrEmpty(this.UsernameTextBox.Text))
            {
                return null;
            }

            if (this.PlatformComboBox.SelectedIndex < 0)
            {
                return null;
            }
            GameStatsPlatformTypeEnum platform = EnumHelper.GetEnumValueFromString<GameStatsPlatformTypeEnum>((string)this.PlatformComboBox.SelectedItem);
            
            string borderColor = this.BorderColorComboBox.Text;
            if (ColorSchemes.HTMLColorSchemeDictionary.ContainsKey(borderColor))
            {
                borderColor = ColorSchemes.HTMLColorSchemeDictionary[borderColor];
            }

            string backgroundColor = this.BackgroundColorComboBox.Text;
            if (ColorSchemes.HTMLColorSchemeDictionary.ContainsKey(backgroundColor))
            {
                backgroundColor = ColorSchemes.HTMLColorSchemeDictionary[backgroundColor];
            }

            string textColor = this.TextColorComboBox.Text;
            if (ColorSchemes.HTMLColorSchemeDictionary.ContainsKey(textColor))
            {
                textColor = ColorSchemes.HTMLColorSchemeDictionary[textColor];
            }

            if (string.IsNullOrEmpty(this.TextFontComboBox.Text))
            {
                return null;
            }

            if (string.IsNullOrEmpty(this.TextSizeComboBox.Text) || !int.TryParse(this.TextSizeComboBox.Text, out int textSize))
            {
                return null;
            }

            if (string.IsNullOrEmpty(this.HTMLText.Text))
            {
                return null;
            }

            GameStatsSetupBase setup = null;
            if (this.GameComboBox.SelectedIndex >= 0)
            {
                string gameName = (string)this.GameComboBox.SelectedItem;
                switch (gameName)
                {
                    case RainboxSixSiegeGameStatsSetup.GameName:
                        setup = new RainboxSixSiegeGameStatsSetup(this.UsernameTextBox.Text, platform);
                        break;
                    case FortniteGameStatsSetup.GameName:
                        setup = new FortniteGameStatsSetup(this.UsernameTextBox.Text, platform, (string)this.CategoryComboBox.SelectedItem);
                        break;
                    case CallOfDutyBlackOps4GameStatsSetup.GameName:
                        setup = new CallOfDutyBlackOps4GameStatsSetup(this.UsernameTextBox.Text, platform, (string)this.CategoryComboBox.SelectedItem);
                        break;
                }
            }

            if (setup == null)
            {
                return null;
            }

            return new OverlayGameStats(this.HTMLText.Text, setup, borderColor, backgroundColor, textColor, this.TextFontComboBox.Text, textSize);
        }

        protected override Task OnLoaded()
        {
            List<string> games = new List<string>() { CallOfDutyBlackOps4GameStatsSetup.GameName, FortniteGameStatsSetup.GameName, RainboxSixSiegeGameStatsSetup.GameName };
            this.GameComboBox.ItemsSource = games.OrderBy(g => g);
            this.PlatformComboBox.ItemsSource = EnumHelper.GetEnumNames<GameStatsPlatformTypeEnum>();

            this.TextFontComboBox.ItemsSource = InstalledFonts.GetInstalledFonts();
            this.TextSizeComboBox.ItemsSource = OverlayTextItemControl.sampleFontSize.Select(f => f.ToString());

            this.BorderColorComboBox.ItemsSource = this.BackgroundColorComboBox.ItemsSource = this.TextColorComboBox.ItemsSource = ColorSchemes.HTMLColorSchemeDictionary.Keys;

            this.TextFontComboBox.Text = "Arial";

            this.HTMLText.Text = GameStatsSetupBase.DefaultHTMLTemplate;

            if (this.item != null)
            {
                this.SetItem(this.item);
            }

            return Task.FromResult(0);
        }

        private void GameComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            this.CategoryComboBox.Visibility = Visibility.Collapsed;
            this.CategoryComboBox.ItemsSource = null;
            if (this.GameComboBox.SelectedIndex >= 0)
            {
                string gameName = (string)this.GameComboBox.SelectedItem;
                if (gameName.Equals(FortniteGameStatsSetup.GameName))
                {
                    this.CategoryComboBox.Visibility = Visibility.Visible;
                    this.CategoryComboBox.ItemsSource = FortniteGameStatsSetup.Categories;
                }
                else if (gameName.Equals(CallOfDutyBlackOps4GameStatsSetup.GameName))
                {
                    this.CategoryComboBox.Visibility = Visibility.Visible;
                    this.CategoryComboBox.ItemsSource = CallOfDutyBlackOps4GameStatsSetup.Categories;
                }
            }
        }
    }
}
