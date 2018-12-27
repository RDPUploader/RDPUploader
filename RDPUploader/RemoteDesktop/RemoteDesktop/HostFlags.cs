using System;

namespace RemoteDesktop
{
    [Flags]
    public enum HostFlags
    {
        AutoReconnect = 0x800,
        ConnectLocalFileSystem = 0x1000,
        ConsoleSession = 0x4000,
        DefaultFlags = 0xe01,
        DesktopBackground = 0x10,
        DesktopComposition = 0x40,
        DisableFastPathOutput = 0x8000,
        DontPlaySound = 2,
        FontSmoothing = 0x20,
        MenuAnimation = 0x100,
        PeristentBmpCache = 0x400,
        PlaySoundOnHost = 1,
        PlaySoundOnPhone = 4,
        RecordSound = 8,
        ShowWindowContents = 0x80,
        SoundFlags = 7,
        UseScanCodes = 0x2000,
        VisualStyles = 0x200

    }
}