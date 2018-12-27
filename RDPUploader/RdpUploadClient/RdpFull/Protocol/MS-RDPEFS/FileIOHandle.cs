using System;
using System.IO;

namespace RdpUploadClient
{
    internal class FileIOHandle
    {
        internal uint createOptions;
        internal uint desiredAccess;
        internal uint deviceId;
        internal bool directory;
        internal string e = "";
        internal string f = "";
        internal string fullpath = "";
        internal string path = "";
        internal FileStream stream;
        internal bool tempFile;

        internal void Close()
        {
            if (this.stream != null)
            {
                this.stream.Close();
                this.stream.Dispose();
                this.stream = null;
            }
        }

    }
}