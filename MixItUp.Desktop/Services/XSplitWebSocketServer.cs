﻿using Mixer.Base.Model.Client;
using MixItUp.Base.Actions;
using MixItUp.Base.Services;
using MixItUp.Base.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.XSplit
{
    #region Data Classes

    [DataContract]
    public class XSplitScene
    {
        [DataMember]
        public string sceneName;
    }

    [DataContract]
    public class XSplitSource
    {
        [DataMember]
        public string sceneName;
        [DataMember]
        public string sourceName;
        [DataMember]
        public bool sourceVisible;
    }

    [DataContract]
    public class XSplitWebBrowserSource : XSplitSource
    {
        [DataMember]
        public string webBrowserUrl;
    }

    public class XSplitPacket : WebSocketPacket
    {
        public JObject data;

        public XSplitPacket(string type, JObject data)
        {
            this.type = type;
            this.data = data;
        }
    }

    #endregion Data Classes

    public class XSplitWebSocketHttpListenerServer : WebSocketHttpListenerServerBase, IStreamingSoftwareService
    {
        public event EventHandler Connected { add { base.OnConnectedOccurred += value; } remove { base.OnConnectedOccurred -= value; } }
        public event EventHandler Disconnected;

        public XSplitWebSocketHttpListenerServer(string address) : base(address) { }

        public Task<bool> Connect()
        {
            this.Start();
            return Task.FromResult(true);
        }

        public async Task Disconnect() { await this.End(); }

        public async Task ShowScene(string sceneName)
        {
            await this.Send(new XSplitPacket("sceneTransition", JObject.FromObject(new XSplitScene() { sceneName = sceneName })));
        }

        public async Task SetSourceVisibility(string sceneName, string sourceName, bool visibility)
        {
            await this.Send(new XSplitPacket("sourceUpdate", JObject.FromObject(new XSplitSource() { sceneName = sceneName, sourceName = sourceName, sourceVisible = visibility })));
        }

        public async Task SetWebBrowserSourceURL(string sceneName, string sourceName, string url)
        {
            await this.Send(new XSplitPacket("sourceUpdate", JObject.FromObject(new XSplitWebBrowserSource() { sceneName = sceneName, sourceName = sourceName, webBrowserUrl = url })));
        }

        public Task SetSourceDimensions(string sceneName, string sourceName, StreamingSourceDimensions dimensions) { return Task.FromResult(0); }

        public Task<StreamingSourceDimensions> GetSourceDimensions(string sceneName, string sourceName) { return Task.FromResult(new StreamingSourceDimensions()); }

        public Task StartStopStream() { return Task.FromResult(0); }

        public Task SaveReplayBuffer() { return Task.FromResult(0); }
        public Task<bool> StartReplayBuffer() { return Task.FromResult(false); }

        protected override WebSocketServerBase CreateWebSocketServer(HttpListenerContext listenerContext)
        {
            return new XSplitWebSocketServer(listenerContext);
        }

        private void XSplitWebServer_OnDisconnectOccurred(object sender, WebSocketCloseStatus e)
        {
            this.Disconnected(sender, new EventArgs());
        }
    }

    public class XSplitWebSocketServer : WebSocketServerBase, IStreamingSoftwareService
    {
        public XSplitWebSocketServer(HttpListenerContext listenerContext) : base(listenerContext) { this.OnDisconnectOccurred += XSplitWebServer_OnDisconnectOccurred; }

        public event EventHandler Connected { add { this.OnConnectedOccurred += value; } remove { this.OnConnectedOccurred -= value; } }
        public event EventHandler Disconnected = delegate { };

        public async Task<bool> Connect() { return await this.Initialize(); }

        public async Task Disconnect() { await base.Disconnect(); }

        public async Task ShowScene(string sceneName)
        {
            await this.Send(new XSplitPacket("sceneTransition", JObject.FromObject(new XSplitScene() { sceneName = sceneName })));
        }

        public async Task SetSourceVisibility(string sceneName, string sourceName, bool visibility)
        {
            await this.Send(new XSplitPacket("sourceUpdate", JObject.FromObject(new XSplitSource() { sceneName = sceneName, sourceName = sourceName, sourceVisible = visibility })));
        }

        public async Task SetWebBrowserSourceURL(string sceneName, string sourceName, string url)
        {
            await this.Send(new XSplitPacket("sourceUpdate", JObject.FromObject(new XSplitWebBrowserSource() { sceneName = sceneName, sourceName = sourceName, webBrowserUrl = url })));
        }

        public Task SetSourceDimensions(string sceneName, string sourceName, StreamingSourceDimensions dimensions) { return Task.FromResult(0); }

        public Task<StreamingSourceDimensions> GetSourceDimensions(string sceneName, string sourceName) { return Task.FromResult(new StreamingSourceDimensions()); }

        public Task StartStopStream() { return Task.FromResult(0); }

        public Task SaveReplayBuffer() { return Task.FromResult(0); }
        public Task<bool> StartReplayBuffer() { return Task.FromResult(false); }

        protected override async Task ProcessReceivedPacket(string packetJSON)
        {
            await base.ProcessReceivedPacket(packetJSON);
        }

        private void XSplitWebServer_OnDisconnectOccurred(object sender, WebSocketCloseStatus e)
        {
            this.Disconnected(sender, new EventArgs());
        }
    }
}
