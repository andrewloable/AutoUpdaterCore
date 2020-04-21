using AutoUpdaterCore;
using Mono.Unix;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace LoableTech
{
    public class AutoUpdater
    {
        public delegate void UpdateDownloadedEventHandler(UpdateDownloadedEventArgs args);
        public delegate void UpdateErrorEventHandler(UpdateErrorEventArgs args);
        public delegate void UpdateDownloadingEventHandler(UpdateDownloadedEventArgs args);
        public static event UpdateDownloadedEventHandler UpdateDownloadedEvent;
        public static event UpdateErrorEventHandler UpdateErrorEvent;
        public static event UpdateDownloadingEventHandler UpdateDownloadingEvent;
        private static string _tempFile;
        private static DownloadParameters _param;
        private static AppWebClient _webClient;
        public static async Task StartAsync(string url, IDictionary<string, string> headers = null)
        {
            var assembly =  Assembly.GetEntryAssembly();
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            if (headers != null && headers.Count > 0)
            {
                foreach (var h in headers)
                    client.DefaultRequestHeaders.Add(h.Key, h.Value);
            }            
            try
            {
                // Get Update Info
                var resp = await client.GetAsync(url);
                resp.EnsureSuccessStatusCode();
                var json = await resp.Content.ReadAsStringAsync();
                _param = JsonSerializer.Deserialize<DownloadParameters>(json);
                var currentVersion = new Version(_param.Version);
                if (currentVersion <= assembly.GetName().Version)
                {
                    UpdateDownloadedEvent?.Invoke(new UpdateDownloadedEventArgs { IsDownloaded = false });
                    return;
                }
                // Start Download
                _tempFile = Path.GetTempFileName();
                _webClient = new AppWebClient();
                _webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                UpdateDownloadingEvent?.Invoke(new UpdateDownloadedEventArgs { IsDownloaded = false });
                await _webClient.DownloadFileTaskAsync(new Uri(_param.Url), _tempFile);
            }
            catch (Exception e)
            {
                UpdateErrorEvent?.Invoke(new UpdateErrorEventArgs { Message = e.Message });
            }
        }

        private static void WebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Console.WriteLine("-- Download Done --");
            if (!string.IsNullOrWhiteSpace(_param.CheckSum) && !IsFileChecksumOK(_tempFile, _param))
            {
                Console.WriteLine("-- Checksum Error --");
                UpdateErrorEvent?.Invoke(new UpdateErrorEventArgs { Message = "CheckSum Error" });
                return;
            }
            var cd = _webClient.ResponseHeaders["Content-Disposition"] != null ? new ContentDisposition(_webClient.ResponseHeaders["Content-Disposition"]) : null;
            var filename = string.IsNullOrEmpty(cd?.FileName) ? Path.GetFileName(_webClient.ResponseUri.LocalPath) : cd.FileName;
            var tempPath = Path.Combine(Path.GetTempPath(), filename);
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            File.Move(_tempFile, tempPath);
            Console.WriteLine("-- File Moved To : " + tempPath + " --");
            var ext = Path.GetExtension(tempPath);
            if (ext.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var zipExtractorPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ZipExtractor");
                var zipExtractor = "";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.OSArchitecture == Architecture.X64)
                {
                    zipExtractor = Path.Combine(zipExtractorPath, "win64", "ZipExtractorCore.exe");
                    Console.WriteLine("ZipExtractor - " + zipExtractor);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.OSArchitecture == Architecture.X86)
                {
                    zipExtractor = Path.Combine(zipExtractorPath, "win32", "ZipExtractorCore.exe");
                    Console.WriteLine("ZipExtractor - " + zipExtractor);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.OSArchitecture == Architecture.Arm)
                {
                    zipExtractor = Path.Combine(zipExtractorPath, "win-arm", "ZipExtractorCore.exe");
                    Console.WriteLine("ZipExtractor - " + zipExtractor);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.OSArchitecture == Architecture.X64)
                {
                    zipExtractor = Path.Combine(zipExtractorPath, "linux64", "ZipExtractorCore");
                    Console.WriteLine("ZipExtractor - " + zipExtractor);
                    var unixFileInfo = new UnixFileInfo(zipExtractor);
                    unixFileInfo.FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.OSArchitecture == Architecture.Arm)
                {
                    zipExtractor = Path.Combine(zipExtractorPath, "linux-arm", "ZipExtractorCore");
                    Console.WriteLine("ZipExtractor - " + zipExtractor);
                    var unixFileInfo = new UnixFileInfo(zipExtractor);
                    unixFileInfo.FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.OSArchitecture == Architecture.X64)
                {
                    zipExtractor = Path.Combine(zipExtractorPath, "osx64", "ZipExtractorCore");
                    Console.WriteLine("ZipExtractor - " + zipExtractor);
                    var unixFileInfo = new UnixFileInfo(zipExtractor);
                    unixFileInfo.FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute;
                }
                // dotnet core will use dll, rename to exe
                var exeFile = Assembly.GetEntryAssembly().Location.Replace(".dll", ".exe", StringComparison.OrdinalIgnoreCase);
                var exePath = Path.GetDirectoryName(exeFile);
                var args = $"-exeFile=\"{exeFile}\" -exePath=\"{exePath}\" -zipFile=\"{tempPath}\"";
                var psi = new ProcessStartInfo
                {
                    FileName = zipExtractor,
                    UseShellExecute = true,
                    Arguments = args
                };
                Process.Start(psi);
                UpdateDownloadedEvent?.Invoke(new UpdateDownloadedEventArgs { IsDownloaded = true });
                return;
            }
            UpdateErrorEvent?.Invoke(new UpdateErrorEventArgs { Message = "Only Zip Files Allowed" });
            return;
        }
        private static bool IsFileChecksumOK(string filename, DownloadParameters param)
        {
            using var hashAlgo = HashAlgorithm.Create(param.Hash ?? "SHA512");
            using var stream = File.OpenRead(filename);
            var checksum = BitConverter.ToString(hashAlgo.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            if (param.CheckSum.Equals(checksum, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }
    }
}
