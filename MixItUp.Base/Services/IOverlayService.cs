﻿using MixItUp.Base.Model.Overlay;
using System;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    [DataContract]
    public class OverlayTextToSpeech
    {
        [DataMember]
        public string Text { get; set; }
        [DataMember]
        public string Voice { get; set; }
        [DataMember]
        public double Volume { get; set; }
        [DataMember]
        public double Pitch { get; set; }
        [DataMember]
        public double Rate { get; set; }
    }

    [DataContract]
    public class OverlaySongRequest
    {
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public string Action { get; set; }
        [DataMember]
        public string Source { get; set; }
        [DataMember]
        public int Volume { get; set; }
    }

    public interface IOverlayService
    {
        event EventHandler OnWebSocketConnectedOccurred;
        event EventHandler<WebSocketCloseStatus> OnWebSocketDisconnectedOccurred;

        Task<bool> Initialize();

        Task<int> TestConnection();

        void StartBatching();

        Task EndBatching();

        Task SendItem(OverlayItemBase item, OverlayItemPosition position, OverlayItemEffects effects);

        Task SendTextToSpeech(OverlayTextToSpeech textToSpeech);

        Task SendSongRequest(OverlaySongRequest songRequest);

        Task RemoveItem(OverlayItemBase item);

        Task Disconnect();
    }
}
