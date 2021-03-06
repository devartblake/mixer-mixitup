﻿using Mixer.Base;
using Mixer.Base.Model.OAuth;
using Mixer.Base.Model.User;
using Mixer.Base.Util;
using MixItUp.Base;
using MixItUp.Base.Commands;
using MixItUp.Base.Model.User;
using MixItUp.Base.Services;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Desktop.Services
{
    public class StreamJarService : OAuthServiceBase, IStreamJarService, IDisposable
    {
        private const string BaseAddress = "https://jar.streamjar.tv/v2/";

        private const string ClientID = "0ff4b414d6ec2296b824cd8a11ff75ff";
        private const string AuthorizationUrl = "https://control.streamjar.tv/oauth/authorize?response_type=code&client_id={0}&redirect_uri={1}&scopes=channel:tips:view";
        private const string TokenUrl = "https://jar.streamjar.tv/v2/oauth/authorize";

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private StreamJarChannel channel;

        public StreamJarService() : base(StreamJarService.BaseAddress) { }

        public StreamJarService(OAuthTokenModel token) : base(StreamJarService.BaseAddress, token) { }

        public async Task<bool> Connect()
        {
            if (this.token != null)
            {
                try
                {
                    await this.RefreshOAuthToken();

                    if (await this.InitializeInternal())
                    {
                        return true;
                    }
                }
                catch (Exception ex) { MixItUp.Base.Util.Logger.Log(ex); }
            }

            string authorizationCode = await this.ConnectViaOAuthRedirect(string.Format(StreamJarService.AuthorizationUrl, StreamJarService.ClientID, MixerConnection.DEFAULT_OAUTH_LOCALHOST_URL));
            if (!string.IsNullOrEmpty(authorizationCode))
            {
                JObject payload = new JObject();
                payload["grant_type"] = "authorization_code";
                payload["client_id"] = StreamJarService.ClientID;
                payload["code"] = authorizationCode;
                payload["redirect_uri"] = MixerConnection.DEFAULT_OAUTH_LOCALHOST_URL;

                this.token = await this.PostAsync<OAuthTokenModel>(StreamJarService.TokenUrl, this.CreateContentFromObject(payload), autoRefreshToken: false);
                if (this.token != null)
                {
                    token.authorizationCode = authorizationCode;

                    return await this.InitializeInternal();
                }
            }

            return false;
        }

        public Task Disconnect()
        {
            this.cancellationTokenSource.Cancel();
            this.token = null;
            return Task.FromResult(0);
        }

        public async Task<StreamJarChannel> GetChannel()
        {
            try
            {
                JArray jarray = await this.GetAsync<JArray>("channels");
                if (jarray != null && jarray.Count > 0)
                {
                    return jarray.First.ToObject<StreamJarChannel>();
                }
            }
            catch (Exception ex) { MixItUp.Base.Util.Logger.Log(ex); }
            return null;
        }

        public async Task<IEnumerable<StreamJarDonation>> GetDonations()
        {
            try
            {
                return await this.GetAsync<IEnumerable<StreamJarDonation>>(string.Format("channels/{0}/tips", this.channel.ID.ToString()));
            }
            catch (Exception ex) { MixItUp.Base.Util.Logger.Log(ex); }
            return new List<StreamJarDonation>();
        }

        protected override async Task RefreshOAuthToken()
        {
            if (this.token != null)
            {
                JObject payload = new JObject();
                payload["grant_type"] = "refresh_token";
                payload["client_id"] = StreamJarService.ClientID;
                payload["refresh_token"] = this.token.refreshToken;
                payload["redirect_uri"] = MixerConnection.DEFAULT_OAUTH_LOCALHOST_URL;

                this.token = await this.PostAsync<OAuthTokenModel>(StreamJarService.TokenUrl, this.CreateContentFromObject(payload), autoRefreshToken: false);
            }
        }

        private async Task<bool> InitializeInternal()
        {
            this.cancellationTokenSource = new CancellationTokenSource();

            this.channel = await this.GetChannel();
            if (this.channel != null)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(this.BackgroundDonationCheck, this.cancellationTokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                return true;
            }
            return false;
        }

        private async Task BackgroundDonationCheck()
        {
            Dictionary<int, StreamJarDonation> donationsReceived = new Dictionary<int, StreamJarDonation>();

            foreach (StreamJarDonation donation in await this.GetDonations())
            {
                donationsReceived[donation.ID] = donation;
            }

            while (!this.cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    foreach (StreamJarDonation sjDonation in await this.GetDonations())
                    {
                        if (!donationsReceived.ContainsKey(sjDonation.ID))
                        {
                            donationsReceived[sjDonation.ID] = sjDonation;
                            UserDonationModel donation = sjDonation.ToGenericDonation();
                            await EventCommand.ProcessDonationEventCommand(donation, OtherEventTypeEnum.StreamJarDonation);
                        }
                    }
                }
                catch (Exception ex) { MixItUp.Base.Util.Logger.Log(ex); }

                await Task.Delay(10000);
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
                    this.cancellationTokenSource.Dispose();
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
