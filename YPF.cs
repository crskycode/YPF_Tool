using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YPF_Tool
{
    static class YPF
    {
        public static void Parse(string filePath)
        {
            using var reader = new BinaryReader(File.OpenRead(filePath));

            if (reader.ReadInt32() != 0x00465059)
            {
                throw new Exception("Not a YPF file.");
            }

            var version = reader.ReadInt32();

            if (version != 0x22B && version < 0xEA)
            {
                throw new Exception("Unsupported file version.");
            }

            var entryCount = reader.ReadInt32();
            var indexSize = reader.ReadInt32() - 0x20;

            reader.BaseStream.Position = 0x20;

            var indexBytes = reader.ReadBytes(indexSize);

            if (indexBytes.Length != indexSize)
            {
                throw new Exception("Failed to read the index.");
            }

            var indexStream = new MemoryStream(indexBytes);
            var indexReader = new BinaryReader(indexStream);

            var nameLengthTable = CreateNameLengthDecryptionTable();
            var nameEncoding = Encoding.GetEncoding("shift_jis");

            for (var i = 0; i < entryCount; i++)
            {
                var entryAddr = 0x20 + indexReader.BaseStream.Position;

                var nameHash = indexReader.ReadUInt32(); // using MurmurHash2

                // Read name length
                int nameLength = indexReader.ReadByte();
                // Decrypt name length
                nameLength = nameLengthTable[nameLength];
                // Read name bytes
                var nameBytes = indexReader.ReadBytes(nameLength);
                // Decrypt name bytes
                DecryptNameBytes(nameBytes);

                var entryName = nameEncoding.GetString(nameBytes);

                var entryType = indexReader.ReadByte();
                var compressMethod = indexReader.ReadByte(); // 0 - no compression, 1 - deflate or snappy
                var originalSize = indexReader.ReadInt32();
                var compressedSize = indexReader.ReadInt32();
                var dataOffset = indexReader.ReadInt64();
                var dataHash = indexReader.ReadInt32(); // using MurmurHash2

                Debug.WriteLine(string.Format("{0:X8} | type={1:X2} compressMethod={2:X2} originalSize={3:X8} compressedSize={4:X8} offset={5:X8} hash={6:X8} name=\"{7}\"",
                    entryAddr, entryType, compressMethod, originalSize, compressedSize, dataOffset, dataHash, entryName));
            }

            Debug.Assert(indexStream.Position == indexStream.Length);
        }

        public static void Create(string filePath, string rootPath)
        {
            var entries = new List<TEntry>();

            // Grab files

            foreach (var localPath in Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories))
            {
                entries.Add(new TEntry
                {
                    LocalPath = localPath,
                    Path = Path.GetRelativePath(rootPath, localPath)
                });
            }

            var nameLengthTable = CreateNameLengthEncryptionTable();
            var nameEncoding = Encoding.GetEncoding("shift_jis");

            using var writer = new BinaryWriter(File.Create(filePath));

            // "YPF"
            writer.Write(0x00465059);

            // Version
            writer.Write(0x1F4);

            // Entry Count
            writer.Write(entries.Count);

            // Index size
            writer.Write(0);

            // Reserved
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            // Begin of index

            var indexPos = writer.BaseStream.Position;

            // Write index

            foreach (var entry in entries)
            {
                var nameBytes = nameEncoding.GetBytes(entry.Path);

                if (nameBytes.Length > 255)
                {
                    throw new Exception($"Path is too long. file : \"{entry.Path}\"");
                }

                var nameHash = MurmurHash2.Compute(nameBytes);
                var nameLength = Convert.ToByte(nameLengthTable[nameBytes.Length]);
                var nameExt = Path.GetExtension(entry.Path);

                EncryptNameBytes(nameBytes);

                EntryTypeDict.TryGetValue(nameExt, out byte entryType);

                writer.Write(nameHash);
                writer.Write(nameLength);
                writer.Write(nameBytes);
                writer.Write(entryType);

                entry.Position = writer.BaseStream.Position;

                writer.Write((byte)0); // compress method
                writer.Write(0);       // original size
                writer.Write(0);       // compressed size
                writer.Write((long)0); // data offset
                writer.Write(0);       // data hash
            }

            // End of index

            var indexSize = Convert.ToUInt32(writer.BaseStream.Position - indexPos) + 0x20;

            // Write entry data

            foreach (var entry in entries)
            {
                Console.WriteLine($"Add \"{entry.Path}\"");

                var data = File.ReadAllBytes(entry.LocalPath);

                entry.DataOffset = writer.BaseStream.Position;
                entry.DataSize = (uint)data.Length;
                entry.DataHash = MurmurHash2.Compute(data);

                writer.Write(data);
                writer.Write(0); // repair data size
            }

            // Write index

            foreach (var entry in entries)
            {
                writer.BaseStream.Position = entry.Position;

                writer.Write((byte)0);          // compress method, 0 - no compression
                writer.Write(entry.DataSize);   // original size
                writer.Write(entry.DataSize);   // compressed size, equal to original size if no compression
                writer.Write(entry.DataOffset);
                writer.Write(entry.DataHash);
            }

            // Write index size

            writer.BaseStream.Position = 0x0C;
            writer.Write(indexSize);

            // Done

            writer.Flush();
        }

        static void DecryptNameBytes(byte[] bytes)
        {
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(-bytes[i] - 1);
            }

            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= 0x36;
            }
        }

        static void EncryptNameBytes(byte[] bytes)
        {
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= 0x36;
            }

            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(-(bytes[i] + 1));
            }
        }

        static void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }

        static int[] CreateNameLengthDecryptionTable()
        {
            var tbl = Enumerable.Range(0, 256).ToArray();

            Swap(ref tbl[0x03], ref tbl[0x0A]);
            Swap(ref tbl[0x06], ref tbl[0x35]);
            Swap(ref tbl[0x09], ref tbl[0x0B]);
            Swap(ref tbl[0x0C], ref tbl[0x10]);
            Swap(ref tbl[0x0D], ref tbl[0x13]);
            Swap(ref tbl[0x11], ref tbl[0x18]);
            Swap(ref tbl[0x15], ref tbl[0x1B]);
            Swap(ref tbl[0x1C], ref tbl[0x1E]);
            Swap(ref tbl[0x20], ref tbl[0x23]);
            Swap(ref tbl[0x26], ref tbl[0x29]);
            Swap(ref tbl[0x2C], ref tbl[0x2F]);
            Swap(ref tbl[0x2E], ref tbl[0x14]);

            Array.Reverse(tbl);

            return tbl;
        }

        static int[] CreateNameLengthEncryptionTable()
        {
            var tbl = CreateNameLengthDecryptionTable();

            return Enumerable.Range(0, 256)
                .Select(i => Array.IndexOf(tbl, i))
                .ToArray();
        }

        static readonly Dictionary<string, byte> EntryTypeDict = new()
        {
            { ".bmp", 1 },
            { ".png", 2 },
            { ".jpg", 3 },
            { ".gif", 4 },
            { ".wav", 5 },
            { ".ogg", 6 },
            { ".psd", 7 },
            { ".ycg", 8 },
            { ".psb", 9 },
            { ".webp", 10 },
        };

        class TEntry
        {
            public string LocalPath;
            public string Path;
            public long Position;
            public long DataOffset;
            public uint DataSize;
            public uint DataHash;
        }
    }
}
