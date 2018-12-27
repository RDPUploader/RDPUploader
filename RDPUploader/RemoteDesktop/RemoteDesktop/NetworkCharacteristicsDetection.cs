using System;

namespace RemoteDesktop
{
    public class NetworkCharacteristicsDetection : IVirtualChannel
    {
        private int m_ChannelID;

        public NetworkCharacteristicsDetection(int ChannelID)
        {
            this.m_ChannelID = ChannelID;
        }

        public void channel_process(RdpPacket data)
        {
        }

        public void close()
        {
        }

        public int ChannelID
        {
            get
            {
                return this.m_ChannelID;
            }
        }

        public string ChannelName
        {
            get
            {
                return "rdpdr\0\0";
            }
        }

    }
}