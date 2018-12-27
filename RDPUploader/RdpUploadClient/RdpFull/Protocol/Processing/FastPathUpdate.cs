using System;

namespace RdpUploadClient
{
    internal class FastPathUpdate
    {
        public static void ProcessFastPathUpdate(RdpPacket packet)
        {
            while (packet.Length - packet.Position > 3L)
            {
                int num = packet.ReadByte();
                FastPathUpdate.UpdateCode updateCode = (FastPathUpdate.UpdateCode)(num & 15);
                FastPathUpdate.Fragmentation fragmentation = (FastPathUpdate.Fragmentation)(num >> 4 & 3);
                bool flag = (num & 128) != 0;

                if (flag)
                {
                    packet.ReadByte();
                }

                if (fragmentation != FastPathUpdate.Fragmentation.FASTPATH_FRAGMENT_SINGLE)
                {
                    throw new RDFatalException("Invalid fragmentation!");
                }

                int littleEndian = packet.ReadLittleEndian16();

                switch (updateCode)
                {
                    case FastPathUpdate.UpdateCode.FASTPATH_UPDATETYPE_ORDERS:
                        {
                            int littleEndian2 = packet.ReadLittleEndian16();
                            Orders.processOrders(packet, (int)packet.Position + littleEndian - 2, littleEndian2);
                            break;
                        }

                    case FastPathUpdate.UpdateCode.FASTPATH_UPDATETYPE_BITMAP:
                        {
                            int littleEndian3 = packet.ReadLittleEndian16();
                            if (littleEndian3 != 1)
                            {
                                throw new RDFatalException("Invalid fastpath bitmap update!");
                            }
                            Bitmaps.processBitmapUpdates(packet);
                            break;
                        }

                    case FastPathUpdate.UpdateCode.FASTPATH_UPDATETYPE_PALETTE:
                        {
                            int littleEndian3 = packet.ReadLittleEndian16();

                            if (littleEndian3 != 2)
                            {
                                throw new RDFatalException("Invalid fastpath palette update!");
                            }

                            Palette.processPalette(packet);
                            break;
                        }

                    case FastPathUpdate.UpdateCode.FASTPATH_UPDATETYPE_SYNCHRONIZE:
                        packet.Position = packet.Position + (long)littleEndian;
                        break;

                    case FastPathUpdate.UpdateCode.FASTPATH_UPDATETYPE_SURFCMDS:
                        packet.Position = packet.Position + (long)littleEndian;
                        break;

                    case FastPathUpdate.UpdateCode.FASTPATH_UPDATETYPE_PTR_NULL:
                        packet.Position = packet.Position + (long)littleEndian;
                        break;

                    case FastPathUpdate.UpdateCode.FASTPATH_UPDATETYPE_PTR_DEFAULT:
                        packet.Position = packet.Position + (long)littleEndian;
                        break;

                    case FastPathUpdate.UpdateCode.FASTPATH_UPDATETYPE_PTR_POSITION:
                        packet.Position = packet.Position + (long)littleEndian;
                        break;

                    case FastPathUpdate.UpdateCode.FASTPATH_UPDATETYPE_COLOR:
                        packet.Position = packet.Position + (long)littleEndian;
                        break;

                    case FastPathUpdate.UpdateCode.FASTPATH_UPDATETYPE_CACHED:
                        packet.Position = packet.Position + (long)littleEndian;
                        break;

                    case FastPathUpdate.UpdateCode.FASTPATH_UPDATETYPE_POINTER:
                        packet.Position = packet.Position + (long)littleEndian;
                        break;
                }
            }
        }

        [Flags]
        public enum UpdateCode
        {
            FASTPATH_UPDATETYPE_ORDERS,
            FASTPATH_UPDATETYPE_BITMAP,
            FASTPATH_UPDATETYPE_PALETTE,
            FASTPATH_UPDATETYPE_SYNCHRONIZE,
            FASTPATH_UPDATETYPE_SURFCMDS,
            FASTPATH_UPDATETYPE_PTR_NULL,
            FASTPATH_UPDATETYPE_PTR_DEFAULT,
            FASTPATH_UPDATETYPE_PTR_POSITION = 8,
            FASTPATH_UPDATETYPE_COLOR,
            FASTPATH_UPDATETYPE_CACHED,
            FASTPATH_UPDATETYPE_POINTER
        }

        [Flags]
        public enum Fragmentation
        {
            FASTPATH_FRAGMENT_SINGLE,
            FASTPATH_FRAGMENT_LAST,
            FASTPATH_FRAGMENT_FIRST,
            FASTPATH_FRAGMENT_NEXT
        }

    }
}