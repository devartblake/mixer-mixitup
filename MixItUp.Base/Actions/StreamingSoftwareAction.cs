﻿using Mixer.Base.Util;
using MixItUp.Base.Services;
using MixItUp.Base.ViewModel.User;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Actions
{
    public enum StreamingSoftwareTypeEnum
    {
        [Name("Default Setting")]
        DefaultSetting,

        [Name("OBS Studio")]
        OBSStudio,
        [Name("XSplit")]
        XSplit,
        [Name("Streamlabs OBS")]
        StreamlabsOBS,
    }

    public enum StreamingActionTypeEnum
    {
        Scene,

        [Name("Source Visibility")]
        SourceVisibility,
        [Name("Text Source")]
        TextSource,
        [Name("Web Browser Source")]
        WebBrowserSource,
        [Name("Source Dimensions")]
        SourceDimensions,

        [Name("Start/Stop Stream")]
        StartStopStream,

        [Name("Save Replay Buffer")]
        SaveReplayBuffer,
    }

    public class StreamingSourceDimensions
    {
        public int X;
        public int Y;
        public int Rotation;
        public float XScale;
        public float YScale;
    }

    [DataContract]
    public class StreamingSoftwareAction : ActionBase
    {
        public const string SourceTextFilesDirectoryName = "SourceTextFiles";

        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return StreamingSoftwareAction.asyncSemaphore; } }

        public static StreamingSoftwareAction CreateSceneAction(StreamingSoftwareTypeEnum softwareType, string sceneName)
        {
            StreamingSoftwareAction action = new StreamingSoftwareAction(softwareType, StreamingActionTypeEnum.Scene);
            action.SceneName = sceneName;
            return action;
        }

        public static StreamingSoftwareAction CreateSourceVisibilityAction(StreamingSoftwareTypeEnum softwareType, string sceneName, string sourceName, bool sourceVisible)
        {
            StreamingSoftwareAction action = new StreamingSoftwareAction(softwareType, StreamingActionTypeEnum.SourceVisibility);
            action.SceneName = sceneName;
            action.SourceName = sourceName;
            action.SourceVisible = sourceVisible;
            return action;
        }

        public static StreamingSoftwareAction CreateTextSourceAction(StreamingSoftwareTypeEnum softwareType, string sceneName, string sourceName, bool sourceVisible, string sourceText, string sourceTextFilePath)
        {
            StreamingSoftwareAction action = StreamingSoftwareAction.CreateSourceVisibilityAction(softwareType, sceneName, sourceName, sourceVisible);
            action.ActionType = StreamingActionTypeEnum.TextSource;
            action.SourceText = sourceText;
            action.SourceTextFilePath = sourceTextFilePath;
            return action;
        }

        public static StreamingSoftwareAction CreateWebBrowserSourceAction(StreamingSoftwareTypeEnum softwareType, string sceneName, string sourceName, bool sourceVisible, string sourceURL)
        {
            StreamingSoftwareAction action = StreamingSoftwareAction.CreateSourceVisibilityAction(softwareType, sceneName, sourceName, sourceVisible);
            action.ActionType = StreamingActionTypeEnum.WebBrowserSource;
            action.SourceURL = sourceURL;
            if (softwareType == StreamingSoftwareTypeEnum.XSplit)
            {
                if (!File.Exists(action.SourceURL) && !action.SourceURL.Contains("://"))
                {
                    action.SourceURL = "http://" + action.SourceURL;
                }
            }
            return action;
        }

        public static StreamingSoftwareAction CreateSourceDimensionsAction(StreamingSoftwareTypeEnum softwareType, string sceneName, string sourceName, bool sourceVisible, StreamingSourceDimensions sourceDimensions)
        {
            StreamingSoftwareAction action = StreamingSoftwareAction.CreateSourceVisibilityAction(softwareType, sceneName, sourceName, sourceVisible);
            action.ActionType = StreamingActionTypeEnum.SourceDimensions;
            action.SourceDimensions = sourceDimensions;
            return action;
        }

        public static StreamingSoftwareAction CreateStartStopStreamAction(StreamingSoftwareTypeEnum softwareType)
        {
            return new StreamingSoftwareAction(softwareType, StreamingActionTypeEnum.StartStopStream);
        }

        public static StreamingSoftwareAction CreateSaveReplayBufferAction(StreamingSoftwareTypeEnum softwareType)
        {
            return new StreamingSoftwareAction(softwareType, StreamingActionTypeEnum.SaveReplayBuffer);
        }

        [DataMember]
        public StreamingSoftwareTypeEnum SoftwareType { get; set; }
        [DataMember]
        public StreamingActionTypeEnum ActionType { get; set; }

        [DataMember]
        public string SceneName { get; set; }

        [DataMember]
        public string SourceName { get; set; }
        [DataMember]
        public bool SourceVisible { get; set; }

        [DataMember]
        public string SourceText { get; set; }
        [DataMember]
        public string SourceTextFilePath { get; set; }

        [DataMember]
        public string SourceURL { get; set; }

        [DataMember]
        public StreamingSourceDimensions SourceDimensions { get; set; }

        public StreamingSoftwareAction() : base(ActionTypeEnum.StreamingSoftware) { }

        public StreamingSoftwareAction(StreamingSoftwareTypeEnum softwareType, StreamingActionTypeEnum actionType)
            : this()
        {
            this.SoftwareType = softwareType;
            this.ActionType = actionType;
        }

        public StreamingSoftwareTypeEnum SelectedStreamingSoftware { get { return (this.SoftwareType == StreamingSoftwareTypeEnum.DefaultSetting) ? ChannelSession.Settings.DefaultStreamingSoftware : this.SoftwareType; } }

        public void UpdateReferenceTextFile(string textToWrite)
        {
            if (!string.IsNullOrEmpty(this.SourceText) && !string.IsNullOrEmpty(this.SourceTextFilePath))
            {
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(this.SourceTextFilePath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(this.SourceTextFilePath));
                    }

                    using (StreamWriter writer = new StreamWriter(File.Open(this.SourceTextFilePath, FileMode.Create)))
                    {
                        writer.Write(textToWrite);
                        writer.Flush();
                    }
                }
                catch (Exception ex) { Logger.Log(ex); }
            }
        }

        protected override async Task PerformInternal(UserViewModel user, IEnumerable<string> arguments)
        {
            if (this.ActionType == StreamingActionTypeEnum.TextSource && !string.IsNullOrEmpty(this.SourceText))
            {
                this.UpdateReferenceTextFile(await this.ReplaceStringWithSpecialModifiers(this.SourceText, user, arguments));
            }

            string url = string.Empty;
            if (this.ActionType == StreamingActionTypeEnum.WebBrowserSource && !string.IsNullOrEmpty(this.SourceURL))
            {
                url = await this.ReplaceStringWithSpecialModifiers(this.SourceURL, user, arguments);
            }

            IStreamingSoftwareService ssService = null;
            if (this.SelectedStreamingSoftware == StreamingSoftwareTypeEnum.OBSStudio)
            {
                if (ChannelSession.Services.OBSWebsocket == null)
                {
                    await ChannelSession.Services.InitializeOBSWebsocket();
                }
                ssService = ChannelSession.Services.OBSWebsocket;
            }
            else if (this.SelectedStreamingSoftware == StreamingSoftwareTypeEnum.XSplit)
            {
                if (ChannelSession.Services.XSplitServer == null)
                {
                    await ChannelSession.Services.InitializeXSplitServer();
                }
                ssService = ChannelSession.Services.XSplitServer;
            }
            else if (this.SelectedStreamingSoftware == StreamingSoftwareTypeEnum.StreamlabsOBS)
            {
                if (ChannelSession.Services.StreamlabsOBSService == null)
                {
                    await ChannelSession.Services.InitializeStreamlabsOBSService();
                }
                ssService = ChannelSession.Services.StreamlabsOBSService;
            }

            if (ssService != null)
            {
                if (this.ActionType == StreamingActionTypeEnum.StartStopStream)
                {
                    await ssService.StartStopStream();
                }
                else if (this.ActionType == StreamingActionTypeEnum.SaveReplayBuffer)
                {
                    await ssService.SaveReplayBuffer();
                }
                else if (this.ActionType == StreamingActionTypeEnum.Scene && !string.IsNullOrEmpty(this.SceneName))
                {
                    await ssService.ShowScene(this.SceneName);
                }
                else if (!string.IsNullOrEmpty(this.SourceName))
                {
                    if (this.ActionType == StreamingActionTypeEnum.WebBrowserSource && !string.IsNullOrEmpty(this.SourceURL))
                    {
                        await ssService.SetWebBrowserSourceURL(this.SceneName, this.SourceName, url);
                    }
                    else if (this.ActionType == StreamingActionTypeEnum.SourceDimensions && this.SourceDimensions != null)
                    {
                        await ssService.SetSourceDimensions(this.SceneName, this.SourceName, this.SourceDimensions);
                    }
                    await ssService.SetSourceVisibility(this.SceneName, this.SourceName, this.SourceVisible);
                }
            }
        }
    }
}
