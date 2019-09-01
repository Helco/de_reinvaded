using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dere.io.prv;

namespace dere.io
{
    [Flags]
    internal enum LeafNode_Flags
    {
        HasEvents = (1 << 0),
        HasNames = (1 << 1)
    }

    public enum GeMotionPathInterpolation
    {
        Linear = 0,
        Hermite,
        Slerp,
        Squad,
        Tripod,
        HermiteZeroDeriv
    }

    public enum GeMotionVKInterpolation
    {
        Linear,
        Hermite,
        HermiteZeroDeriv
    }

    public enum GeMotionQKInterpolation
    {
        Linear,
        Slerp,
        Squad
    }

    internal static class GeMotionTimeSpecifier
    {
        public static float[] LoadFromReader(BinaryReader reader, int frameCount, byte compression)
        {
            if ((compression & (1 << 1)) > 0)
            {
                float startTime = reader.ReadSingle();
                float deltaTime = reader.ReadSingle();
                return Enumerable
                    .Range(0, frameCount)
                    .Select(i => startTime + i * deltaTime)
                    .ToArray();
            }
            else
            {
                return Enumerable
                    .Range(0, frameCount)
                    .Select(_ => reader.ReadSingle())
                    .ToArray();
            }
        }
    }

    public class GeMotionVKFrameList
    {
        public bool isLooping;
        public GeMotionVKInterpolation interpolation;
        public float[] frameTimes;
        public Vector3[] frames;

        public static GeMotionVKFrameList LoadFromReader(BinaryReader reader)
        {
            GeMotionVKFrameList thiz = new GeMotionVKFrameList();
            reader.ReadUInt32(); // block size
            thiz.isLooping = reader.ReadByte() != 0;
            byte compression = reader.ReadByte();
            thiz.interpolation = reader.ReadEnum<GeMotionVKInterpolation>(1);
            reader.ReadByte(); // padding
            int frameCount = reader.ReadInt32();
            thiz.frameTimes = GeMotionTimeSpecifier.LoadFromReader(reader, frameCount, compression);
            thiz.frames = Enumerable
                .Range(0, frameCount)
                .Select(_ => Vector3.LoadFromReader(reader))
                .ToArray();
            return thiz;
        }
    }

    public class GeMotionQKFrameList
    {
        public bool isLooping;
        public GeMotionQKInterpolation interpolation;
        public float[] frameTimes;
        public Quaternion[] frames;

        public static GeMotionQKFrameList LoadFromReader(BinaryReader reader)
        {
            GeMotionQKFrameList thiz = new GeMotionQKFrameList();
            reader.ReadUInt32(); // block size
            thiz.isLooping = reader.ReadByte() != 0;
            byte compression = reader.ReadByte();
            thiz.interpolation = reader.ReadEnum<GeMotionQKInterpolation>(1);
            reader.ReadByte(); // padding
            int frameCount = reader.ReadInt32();
            thiz.frameTimes = GeMotionTimeSpecifier.LoadFromReader(reader, frameCount, compression);

            if ((compression & (1 << 0)) > 0)
            {
                var hinge = Vector3.LoadFromReader(reader);
                thiz.frames = Enumerable
                    .Range(0, frameCount)
                    .Select(_ => Quaternion.FromAxisAngle(hinge, reader.ReadSingle()))
                    .ToArray();
            }
            else
            {
                thiz.frames = Enumerable
                    .Range(0, frameCount)
                    .Select(_ => Quaternion.LoadFromReader(reader))
                    .ToArray();
            }

            return thiz;
        }
    }

    public class GeMotionPath
    {
        public string name;
        public GeMotionQKFrameList qkFrames = null;
        public GeMotionVKFrameList vkFrames = null;

        public static GeMotionPath LoadFromReader(BinaryReader reader, string name = "")
        {
            GeMotionPath thiz = new GeMotionPath();
            thiz.name = name;

            // Read flags, the ugly way :(
            UInt16 flags = reader.ReadUInt16();
            bool hasRotationKeys = (flags & (1 << 0)) > 0;
            bool hasTranslationKeys = (flags & (1 << 1)) > 0;
            if (reader.ReadUInt16() != 0x1001)
                throw new InvalidDataException("Unsupported motion path version");

            if (hasTranslationKeys)
                thiz.vkFrames = GeMotionVKFrameList.LoadFromReader(reader);
            if (hasRotationKeys)
                thiz.qkFrames = GeMotionQKFrameList.LoadFromReader(reader);

            return thiz;
        }
    }

    public class GeMotion
    {
        public string name;
        public GeMotionPath[] paths;

        public static GeMotion LoadFromStream(Stream stream)
        {
            GeMotion thiz = new GeMotion();
            BinaryReader reader = new BinaryReader(stream);
            if (reader.ReadUInt32() != 0x424E544D) // "MTNB"
                throw new InvalidDataException("Invalid motion signature");
            if (reader.ReadUInt32() != 0xF0)
                throw new InvalidDataException("Unsupported motion version");
            UInt16 nameLen = reader.ReadUInt16();
            bool maintainNames = reader.ReadByte() != 0;
            if (reader.ReadByte() != 0x02)
                throw new InvalidDataException("Motion root node is not a leaf");

            thiz.name = nameLen > 0
                ? reader.ReadSizedCString(nameLen)
                : "";

            // Root leaf
            int pathCount = reader.ReadInt32();
            reader.ReadUInt32(); // name checksum
            var flags = reader.ReadFlags<LeafNode_Flags>();
            if (flags.HasFlag(LeafNode_Flags.HasEvents))
                throw new InvalidDataException("Motion events are not supported");
            string[] pathNames = null;
            if (flags.HasFlag(LeafNode_Flags.HasNames))
                pathNames = StrBlock.LoadFromReader(reader);

            thiz.paths = Enumerable
                .Range(0, pathCount)
                .Select(i => GeMotionPath.LoadFromReader(reader, pathNames == null ? "" : pathNames[i]))
                .ToArray();
            return thiz;
        }
    }
}
