﻿using Mixer.Base.Util;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Requirement;
using MixItUp.Base.ViewModel.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Actions
{
    public enum GameQueueActionType
    {
        [Name("User Join Queue")]
        JoinQueue,
        [Name("User's Queue Position")]
        QueuePosition,
        [Name("Queue Status")]
        QueueStatus,
        [Name("User Leave Queue")]
        LeaveQueue,
        [Name("Remove User at Front of Queue")]
        RemoveFirst,
        [Name("Remove Random User in Queue")]
        RemoveRandom,
        [Name("Remove First User of Type in Queue")]
        RemoveFirstType,
        [Name("Enable/Disable Queue")]
        EnableDisableQueue,
        [Name("Clear Queue")]
        ClearQueue,
        [Name("User Join Front of Queue")]
        JoinFrontOfQueue,
    }

    [DataContract]
    public class GameQueueAction : ActionBase
    {
        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return GameQueueAction.asyncSemaphore; } }

        [DataMember]
        public GameQueueActionType GameQueueType { get; set; }

        [DataMember]
        public RoleRequirementViewModel RoleRequirement { get; set; }

        public GameQueueAction() : base(ActionTypeEnum.GameQueue) { }

        public GameQueueAction(GameQueueActionType gameQueueType, RoleRequirementViewModel roleRequirement = null)
            : this()
        {
            this.GameQueueType = gameQueueType;
            this.RoleRequirement = roleRequirement;
        }

        protected override async Task PerformInternal(UserViewModel user, IEnumerable<string> arguments)
        {
            if (ChannelSession.Chat != null)
            {
                if (this.GameQueueType == GameQueueActionType.EnableDisableQueue)
                {
                    if (ChannelSession.Settings.GameQueueRequirements != null)
                    {
                        ChannelSession.GameQueueEnabled = !ChannelSession.GameQueueEnabled;
                    }
                }
                else
                {
                    if (!ChannelSession.GameQueueEnabled)
                    {
                        await ChannelSession.Chat.Whisper(user.UserName, "The game queue is not currently enabled");
                        return;
                    }

                    if (this.GameQueueType == GameQueueActionType.JoinQueue || this.GameQueueType == GameQueueActionType.JoinFrontOfQueue)
                    {
                        int position = ChannelSession.GameQueue.IndexOf(user);
                        if (position == -1)
                        {
                            if (!await ChannelSession.Settings.GameQueueRequirements.DoesMeetUserRoleRequirement(user))
                            {
                                await ChannelSession.Settings.GameQueueRequirements.Role.SendNotMetWhisper(user);
                                return;
                            }

                            if (!ChannelSession.Settings.GameQueueRequirements.DoesMeetRankRequirement(user))
                            {
                                await ChannelSession.Settings.GameQueueRequirements.Rank.SendRankNotMetWhisper(user);
                                return;
                            }

                            if (!ChannelSession.Settings.GameQueueRequirements.DoesMeetInventoryRequirement(user))
                            {
                                await ChannelSession.Settings.GameQueueRequirements.Inventory.SendNotMetWhisper(user);
                                return;
                            }

                            if (!ChannelSession.Settings.GameQueueRequirements.DoesMeetCurrencyRequirement(user) || !ChannelSession.Settings.GameQueueRequirements.TrySubtractCurrencyAmount(user))
                            {
                                await ChannelSession.Settings.GameQueueRequirements.Currency.SendCurrencyNotMetWhisper(user);
                                return;
                            }

                            if (this.GameQueueType == GameQueueActionType.JoinFrontOfQueue)
                            {
                                ChannelSession.GameQueue.Insert(0, user);
                            }
                            else
                            {
                                if (ChannelSession.Settings.GameQueueSubPriority)
                                {
                                    if (user.HasPermissionsTo(MixerRoleEnum.Subscriber))
                                    {
                                        int totalSubs = ChannelSession.GameQueue.Count(u => u.HasPermissionsTo(MixerRoleEnum.Subscriber));
                                        ChannelSession.GameQueue.Insert(totalSubs, user);
                                    }
                                    else
                                    {
                                        ChannelSession.GameQueue.Add(user);
                                    }
                                }
                                else
                                {
                                    ChannelSession.GameQueue.Add(user);
                                }
                            }
                        }
                        await this.PrintUserPosition(user);
                    }
                    else if (this.GameQueueType == GameQueueActionType.QueuePosition)
                    {
                        await this.PrintUserPosition(user);
                    }
                    else if (this.GameQueueType == GameQueueActionType.QueueStatus)
                    {
                        StringBuilder message = new StringBuilder();
                        message.Append(string.Format("There are currently {0} waiting to play with @{1}.", ChannelSession.GameQueue.Count(), ChannelSession.Channel.user.username));

                        if (ChannelSession.GameQueue.Count() > 0)
                        {
                            message.Append(" The following users are next up to play: ");

                            List<string> users = new List<string>();
                            for (int i = 0; i < ChannelSession.GameQueue.Count() && i < 5; i++)
                            {
                                users.Add("@" + ChannelSession.GameQueue[i].UserName);
                            }

                            message.Append(string.Join(", ", users));
                            message.Append(".");
                        }

                        await ChannelSession.Chat.SendMessage(message.ToString());
                    }
                    else if (this.GameQueueType == GameQueueActionType.LeaveQueue)
                    {
                        ChannelSession.GameQueue.Remove(user);
                        await ChannelSession.Chat.Whisper(user.UserName, string.Format("You have left the queue to play with @{0}.", ChannelSession.Channel.user.username));
                    }
                    else if (this.GameQueueType == GameQueueActionType.RemoveFirst || this.GameQueueType == GameQueueActionType.RemoveRandom || this.GameQueueType == GameQueueActionType.RemoveFirstType)
                    {
                        if (ChannelSession.GameQueue.Count() > 0)
                        {
                            UserViewModel queueUser = null;
                            if (this.GameQueueType == GameQueueActionType.RemoveFirst)
                            {
                                queueUser = ChannelSession.GameQueue.ElementAt(0);
                            }
                            else if (this.GameQueueType == GameQueueActionType.RemoveRandom)
                            {
                                int index = RandomHelper.GenerateRandomNumber(ChannelSession.GameQueue.Count());
                                queueUser = ChannelSession.GameQueue.ElementAt(index);
                            }
                            else if (this.GameQueueType == GameQueueActionType.RemoveFirstType)
                            {
                                queueUser = ChannelSession.GameQueue.FirstOrDefault(u => this.RoleRequirement.DoesMeetRequirement(u));
                                if (queueUser != null)
                                {
                                    queueUser = ChannelSession.GameQueue.ElementAt(0);
                                }
                            }

                            if (queueUser != null)
                            {
                                ChannelSession.GameQueue.Remove(queueUser);
                                await ChannelSession.Chat.SendMessage(string.Format("It's time to play @{0}! Listen carefully for instructions on how to join @{1}", queueUser.UserName,
                                    ChannelSession.Channel.user.username));
                            }
                        }
                    }
                    else if (this.GameQueueType == GameQueueActionType.ClearQueue)
                    {
                        ChannelSession.GameQueue.Clear();
                    }
                }

                GlobalEvents.GameQueueUpdated();
            }
        }

        private async Task PrintUserPosition(UserViewModel user)
        {
            int position = ChannelSession.GameQueue.IndexOf(user);
            if (position != -1)
            {
                await ChannelSession.Chat.Whisper(user.UserName, string.Format("You are #{0} in the queue to play with @{1}.", (position + 1), ChannelSession.Channel.user.username));
            }
            else
            {
                await ChannelSession.Chat.Whisper(user.UserName, string.Format("You are not currently in the queue to play with @{0}.", ChannelSession.Channel.user.username));
            }
        }
    }
}
