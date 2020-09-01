using System;

namespace RECVXSRT
{
    [Flags]
    public enum ProgramFlags : byte
    {
        None = 0,

        Debug = 1,
        DebugEnemy = 2,

        NoTitleBar = 4,
        AlwaysOnTop = 8,
        Transparent = 16,

        NoInventory = 32,
        NoEnemyHealth = 64,

        DirectXOverlay = 128
    }
}