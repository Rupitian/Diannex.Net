using System;

namespace Diannex.NET
{
    public static class Extensions
    {
        public static int ReadInt32(this byte[] data, ref int index)
        {
            int val = BitConverter.ToInt32(data, index);
            index += 4;
            return val;
        }

        public static double ReadDouble(this byte[] data, ref int index)
        {
            double val = BitConverter.ToDouble(data, index);
            index += 8;
            return val;
        }
    }
}
