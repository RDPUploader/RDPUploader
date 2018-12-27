using System;

namespace RdpUploadClient
{
    internal interface IVirtualChannel
    {
        void channel_process(RdpPacket data);

        void close();

        int ChannelID { get; }

        string ChannelName { get; }

    }
}