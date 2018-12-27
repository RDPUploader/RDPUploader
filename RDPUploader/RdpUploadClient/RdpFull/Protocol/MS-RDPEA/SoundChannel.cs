using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RdpUploadClient
{
    internal class SoundChannel : IVirtualChannel
    {
        public void channel_process(RdpPacket data)
        {
            Debug.WriteLine("Channel SOUND!");
        }

        public void close()
        {

        }

        public int ChannelID
        {
            get
            {
                return 0x03ee; // 1006
            }
        }

        public string ChannelName
        {
            get
            {
                return "rdpsnd\0\0";
            }
        }

    }
}