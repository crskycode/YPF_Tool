using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YPF_Tool
{
    static class MurmurHash2
    {
        public static uint Compute(byte[] data, uint seed = 0)
        {
            // original https://github.com/aappleby/smhasher/blob/master/src/MurmurHash2.cpp

            uint len = (uint)data.Length;

            // 'm' and 'r' are mixing constants generated offline.
            // They're not really 'magic', they just happen to work well.

            const uint m = 0x5bd1e995;
            const int r = 24;

            // Initialize the hash to a 'random' value

            uint h = seed ^ len;

            // Mix 4 bytes at a time into the hash

            int data_p = 0;

            while (len >= 4)
            {
                uint k = BitConverter.ToUInt32(data, data_p);

                k *= m;
                k ^= k >> r;
                k *= m;

                h *= m;
                h ^= k;

                data_p += 4;
                len -= 4;
            }

            // Handle the last few bytes of the input array

            switch (len)
            {
                case 3:
                    h ^= (uint)data[data_p + 2] << 16;
                    goto case 2;
                case 2:
                    h ^= (uint)data[data_p + 1] << 8;
                    goto case 1;
                case 1:
                    h ^= data[data_p];
                    h *= m;
                    break;
            }

            // Do a few final mixes of the hash to ensure the last few
            // bytes are well-incorporated.

            h ^= h >> 13;
            h *= m;
            h ^= h >> 15;

            return h;
        }
    }
}
