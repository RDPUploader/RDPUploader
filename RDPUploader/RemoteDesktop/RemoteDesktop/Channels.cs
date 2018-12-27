using System;
using System.Collections.Generic;

namespace RemoteDesktop
{
    internal class Channels
    {
        private const int CHANNEL_FLAG_FIRST = 1;
        private const int CHANNEL_FLAG_LAST = 2;
        
        internal static void channel_process(int ChannelID, RdpPacket data)
        {
            data.getLittleEndian32();
            int num = data.getLittleEndian32();
            if ((num & 1) != 0)
            {
                RDPClient.m_FullPacket = new RdpPacket();
            }
            RDPClient.m_FullPacket.append(data);
            if ((num & 2) != 0)
            {
                RDPClient.m_FullPacket.Position = 0L;
                foreach (IVirtualChannel channel in RDPClient.m_Channels)
                {
                    if (channel.ChannelID == ChannelID)
                    {
                        channel.channel_process(RDPClient.m_FullPacket);
                    }
                }
            }
        }

        public static List<IVirtualChannel> RegisteredChannels
        {
            get
            {
                return RDPClient.m_Channels;
            }
        }

    }
}