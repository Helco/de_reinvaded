using System;
using System.Linq;
using System.IO;

namespace dere.io.prv {
    public static class EnumUtils
    {
        public static T intToEnum<T>(int i) where T : struct, IConvertible
        {
            if (Enum.IsDefined(typeof(T), i))
                return (T)Enum.Parse(typeof(T), i.ToString());
            else
                return (T)Enum.Parse(typeof(T), "Unknown");
        }

        public static T intToFlags<T>(uint value) where T : struct, IConvertible
        {
            string flagString = "";
            for (int bit = 0; bit < 32; bit++)
            {
                int intFlag = 1 << bit;
                if ((value & intFlag) > 0 && Enum.IsDefined(typeof(T), intFlag))
                    flagString += "," + Enum.Parse(typeof(T), intFlag.ToString()).ToString();
            }
            if (flagString.Length == 0)
                return default(T);
            else
                return (T)Enum.Parse(typeof(T), flagString.Substring(1));
        }

        private static uint readNByteInt(BinaryReader reader, int bytes)
        {
            uint value;
            switch(bytes) {
                case 1: value = (uint)reader.ReadByte(); break;
                case 2: value = (uint)reader.ReadUInt16(); break;
                case 4: value = reader.ReadUInt32(); break;
                default: throw new InvalidOperationException("Invalid enum size");
            }
            return value;
        }

        public static T ReadEnum<T>(this BinaryReader reader, int bytes=4) where T : struct, IConvertible
        {
            return intToEnum<T>((int)readNByteInt(reader, bytes));
        }

        public static T ReadFlags<T>(this BinaryReader reader, int bytes=4) where T : struct, IConvertible
        {
            return intToFlags<T>(readNByteInt(reader, bytes));
        }
    }
}
