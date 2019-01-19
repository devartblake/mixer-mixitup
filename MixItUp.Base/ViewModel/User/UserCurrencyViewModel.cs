﻿using MixItUp.Base.Commands;
using MixItUp.Base.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.ViewModel.User
{
    public enum CurrencyResetRateEnum
    {
        Never,
        Yearly,
        Monthly,
        Weekly,
        Daily,
    }

    [DataContract]
    public class UserCurrencyViewModel : IEquatable<UserCurrencyViewModel>
    {
        public static UserRankViewModel NoRank = new UserRankViewModel("No Rank", 0);

        [DataMember]
        public Guid ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool IsTrackingSparks { get; set; }
        [DataMember]
        public bool IsTrackingEmbers { get; set; }

        [DataMember]
        public int AcquireAmount { get; set; }
        [DataMember]
        public int AcquireInterval { get; set; }
        [DataMember]
        public int MinimumActiveRate { get; set; }

        [DataMember]
        public int OfflineAcquireAmount { get; set; }
        [DataMember]
        public int OfflineAcquireInterval { get; set; }

        [DataMember]
        public int MaxAmount { get; set; }

        [DataMember]
        public string SpecialIdentifier { get; set; }

        [DataMember]
        public int SubscriberBonus { get; set; }
        [DataMember]
        public int ModeratorBonus { get; set; }

        [DataMember]
        public int OnFollowBonus { get; set; }
        [DataMember]
        public int OnHostBonus { get; set; }
        [DataMember]
        public int OnSubscribeBonus { get; set; }

        [DataMember]
        public CurrencyResetRateEnum ResetInterval { get; set; }
        [DataMember]
        public DateTimeOffset LastReset { get; set; }

        [DataMember]
        public List<UserRankViewModel> Ranks { get; set; }
        [DataMember]
        public CustomCommand RankChangedCommand { get; set; }

        [JsonIgnore]
        public bool IsRank { get { return this.Ranks.Count > 0; } }

        public UserCurrencyViewModel()
        {
            this.ID = Guid.NewGuid();
            this.MinimumActiveRate = 0;
            this.MaxAmount = int.MaxValue;
            this.SpecialIdentifier = string.Empty;
            this.ResetInterval = CurrencyResetRateEnum.Never;
            this.LastReset = DateTimeOffset.MinValue;
            this.Ranks = new List<UserRankViewModel>();
        }

        [JsonIgnore]
        public bool IsActive { get { return this.AcquireInterval != 0; } }

        [JsonIgnore]
        public bool IsOnlineIntervalMinutes { get { return this.AcquireAmount == 1 && this.AcquireInterval == 1; } }

        [JsonIgnore]
        public bool IsOnlineIntervalHours { get { return this.AcquireAmount == 1 && this.AcquireInterval == 60; } }

        [JsonIgnore]
        public bool IsOnlineIntervalDisabled { get { return this.AcquireAmount == 0 && this.AcquireInterval == 0; } }

        [JsonIgnore]
        public bool IsOfflineIntervalMinutes { get { return this.OfflineAcquireAmount == 1 && this.OfflineAcquireInterval == 1; } }

        [JsonIgnore]
        public bool IsOfflineIntervalHours { get { return this.OfflineAcquireAmount == 1 && this.OfflineAcquireInterval == 60; } }

        [JsonIgnore]
        public bool IsOfflineIntervalDisabled { get { return this.OfflineAcquireAmount == 0 && this.OfflineAcquireInterval == 0; } }

        [JsonIgnore]
        public bool HasMinimumActiveRate { get { return this.MinimumActiveRate > 0; } }

        [JsonIgnore]
        public string UserAmountSpecialIdentifier { get { return string.Format("{0}{1}", SpecialIdentifierStringBuilder.UserSpecialIdentifierHeader, this.SpecialIdentifier); } }

        [JsonIgnore]
        public string UserRankNameSpecialIdentifier { get { return string.Format("{0}rank", this.UserAmountSpecialIdentifier); } }

        [JsonIgnore]
        public string UserAmountNextSpecialIdentifier { get { return string.Format("{0}next", this.UserAmountSpecialIdentifier); } }

        [JsonIgnore]
        public string UserRankNextNameSpecialIdentifier { get { return string.Format("{0}nextrank", this.UserAmountSpecialIdentifier); } }

        [JsonIgnore]
        public string TopRegexSpecialIdentifier { get { return string.Format("{0}\\d+{1}", SpecialIdentifierStringBuilder.TopSpecialIdentifierHeader, this.SpecialIdentifier); } }

        [JsonIgnore]
        public string Top10SpecialIdentifier { get { return string.Format("{0}10{1}", SpecialIdentifierStringBuilder.TopSpecialIdentifierHeader, this.SpecialIdentifier); } }

        public UserRankViewModel GetRankForPoints(int points)
        {
            UserRankViewModel rank = UserCurrencyViewModel.NoRank;
            if (this.Ranks.Count > 0)
            {
                rank = this.Ranks.Where(r => r.MinimumPoints <= points).OrderByDescending(r => r.MinimumPoints).FirstOrDefault();
                if (rank == null)
                {
                    rank = UserCurrencyViewModel.NoRank;
                }
            }
            return rank;
        }

        public UserRankViewModel GetNextRankForPoints(int points)
        {
            UserRankViewModel rank = UserCurrencyViewModel.NoRank;
            if (this.Ranks.Count > 0)
            {
                rank = this.Ranks.Where(r => r.MinimumPoints > points).OrderBy(r => r.MinimumPoints).FirstOrDefault();
                if (rank == null)
                {
                    rank = UserCurrencyViewModel.NoRank;
                }
            }
            return rank;
        }

        public async Task UpdateUserData()
        {
            if (this.IsActive && !this.IsTrackingSparks && !this.IsTrackingEmbers)
            {
                bool bonusesCanBeApplied = (ChannelSession.Channel.online || this.OfflineAcquireAmount > 0);
                DateTimeOffset minActiveTime = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(this.MinimumActiveRate));
                int interval = ChannelSession.Channel.online ? this.AcquireInterval : this.OfflineAcquireInterval;
                if (interval > 0)
                {
                    foreach (UserViewModel user in await ChannelSession.ActiveUsers.GetAllWorkableUsers())
                    {
                        if (!user.Data.IsCurrencyRankExempt && (!this.HasMinimumActiveRate || user.LastActivity > minActiveTime))
                        {
                            int minutes = ChannelSession.Channel.online ? user.Data.ViewingMinutes : user.Data.OfflineViewingMinutes;
                            if (minutes % interval == 0)
                            {
                                user.Data.AddCurrencyAmount(this, ChannelSession.Channel.online ? this.AcquireAmount : this.OfflineAcquireAmount);
                                if (bonusesCanBeApplied)
                                {
                                    if (user.HasPermissionsTo(MixerRoleEnum.Mod))
                                    {
                                        user.Data.AddCurrencyAmount(this, this.ModeratorBonus);
                                    }
                                    else if (user.HasPermissionsTo(MixerRoleEnum.Subscriber))
                                    {
                                        user.Data.AddCurrencyAmount(this, this.SubscriberBonus);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool ShouldBeReset()
        {
            if (this.ResetInterval != CurrencyResetRateEnum.Never)
            {
                DateTimeOffset newResetDate = DateTimeOffset.MinValue;

                if (this.ResetInterval == CurrencyResetRateEnum.Daily) { newResetDate = this.LastReset.AddDays(1); }
                else if (this.ResetInterval == CurrencyResetRateEnum.Weekly) { newResetDate = this.LastReset.AddDays(7); }
                else if (this.ResetInterval == CurrencyResetRateEnum.Monthly) { newResetDate = this.LastReset.AddMonths(1); }
                else if (this.ResetInterval == CurrencyResetRateEnum.Yearly) { newResetDate = this.LastReset.AddYears(1); }

                return (newResetDate < DateTimeOffset.Now);
            }
            return false;
        }

        public async Task Reset()
        {
            foreach (UserDataViewModel userData in ChannelSession.Settings.UserData.Values)
            {
                userData.ResetCurrencyAmount(this);
                ChannelSession.Settings.UserData.ManualValueChanged(userData.ID);
            }
            this.LastReset = new DateTimeOffset(DateTimeOffset.Now.Date);

            await ChannelSession.SaveSettings();
        }

        public override bool Equals(object obj)
        {
            if (obj is UserCurrencyViewModel)
            {
                return this.Equals((UserCurrencyViewModel)obj);
            }
            return false;
        }

        public bool Equals(UserCurrencyViewModel other)
        {
            return this.ID.Equals(other.ID);
        }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }
    }
}
