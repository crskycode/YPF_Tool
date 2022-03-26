using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace YPF_Tool
{
    static class Extensions
    {
        public static string ReadAnsiString(this BinaryReader reader, int length, Encoding encoding)
        {
            var bytes = reader.ReadBytes(length);
            return encoding.GetString(bytes);
        }

        public static byte[] Inflate(byte[] buffer)
        {
            byte[] block = new byte[256];
            MemoryStream outputStream = new MemoryStream();

            Inflater inflater = new Inflater();
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            using (InflaterInputStream inflaterInputStream = new InflaterInputStream(memoryStream, inflater))
            {
                while (true)
                {
                    int numBytes = inflaterInputStream.Read(block, 0, block.Length);
                    if (numBytes < 1)
                        break;
                    outputStream.Write(block, 0, numBytes);
                }
            }

            return outputStream.ToArray();
        }

    }
}
