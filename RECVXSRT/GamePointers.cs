using System;

namespace RECVXSRT
{
    public class GamePointers
    {
        public IntPtr Time { get; set; }
        public IntPtr Room { get; set; }
        public IntPtr Serum { get; set; }
        public IntPtr Poison { get; set; }
        public IntPtr Health { get; set; }
        public IntPtr Character { get; set; }
        public IntPtr Inventory { get; set; }
        public IntPtr Difficulty { get; set; }
        public IntPtr Enemies { get; set; }
    }
}