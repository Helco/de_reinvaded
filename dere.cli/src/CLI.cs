using System;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using dere.io;

namespace dere.cli
{
    public class CLI
    {
        public static void convertBitmap(string[] args)
        {
            var mipmaps = GeBitmap.LoadFromStream(new FileStream(args[1], FileMode.Open, FileAccess.Read));
            Console.WriteLine("Loaded " + mipmaps.Length + " mipmaps");
            var baseName = Path.GetFileNameWithoutExtension(args[1]);
            for (int i = 0; i < mipmaps.Length; i++)
                mipmaps[i].Save(baseName + "_mip" + i + ".png", ImageFormat.Png);
        }

        public static void extractVFile(string[] args)
        {
            var vfile = VFile.LoadFromStream(new FileStream(args[1], FileMode.Open, FileAccess.Read));
            Console.WriteLine("Extract " + vfile.FileNames.Count + " files");
            foreach (var filename in vfile.FileNames)
            {
                var dir = Path.GetDirectoryName(filename);
                if (dir.Length > 0)
                    Directory.CreateDirectory(dir);

                var inStream = vfile.OpenFile(filename);
                var outStream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
                inStream.CopyTo(outStream);
                outStream.Flush();
                outStream.Close();
            }
        }

        public static void help(string[] args = null)
        {
            Console.WriteLine("usage: dere.cli <verb> [<arg>...]");
            Console.WriteLine("verbs:");
            foreach (var verb in verbs)
                Console.WriteLine("  " + verb.Key);
        }

        public static IReadOnlyDictionary<string, Action<string[]>> verbs = new Dictionary<string, Action<string[]>>
        {
            { "convertBitmap", convertBitmap },
            { "extractVFile", extractVFile },
            { "help", help }
        };

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                help();
            }
            else if (!verbs.ContainsKey(args[0]))
            {
                Console.WriteLine("Invalid verb " + args[0]);
                help();
                return;
            }
            else
                verbs[args[0]](args);
        }
    }
}
