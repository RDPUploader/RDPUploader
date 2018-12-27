using System;
using System.Collections.Generic;
using System.IO;

namespace RemoteDesktop
{
    public class PacketLogger
    {
        private Dictionary<int, PacketBlob> m_Blobs = new Dictionary<int, PacketBlob>();
        private Dictionary<int, Queue<ReceivedPacket>> m_ReceivedPackets = new Dictionary<int, Queue<ReceivedPacket>>();
        private Dictionary<string, byte> m_Sockets = new Dictionary<string, byte>();
        private Dictionary<int, DateTime> m_SocketsDelay = new Dictionary<int, DateTime>();
        private Dictionary<int, long> m_SocketsLastReceivedTick = new Dictionary<int, long>();

        public void AddBlob(PacketType type, byte[] data, string socket)
        {
            if (!m_Blobs.ContainsKey(this.BuildBlobId(type, socket)))
            {
                m_Blobs.Add(BuildBlobId(type, socket), new PacketBlob(data, type));
            }
        }

        private int BuildBlobId(PacketType type, string socket)
        {
            byte num;
            if (!this.m_Sockets.TryGetValue(socket, out num))
            {
                num = (byte) (this.m_Sockets.Count + 1);
                this.m_Sockets.Add(socket, num);
                byte[] bytes = ASCIIEncoding.GetBytes(socket);
            }
            return (((int) type) | (num << 0x10));
        }

        public byte[] GetBlob(PacketType type, string socket)
        {
            PacketBlob blob;
            if (m_Blobs.TryGetValue(BuildBlobId(type, socket), out blob))
            {
                return blob.Data;
            }
            return null;
        }

        public byte[] GetSSLPublicKey(string socket)
        {
            PacketBlob blob;
            if (m_Blobs.TryGetValue(BuildBlobId(PacketType.PublicKey, socket), out blob))
            {
                return blob.Data;
            }
            return null;
        }

        public void SSLPublicKey(byte[] data, string socket)
        {
            if (GetSSLPublicKey(socket) == null)
            {
                m_Blobs.Add(BuildBlobId(PacketType.PublicKey, socket), new PacketBlob(data, PacketType.PublicKey));
            }
        }

        private class PacketBlob
        {
            private byte[] m_Data;
            private PacketLogger.PacketType m_Type;

            public PacketBlob(byte[] data, PacketLogger.PacketType type)
            {
                this.m_Data = data;
                this.m_Type = type;
            }

            public byte[] Data
            {
                get
                {
                    return this.m_Data;
                }
            }

            public PacketLogger.PacketType Type
            {
                get
                {
                    return this.m_Type;
                }
            }
        }

        [Flags]
        public enum PacketType
        {
            BitmapCache = 7,
            ClientRandom = 8,
            Exception = 9,
            HostSettings = 6,
            None = -1,
            NTLM_ClientChallenge = 4,
            NTLM_ExportedSessionKey = 5,
            NTLM_KeyExchangeKey = 11,
            NTLM_ResponseKeyNT = 10,
            PublicKey = 3,
            Received = 1,
            Sent = 2,
            SocketName = 12
        }

        private class ReceivedPacket
        {
            private byte[] m_Data;
            private uint m_Ticks;

            public ReceivedPacket(byte[] data, uint ticks)
            {
                this.m_Data = data;
                this.m_Ticks = ticks;
            }

            public byte[] Data
            {
                get
                {
                    return this.m_Data;
                }
            }

            public uint Ticks
            {
                get
                {
                    return this.m_Ticks;
                }
            }
        }

    }
}