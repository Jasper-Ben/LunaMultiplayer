﻿using Lidgren.Network;
using LunaCommon.Message.Base;
using UniLinq;

namespace LunaCommon.Message.Data.Chat
{
    public class PlayerChatChannels
    {
        public string PlayerName;
        public int ChannelCount;
        public string[] Channels = new string[0];

        public void Serialize(NetOutgoingMessage lidgrenMsg, bool dataCompressed)
        {
            lidgrenMsg.Write(PlayerName);
            lidgrenMsg.Write(ChannelCount);
            for (var i = 0; i < ChannelCount; i++)
            {
                lidgrenMsg.Write(Channels[i]);
            }
        }

        public void Deserialize(NetIncomingMessage lidgrenMsg, bool dataCompressed)
        {
            PlayerName = lidgrenMsg.ReadString();
            ChannelCount = lidgrenMsg.ReadInt32();
            Channels = ArrayPool<string>.Claim(ChannelCount);
            for (var i = 0; i < ChannelCount; i++)
            {
                Channels[i] = lidgrenMsg.ReadString();
            }
        }

        public void Recycle()
        {
            ArrayPool<string>.Release(ref Channels);
        }

        public int GetByteCount()
        {
            return PlayerName.GetByteCount() + sizeof(int) + Channels.GetByteCount(ChannelCount);
        }

        public PlayerChatChannels Clone()
        {
            if (MemberwiseClone() is PlayerChatChannels obj)
            {
                obj.PlayerName = PlayerName.Clone() as string;
                obj.Channels = Channels.Select(c => c.Clone() as string).ToArray();

                return obj;
            }

            return null;
        }
    }
}