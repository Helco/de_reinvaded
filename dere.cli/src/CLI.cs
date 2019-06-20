using System;
using System.Drawing.Imaging;
using System.IO;
using dere.io;

namespace dere.cli
{
    public class CLI
    {
        public static void Main(string[] args)
        {
            var mipmaps = GeBitmap.LoadFromStream(new FileStream(args[0], FileMode.Open, FileAccess.Read));
            Console.WriteLine("Loaded " + mipmaps.Length + " mipmaps");
            var baseName = Path.GetFileNameWithoutExtension(args[0]);
            for (int i = 0; i < mipmaps.Length; i++)
                mipmaps[i].Save(baseName + "_mip" + i + ".png", ImageFormat.Png);
        }
    }
}
