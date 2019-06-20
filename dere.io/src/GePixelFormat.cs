using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;

namespace dere.io.prv
{
    public enum GePixelFormat
    {
        Palette8 = 1,
        Gray8,

        Rgb16_555,
        Bgr16_555,
        Rgb16_565,
        Bgr16_565,
        Argb16_4444,
        Argb16_1555,
        Rgb24,
        Bgr24,

        Rgbx32 = 12,
        Xrgb32,
        Bgrx32,
        Xbgr32,
        Rgba32,
        Argb32,
        Bgra32,
        Abgr32,

        Unknown = -1
    }

    public struct PixelFormatInfo
    {
        public PixelFormatInfo(GePixelFormat format)
        {
            if (byEnum.ContainsKey(format))
                this = byEnum[format];
            else if (format == GePixelFormat.Gray8) {
                bytesPerPixel = 1;
                rShift = gShift = bShift = aShift = 0;
                rBits = gBits = bBits = 8;
                aBits = 0;
            }
            else
                throw new InvalidOperationException("This format has no RGB bit representation");
        }

        private PixelFormatInfo(string order, int rBits, int gBits, int bBits, int aBits)
        {
            if (order.Length != 4)
                throw new InvalidOperationException("Invalid pixelformatinfo order");

            this.rBits = rBits;
            this.gBits = gBits;
            this.bBits = bBits;
            this.aBits = aBits;
            bytesPerPixel = (rBits + gBits + bBits + aBits + 7) / 8;
            rShift = gShift = bShift = aShift = -1;

            int off = 0;
            for (int i = 3; i >= 0; i--) // big endian colors...
            {
                switch(order[i])
                {
                    case 'r': rShift = off; off += rBits; break;
                    case 'g': gShift = off; off += gBits; break;
                    case 'b': bShift = off; off += bBits; break;
                    case 'a': aShift = off; off += aBits; break;
                    case 'x': off += aBits; this.aBits = 0; break;
                    default: throw new InvalidOperationException("Invalid pixelformatinfo order");
                }
            }
            if (rShift < 0 || gShift < 0 || bShift < 0)
                throw new InvalidOperationException("Invalid pixelformatinfo order");
        }

        private static IReadOnlyDictionary<GePixelFormat, PixelFormatInfo> byEnum = new Dictionary<GePixelFormat, PixelFormatInfo>()
        {
            { GePixelFormat.Rgb16_555,      new PixelFormatInfo("rgbx", 5, 5, 5, 0) },
            { GePixelFormat.Bgr16_555,      new PixelFormatInfo("bgrx", 5, 5, 5, 0) },
            { GePixelFormat.Rgb16_565,      new PixelFormatInfo("rgbx", 5, 6, 5, 0) },
            { GePixelFormat.Bgr16_565,      new PixelFormatInfo("bgrx", 5, 6, 5, 0) },
            { GePixelFormat.Argb16_4444,    new PixelFormatInfo("argb", 4, 4, 4, 4) },
            { GePixelFormat.Argb16_1555,    new PixelFormatInfo("argb", 5, 5, 5, 1) },
            { GePixelFormat.Rgb24,          new PixelFormatInfo("rgbx", 8, 8, 8, 0) },
            { GePixelFormat.Bgr24,          new PixelFormatInfo("bgrx", 8, 8, 8, 0) },
            { GePixelFormat.Rgbx32,         new PixelFormatInfo("rgbx", 8, 8, 8, 8) },
            { GePixelFormat.Xrgb32,         new PixelFormatInfo("xrgb", 8, 8, 8, 8) },
            { GePixelFormat.Xbgr32,         new PixelFormatInfo("xbgr", 8, 8, 8, 8) },
            { GePixelFormat.Rgba32,         new PixelFormatInfo("rgba", 8, 8, 8, 8) },
            { GePixelFormat.Argb32,         new PixelFormatInfo("argb", 8, 8, 8, 8) },
            { GePixelFormat.Bgra32,         new PixelFormatInfo("bgra", 8, 8, 8, 8) },
            { GePixelFormat.Abgr32,         new PixelFormatInfo("abgr", 8, 8, 8, 8) }
        };

        public int bytesPerPixel;
        public int rShift, rBits;
        public int gShift, gBits;
        public int bShift, bBits;
        public int aShift, aBits;

        private static byte getExtendedValue(UInt32 value, int shift, int bits)
        {
            int mask = (1 << bits) - 1;
            long raw = (value >> shift) & mask;
            return (byte)(raw * 255 / mask);
        }

        public Color ReadColor(BinaryReader reader)
        {
            byte[] buffer = new byte[4] { 0, 0, 0, 0 };
            reader.BaseStream.Read(buffer, 0, bytesPerPixel);
            UInt32 value = BitConverter.ToUInt32(buffer, 0);
            return Color.FromArgb(
                aBits == 0 ? 255 : getExtendedValue(value, aShift, aBits),
                getExtendedValue(value, rShift, rBits),
                getExtendedValue(value, gShift, gBits),
                getExtendedValue(value, bShift, bBits)
            );
        }
    }
}
