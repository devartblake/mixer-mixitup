﻿using Mixer.Base.Model.Chat;
using Mixer.Base.Model.User;
using Mixer.Base.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.ViewModel.User
{
    public enum UserRole
    {
        Banned,
        User,
        Pro,
        Follower,
        Subscriber,
        Mod,
        Staff,
        Streamer,

        Custom = 99,
    }

    [DataContract]
    public class UserViewModel : IEquatable<UserViewModel>, IComparable<UserViewModel>
    {
        private const string DefaultAvatarLink = "https://mixer.com/_latest/assets/images/main/avatars/default.jpg";

        public static IEnumerable<UserRole> SelectableUserRoles()
        {
            List<UserRole> roles = new List<UserRole>(EnumHelper.GetEnumList<UserRole>());
            roles.Remove(UserRole.Banned);
            roles.Remove(UserRole.Custom);
            return roles;
        }

        [DataMember]
        public uint ID { get; set; }

        [DataMember]
        public string UserName { get; set; }

        [DataMember]
        public string AvatarLink { get; set; }

        [JsonIgnore]
        public HashSet<UserRole> Roles { get; set; }

        [JsonIgnore]
        public int ChatOffenses { get; set; }

        public UserViewModel()
        {
            this.AvatarLink = DefaultAvatarLink;
        }

        public UserViewModel(UserModel user) : this(user.id, user.username) { }

        public UserViewModel(ChatUserModel user) : this(user.userId.GetValueOrDefault(), user.userName, user.userRoles) { }

        public UserViewModel(ChatUserEventModel userEvent) : this(userEvent.id, userEvent.username, userEvent.roles) { }

        public UserViewModel(ChatMessageEventModel messageEvent) : this(messageEvent.user_id, messageEvent.user_name, messageEvent.user_roles) { }

        public UserViewModel(uint id, string username) : this(id, username, new string[] { }) { }

        public UserViewModel(uint id, string username, string[] userRoles)
            : this()
        {
            this.ID = id;
            this.UserName = username;
            this.Roles = new HashSet<UserRole>();

            this.Roles.Add(UserRole.User);
            if (userRoles.Any(r => r.Equals("Owner"))) { this.Roles.Add(UserRole.Streamer); }
            if (userRoles.Any(r => r.Equals("Staff"))) { this.Roles.Add(UserRole.Staff); }
            if (userRoles.Any(r => r.Equals("Mod"))) { this.Roles.Add(UserRole.Mod); }
            if (userRoles.Any(r => r.Equals("Subscriber"))) { this.Roles.Add(UserRole.Subscriber); }
            if (userRoles.Any(r => r.Equals("Pro"))) { this.Roles.Add(UserRole.Pro); }
            if (userRoles.Any(r => r.Equals("Banned"))) { this.Roles.Add(UserRole.Banned); }
        }

        [JsonIgnore]
        public string RolesString { get { return string.Join(", ", this.Roles.OrderByDescending(r => r).Select(r => EnumHelper.GetEnumName(r))); } }

        [JsonIgnore]
        public UserRole PrimaryRole { get { return this.Roles.Max(); } }

        [JsonIgnore]
        public UserRole PrimarySortableRole { get { return this.Roles.Where(r => r != UserRole.Follower).Max(); } }

        [JsonIgnore]
        public bool IsFollower
        {
            get { return this.Roles.Contains(UserRole.Follower); }
            set { this.Roles.Add(UserRole.Follower); }
        }

        [JsonIgnore]
        public bool IsSubscriber { get { return this.Roles.Contains(UserRole.Subscriber); } }

        public string PrimaryRoleColor
        {
            get
            {
                if (this.Roles.Contains(UserRole.Streamer))
                {
                    return "#FF000000";
                }
                else if (this.Roles.Contains(UserRole.Staff))
                {
                    return "#FFFFD700";
                }
                else if (this.Roles.Contains(UserRole.Mod))
                {
                    return "#FF008000";
                }
                else if (this.Roles.Contains(UserRole.Pro))
                {
                    return "#FF800080";
                }
                else
                {
                    return "#FF0000FF";
                }
            }
        }

        public async Task SetDetails(bool checkForFollow = true)
        {
            if (this.ID > 0)
            {
                UserWithChannelModel userWithChannel = await ChannelSession.Connection.GetUser(this.GetModel());
                if (!string.IsNullOrEmpty(userWithChannel.avatarUrl))
                {
                    this.AvatarLink = userWithChannel.avatarUrl;
                }
            }

            if (checkForFollow)
            {
                DateTimeOffset? followDate = await ChannelSession.Connection.CheckIfFollows(ChannelSession.Channel, this.GetModel());
                if (followDate != null)
                {
                    this.IsFollower = true;
                }
            }
        }

        public UserModel GetModel()
        {
            return new UserModel()
            {
                id = this.ID,
                username = this.UserName,
            };
        }

        public ChatUserModel GetChatModel()
        {
            return new ChatUserModel()
            {
                userId = this.ID,
                userName = this.UserName,
                userRoles = this.Roles.Select(r => r.ToString()).ToArray(),
            };
        }

        public override bool Equals(object obj)
        {
            if (obj is UserViewModel)
            {
                return this.Equals((UserViewModel)obj);
            }
            return false;
        }

        public bool Equals(UserViewModel other) { return this.ID.Equals(other.ID); }

        public int CompareTo(UserViewModel other)
        {
            int order = this.PrimaryRole.CompareTo(other.PrimaryRole);
            if (order == 0)
            {
                return this.UserName.CompareTo(other.UserName);
            }
            return order;
        }

        public override int GetHashCode() { return this.ID.GetHashCode(); }

        public override string ToString() { return this.UserName; }
    }
}
