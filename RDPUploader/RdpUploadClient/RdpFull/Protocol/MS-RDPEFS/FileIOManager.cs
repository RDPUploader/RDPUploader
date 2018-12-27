using System;

namespace RdpUploadClient
{
    internal class FileIOManager
    {
        private static FileIOHandle[] m_Slots = new FileIOHandle[0x100];

        internal static void clearFile(int handle)
        {
            m_Slots[handle - 1] = null;
        }

        internal static void closeFiles()
        {
            for (int i = 0; i < (m_Slots.Length - 1); i++)
            {
                if (m_Slots[i] != null)
                {
                    m_Slots[i].Close();
                    m_Slots[i] = null;
                }
            }
        }

        internal static FileIOHandle getFile(int handle)
        {
            return m_Slots[handle - 1];
        }

        internal static int getFileLength(string path)
        {
            for (int i = 0; i < (m_Slots.Length - 1); i++)
            {
                if ((m_Slots[i] != null) && (m_Slots[i].fullpath == path))
                {
                    if (m_Slots[i].stream != null)
                    {
                        return (int) m_Slots[i].stream.Length;
                    }
                    return -1;
                }
            }
            return -1;
        }

        internal static int getFreeSlot()
        {
            for (int i = 0; i < (m_Slots.Length - 1); i++)
            {
                if (m_Slots[i] == null)
                {
                    return (i + 1);
                }
            }
            return -1;
        }

        internal static void putFile(int handle, FileIOHandle file)
        {
            m_Slots[handle - 1] = file;
        }

    }
}