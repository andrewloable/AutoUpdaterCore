using LoableTech;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;

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
                        Console.WriteLine("-- Waiting for exit " + exeFile + " --");
                        proc.WaitForExit();
                        Console.WriteLine("-- Application closed " + exeFile + " --");
                    }
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine(e.StackTrace);
                }
            }
            Console.WriteLine("-- Waiting for process to close --");
            Thread.Sleep(5000);
            Console.WriteLine("-- Extrating Files --");
            // open zip file            
            using var zip = ZipStorer.Open(zipFile, FileAccess.Read);
            var dir = zip.Files.Values;
            foreach(var item in dir)
            {
                try
                {
                    if (item.FilenameInZip.ToLower().Contains("zipextractor", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var extractFile = Path.Combine(exePath, item.FilenameInZip);
                    if (File.Exists(extractFile))
                    {
                        Console.WriteLine("-- Deleting File " + extractFile);
                        File.Delete(extractFile);
                    }
                    Console.WriteLine("-- Extracting File " + extractFile);
                    zip.ExtractFile(item, extractFile);
                    Console.WriteLine("-- Done Extracting File " + extractFile);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    continue;
                }
            }
            zip.Close();
            // done extract, run exe file again
            var psi = new ProcessStartInfo();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.OSArchitecture == Architecture.X64)
            {
                psi.FileName = exeFile;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.OSArchitecture == Architecture.X86)
            {
                psi.FileName = exeFile;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.OSArchitecture == Architecture.Arm)
            {
                psi.FileName = exeFile;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.OSArchitecture == Architecture.X64)
            {
                psi.FileName = "/usr/bin/dotnet";
                psi.Arguments = exeFile.Replace(".exe", ".dll");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.OSArchitecture == Architecture.Arm)
            {
                psi.FileName = "/usr/bin/dotnet";
                psi.Arguments = exeFile.Replace(".exe", ".dll");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.OSArchitecture == Architecture.X64)
            {
                psi.FileName = "/usr/bin/dotnet";
                psi.Arguments = exeFile.Replace(".exe", ".dll");
            }
            Console.WriteLine("-- Running Application --");
            Console.WriteLine("Filename : " + psi.FileName);
            Console.WriteLine("Args : " + psi.Arguments);
            Process.Start(psi).WaitForExit();
            File.Delete(zipFile);
            Console.WriteLine("-- Update Done --");
            return;
        }
    }
}
