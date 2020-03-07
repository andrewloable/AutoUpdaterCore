using LoableTech;
using System;
using System.IO;
using System.Security.Cryptography;

namespace GetFileChecksum
{
    class Program
    {
        static void Main(string[] args)
        {
            FlagParser.Parse(args);
            var file = FlagParser.StringFlag("file", string.Empty, true);
            var hash = FlagParser.StringFlag("hash", string.Empty, true);
            if (string.IsNullOrWhiteSpace(file) || string.IsNullOrWhiteSpace(hash))
            {
                Console.WriteLine("Invalid Parameters");
                return;
            }
            using var hashAlgo = HashAlgorithm.Create(hash ?? "SHA512");
            using var stream = File.OpenRead(file);
            var checksum = BitConverter.ToString(hashAlgo.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            Console.WriteLine("--- File Checksum ---");
            Console.WriteLine(checksum);
        }
    }
}
