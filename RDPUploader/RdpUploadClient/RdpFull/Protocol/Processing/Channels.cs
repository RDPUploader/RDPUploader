using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RdpUploadClient
{
    internal class Channels
    {
        private static List<IVirtualChannel> m_Channels = new List<IVirtualChannel>();
        internal static RdpPacket m_FullPacket;

        internal static void channel_process(int channelID, RdpPacket data)
        {
            data.ReadLittleEndian32();
            var num = (CHANNEL_FLAG)data.ReadLittleEndian32();

            if (num.HasFlag(CHANNEL_FLAG.CHANNEL_FLAG_FIRST))
            {
                m_FullPacket = new RdpPacket();
            }

            m_FullPacket.Append(data);

            if (num.HasFlag(CHANNEL_FLAG.CHANNEL_FLAG_LAST))
            {
                m_FullPacket.Position = 0L;

                foreach (IVirtualChannel channel in m_Channels)
                {
                    if (channel.ChannelID == channelID)
                    {
                        channel.channel_process(m_FullPacket);
                    }
                }
            }
        }

        public static List<IVirtualChannel> RegisteredChannels
        {
            get
            {
                return m_Channels;
            }
        }

        // Битовые флаги
        [Flags]
        private enum CHANNEL_FLAG
        {
            CHANNEL_FLAG_FIRST = 0x00000001,
            CHANNEL_FLAG_LAST = 0x00000002,
            CHANNEL_FLAG_SHOW_PROTOCOL = 0x00000010,
            CHANNEL_FLAG_SUSPEND = 0x00000020,
            CHANNEL_FLAG_RESUME = 0x00000040,
            CHANNEL_PACKET_COMPRESSED = 0x00200000,
            CHANNEL_PACKET_AT_FRONT = 0x00400000,
            CHANNEL_PACKET_FLUSHED = 0x00800000,
            CompressionTypeMask = 0x000F0000
        }

    }
}