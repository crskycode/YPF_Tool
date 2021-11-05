using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YPF_Tool
{
    static class Extensions
    {
        public static string ReadAnsiString(this BinaryReader reader, int length, Encoding encoding)
        {
            var bytes = reader.ReadBytes(length);
            return encoding.GetString(bytes);
        }
    }
}
