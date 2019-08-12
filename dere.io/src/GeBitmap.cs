using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using dere.io.prv;

namespace dere.io
{
    [Flags]
    enum GeBM_Flags
    {
        WhAreLog2 = (1 << 0),
        HasColorKey = (1 << 1),
        HasAlpha = (1 << 2),
        HasPalette = (1 << 3),
        IfNotLog2AreByte = (1 << 5)
    }

    [Flags]
    enum GeBM_Palette_Flags
    {
        IsSize256 = (1 << 5),
        IsCompressed = (1 << 6),

        FormatMask = (1 << 5) - 1
    }

    public interface IBitmapFactory
    {
        IBitmap CreateNew(int width, int height);
    }
    public interface IBitmap
    {
        void SetPixel(int x, int y, Color c);
    }

    public static class GeBitmap
    {
        private static int shiftRRoundup(int val, int shift) =>
            (val + (1 << shift) - 1) >> shift;

        private static Color[] readPalette(BinaryReader reader)
        {
            var rawFlags = reader.ReadByte();
            var flags = EnumUtils.intToFlags<GeBM_Palette_Flags>((uint)rawFlags);
            if (flags.HasFlag(GeBM_Palette_Flags.IsCompressed))
                throw new InvalidDataException("Unsupported compressed palette");
            var info = new PixelFormatInfo((GePixelFormat)(rawFlags & (byte)GeBM_Palette_Flags.FormatMask));
            int count = flags.HasFlag(GeBM_Palette_Flags.IsSize256)
                ? 256
                : (int)reader.ReadByte();
            return Enumerable.Repeat(0, count).Select(_ => info.ReadColor(reader)).ToArray();
        }

        private static IBitmap readMipmap(BinaryReader reader, IBitmapFactory bitmapFactory, int level, int totalWidth, int totalHeight, GePixelFormat format, Color[] palette, Nullable<Color> colorKey)
        {
            int curWidth = shiftRRoundup(totalWidth, level);
            int curHeight = shiftRRoundup(totalHeight, level);
            Nullable<PixelFormatInfo> info = format == GePixelFormat.Palette8
                ? new Nullable<PixelFormatInfo>()
                : new PixelFormatInfo(format);
            IBitmap image = bitmapFactory.CreateNew(curWidth, curHeight);
            for (int y = 0; y < curHeight; y++) {
                for (int x = 0; x < curWidth; x++) {
                    Color color;
                    if (info == null)
                        color = palette[reader.ReadByte()];
                    else
                        color = info.Value.ReadColor(reader);
                    if (colorKey.HasValue && colorKey.Value == color)
                        color = new Color(0, 0, 0, 0);
                    image.SetPixel(x, y, color);
                }
            }
            return image;
        }

        public static IBitmap[] LoadFromStream(Stream stream, IBitmapFactory bitmapFactory)
        {
            BinaryReader reader = new BinaryReader(stream);
            if (reader.ReadUInt32() != 0x6D426547)
                throw new InvalidDataException("Invalid signature");
            if (reader.ReadByte() >> 4 != 4)
                throw new InvalidDataException("Unsupported version");
            var flags = reader.ReadFlags<GeBM_Flags>(1);
            var format = reader.ReadEnum<GePixelFormat>(1);
            if (format == GePixelFormat.Unknown)
                throw new InvalidDataException("Unsupported pixel format");
            var tmp = reader.ReadByte();
            var seekMipCount = (tmp >> 0) & 0xF;
            var maximumMip = (tmp >> 4) & 0xF;

            int width, height;
            if (flags.HasFlag(GeBM_Flags.WhAreLog2))
            {
                tmp = reader.ReadByte();
                width = 1 << (tmp >> 4);
                height = 1 << (tmp & 0xF);
            }
            else if (flags.HasFlag(GeBM_Flags.IfNotLog2AreByte))
            {
                width = (int)reader.ReadByte();
                height = (int)reader.ReadByte();
            }
            else
            {
                width = (int)reader.ReadUInt16();
                height = (int)reader.ReadUInt16();
            }

            Nullable<Color> colorKey = null;
            Nullable<byte> colorKeyIndex = null;
            if (flags.HasFlag(GeBM_Flags.HasColorKey))
            {
                if (format == GePixelFormat.Palette8)
                    colorKeyIndex = reader.ReadByte();
                else
                    colorKey = new PixelFormatInfo(format).ReadColor(reader);
            }

            Color[] palette = flags.HasFlag(GeBM_Flags.HasPalette)
                ? readPalette(reader)
                : null;

            if (colorKeyIndex != null)
                colorKey = palette[colorKeyIndex.Value];

            var mipmaps = new List<IBitmap>();
            while(true)
            {
                int level = (int)reader.ReadByte();
                if (level > maximumMip)
                    break;
                mipmaps.Add(readMipmap(reader, bitmapFactory, level, width, height, format, palette, colorKey));
            }

            if (flags.HasFlag(GeBM_Flags.HasAlpha))
                throw new InvalidDataException("Unsupported alpha bitmap");
            return mipmaps.ToArray();
        }
    }
}
