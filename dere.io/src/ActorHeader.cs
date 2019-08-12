using System;
using System.IO;

namespace dere.io
{
    public struct ActorHeader
    {
        public uint signature;
        public uint version;
        public bool hasBody;
        public uint motionCount;

        public static ActorHeader LoadFromStream(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            ActorHeader header = new ActorHeader();
            header.signature = reader.ReadUInt32();
            header.version = reader.ReadUInt32();
            header.hasBody = reader.ReadUInt32() != 0;
            header.motionCount = reader.ReadUInt32();

            if (header.signature != 0x52544341)
                throw new InvalidDataException("Invalid actor header signature");
            if (header.version != 0xF1)
                throw new InvalidDataException("Invalid actor header version");
            if (!header.hasBody)
                throw new InvalidDataException("Invalid actor without body");
            return header;
        }
    }
}
