﻿using MixItUp.Base.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Util
{
    public class TimerCommandHelper : IDisposable
    {
        private bool isInitialized = false;

        private Dictionary<string, int> timerCommandIndexes = new Dictionary<string, int>();

        private CancellationTokenSource backgroundThreadCancellationTokenSource = new CancellationTokenSource();

        public void Initialize()
        {
            if (!this.isInitialized)
            {
                this.isInitialized = true;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(async () => { await this.TimerCommandsBackground(); }, this.backgroundThreadCancellationTokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        private async Task TimerCommandsBackground()
        {
            this.timerCommandIndexes = new Dictionary<string, int>();

            int totalTime = 0;

            int nonGroupTotalTime = 0;
            int startTotalMessages = ChannelSession.Chat.Messages.Count;
            timerCommandIndexes[string.Empty] = 0;

            await BackgroundTaskWrapper.RunBackgroundTask(this.backgroundThreadCancellationTokenSource, async (tokenSource) =>
            {
                tokenSource.Token.ThrowIfCancellationRequested();

                await Task.Delay(1000 * 60, tokenSource.Token);

                tokenSource.Token.ThrowIfCancellationRequested();

                if (!ChannelSession.Settings.DisableAllTimers)
                {
                    totalTime++;

                    foreach (var kvp in ChannelSession.Settings.CommandGroups)
                    {
                        if (kvp.Value.TimerInterval > 0 && totalTime % kvp.Value.TimerInterval == 0)
                        {
                            if (!timerCommandIndexes.ContainsKey(kvp.Key))
                            {
                                timerCommandIndexes[kvp.Key] = 0;
                            }

                            IEnumerable<TimerCommand> groupTimers = ChannelSession.Settings.TimerCommands.Where(c => c.IsEnabled && !string.IsNullOrEmpty(c.GroupName) && c.GroupName.Equals(kvp.Key));
                            await this.RunTimerFromGroup(kvp.Key, groupTimers);
                        }
                    }

                    nonGroupTotalTime++;

                    if (nonGroupTotalTime >= ChannelSession.Settings.TimerCommandsInterval)
                    {
                        if ((ChannelSession.Chat.Messages.Count - startTotalMessages) >= ChannelSession.Settings.TimerCommandsMinimumMessages)
                        {
                            IEnumerable<TimerCommand> nonGroupTimers = ChannelSession.Settings.TimerCommands.Where(c => c.IsEnabled && string.IsNullOrEmpty(c.GroupName));
                            await this.RunTimerFromGroup(string.Empty, nonGroupTimers);

                            startTotalMessages = ChannelSession.Chat.Messages.Count;
                            nonGroupTotalTime = 0;
                        }
                    }
                }
            });
        }

        private async Task RunTimerFromGroup(string groupName, IEnumerable<TimerCommand> timers)
        {
            if (timers != null && timers.Count() > 0)
            {
                if (timerCommandIndexes[groupName] >= timers.Count())
                {
                    timerCommandIndexes[groupName] = 0;
                }

                await timers.ElementAt(timerCommandIndexes[groupName]).Perform();

                timerCommandIndexes[groupName]++;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    this.backgroundThreadCancellationTokenSource.Dispose();
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
