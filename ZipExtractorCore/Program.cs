using LoableTech;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace ZipExtractorCore
{
    class Program
    {
        static void Main(string[] args)
        {
            FlagParser.Parse(args);
            var exeFile = FlagParser.StringFlag("exeFile", string.Empty, true);
            var exePath = FlagParser.StringFlag("exePath", string.Empty, true);
            var zipFile = FlagParser.StringFlag("zipFile", string.Empty, true);

            if (string.IsNullOrWhiteSpace(exeFile) || string.IsNullOrWhiteSpace(exePath) || string.IsNullOrWhiteSpace(zipFile))
            {
                Console.WriteLine("Invalid Parameters");
                return;
            }
            // make sure exe file to update is not running
            foreach(var proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.MainModule.FileName.Equals(exeFile, StringComparison.OrdinalIgnoreCase))
                    {
                        proc.WaitForExit();
                    }
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine(e.StackTrace);
                }
            }
            // open zip file            
            using var zip = ZipStorer.Open(zipFile, FileAccess.Read);
            var dir = zip.Files.Values;
            foreach(var item in dir)
            {
                var extractFile = Path.Combine(exePath, item.FilenameInZip);
                if (File.Exists(extractFile))
                    File.Delete(extractFile);
                zip.ExtractFile(item, extractFile);
            }
            zip.Close();
            // done extract, run exe file again
            var psi = new ProcessStartInfo(exeFile);
            Process.Start(psi).WaitForExit();
            File.Delete(zipFile);
            return;
        }
    }
}
