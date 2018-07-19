﻿using MixItUp.Base.Commands;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MixItUp.Base.Model.Remote
{
    [DataContract]
    public class RemoteBoardItemModel
    {
        [DataMember]
        public Guid ID { get; set; }
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public RemoteBoardItemSizeEnum Size { get; set; }

        [DataMember]
        public int XPosition { get; set; }
        [DataMember]
        public int YPosition { get; set; }

        [JsonIgnore]
        public RemoteCommand Command { get { return ChannelSession.Settings.RemoteCommands.FirstOrDefault(c => c.ID.Equals(this.ID)); } }

        public RemoteBoardItemModel() { }

        public virtual void SetValuesFromCommand()
        {
            RemoteCommand command = this.Command;
            if (command != null)
            {
                this.Name = command.Name;
            }
        }
    }

    [DataContract]
    public class RemoteBoardButtonModel : RemoteBoardItemModel
    {
        [DataMember]
        public string BackgroundColor { get; set; }

        [DataMember]
        public string TextColor { get; set; }

        [DataMember]
        public string ImageName { get; set; }

        public RemoteBoardButtonModel() { }

        public RemoteBoardButtonModel(RemoteCommand command, int xPosition, int yPosition)
        {
            this.ID = command.ID;
            this.Name = command.Name;
            this.XPosition = xPosition;
            this.YPosition = yPosition;

            this.SetValuesFromCommand();
        }

        public override void SetValuesFromCommand()
        {
            base.SetValuesFromCommand();

            RemoteCommand command = this.Command;
            if (command != null)
            {
                this.BackgroundColor = command.BackgroundColor;
                this.TextColor = command.TextColor;
                this.ImageName = command.ImageName;
            }
        }
    }
}
