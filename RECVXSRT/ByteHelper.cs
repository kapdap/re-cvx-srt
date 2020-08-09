namespace RECVXSRT
{
    public class ByteHelper
    {
        public static short SwapBytes(short bytes, bool swap = true)
        {
            return (short)SwapBytes((ushort)bytes, swap);
        }

        public static ushort SwapBytes(ushort bytes, bool swap = true)
        {
            if (!swap) return bytes;

            int b1 = (bytes >> 0) & 0xff;
            int b2 = (bytes >> 8) & 0xff;

            return (ushort)(b1 << 8 | b2 << 0);
        }

        public static int SwapBytes(int bytes, bool swap = true)
        {
            return (int)SwapBytes((uint)bytes, swap);
        }

        public static uint SwapBytes(uint bytes, bool swap = true)
        {
            if (!swap) return bytes;

            return ((bytes & 0x000000ff) << 24) +
                   ((bytes & 0x0000ff00) << 8) +
                   ((bytes & 0x00ff0000) >> 8) +
                   ((bytes & 0xff000000) >> 24);
        }
    }
}