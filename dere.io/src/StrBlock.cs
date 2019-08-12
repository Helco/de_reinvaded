using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace dere.io.prv
{
    public static class StrBlock
    {
        public static string[] LoadFromReader(BinaryReader reader)
        {
            if (reader.ReadUInt32() != 0x424B4253)
                throw new InvalidDataException("Invalid string block signature");
            int count = reader.ReadInt32();
            reader.BaseStream.Seek(count * 4 + 4, SeekOrigin.Current);
            return Enumerable.Range(0, count)
                .Select((_) => reader.ReadCString())
                .ToArray();
        }
    }
}
