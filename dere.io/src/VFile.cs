using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using dere.io.prv;

namespace dere.io
{
    public class VFile
    {
        private VFile(BinaryReader reader)
        {
            this.reader = reader;
        }

        private BinaryReader reader;
        private Dictionary<string, (long start, long size)> files =
            new Dictionary<string, (long start, long size)>();

        public IReadOnlyCollection<string> FileNames => files.Keys;

        public Stream OpenFile(string name) {
            if (!files.ContainsKey(name))
                throw new InvalidOperationException("VFile does not contain file: " + name);
            var value = files[name];
            Stream baseStream = reader.BaseStream;
            baseStream.Seek(value.start, SeekOrigin.Begin);
            return new RangeStream(baseStream, value.size, false, false);
        }

        private void readNode(string parentPath)
        {
            string curPath = parentPath + reader.ReadZString();
            if (curPath.Length == 0 && files.Count > 0)
                throw new InvalidOperationException("Invalid empty file name in vfile");

            reader.BaseStream.Seek(3 * 4, SeekOrigin.Current); // skipping time and attributes
            var size = reader.ReadUInt32();
            var start = reader.ReadUInt32();
            if (size > 0)
                files.Add(curPath, (start, size));

            var hintsSize = reader.ReadUInt32();
            reader.BaseStream.Seek(hintsSize, SeekOrigin.Current);

            if (reader.ReadUInt32() != 0xFFFFFFFF)
                readNode(curPath == "" ? "" : curPath + "/");
            if (reader.ReadUInt32() != 0xFFFFFFFF)
                readNode(parentPath);
        }

        public static VFile LoadFromStream(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            if (reader.ReadUInt32() != 0x30304656)
                throw new InvalidDataException("Invalid vfile signature");
            if (reader.ReadUInt16() != 0)
                throw new InvalidDataException("Unsupported vfile version");
            reader.ReadUInt16(); // unknown
            if (reader.ReadUInt32() != 0)
                throw new InvalidDataException("Unsupported dispersed vfile");
            reader.BaseStream.Position = reader.ReadUInt32(); // to directoryOffset
            // skipping dataLength, endPosition

            if (reader.ReadUInt32() != 0x31305444)
                throw new InvalidDataException("Invalid dirtree signature");
            reader.ReadUInt32(); // skipping size

            VFile vfile = new VFile(reader);
            if (reader.ReadUInt32() != 0xFFFFFFFF)
                vfile.readNode("");

            return vfile;
        }
    }
}