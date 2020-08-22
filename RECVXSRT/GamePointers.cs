using System;

namespace RECVXSRT
{
    public class GamePointers
    {
        public IntPtr Time { get; set; }
        public IntPtr Room { get; set; }
        public IntPtr Status { get; set; }
        public IntPtr Health { get; set; }
        public IntPtr Character { get; set; }
        public IntPtr Inventory { get; set; }
        public IntPtr Difficulty { get; set; }
        public IntPtr Enemy { get; set; }
        public IntPtr EnemyCount { get; set; }
        public IntPtr Saves { get; set; }
        public IntPtr Retries { get; set; }
    }
}