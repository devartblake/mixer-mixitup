﻿using Mixer.Base.Clients;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MixItUp.Base.MixerAPI
{
    public class ConstellationClientWrapper : MixerRequestWrapperBase
    {
        public ConstellationClient Client { get; private set; }

        public ConstellationClientWrapper(ConstellationClient client)
        {
            this.Client = client;
        }

        public async Task<bool> Connect() { return await this.RunAsync(this.Client.Connect()); }

        public async Task<bool> SubscribeToEvents(IEnumerable<ConstellationEventType> events) { return await this.RunAsync(this.Client.SubscribeToEvents(events)); }

        public async Task<bool> UnsubscribeToEvents(IEnumerable<ConstellationEventType> events) { return await this.RunAsync(this.Client.UnsubscribeToEvents(events)); }

        public async Task Disconnect() { await this.RunAsync(this.Client.Disconnect()); }
    }
}