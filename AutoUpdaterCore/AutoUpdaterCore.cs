using AutoUpdaterCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace LoableTech
{
    public class AutoUpdater
    {
        public delegate void UpdateDownloadedEventHandler(UpdateDownloadedEventArgs args);
        public delegate void UpdateErrorEventHandler(UpdateErrorEventArgs args);
        public static event UpdateDownloadedEventHandler UpdateDownloadedEvent;
        public static event UpdateErrorEventHandler UpdateErrorEvent;
        private static string _tempFile;
        private static DownloadParameters _param;
        private static AppWebClient _webClient;
        public static async Task StartAsync(string url, IDictionary<string, string> headers = null)
        {
            var assembly =  Assembly.GetEntryAssembly();
            var client = new HttpClient();
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
                await _webClient.DownloadFileTaskAsync(new Uri(_param.Url), _tempFile);
            }
            catch (Exception e)
            {
                UpdateErrorEvent?.Invoke(new UpdateErrorEventArgs { Message = e.Message });
            }
        }
        private static void WebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_param.CheckSum) && !IsFileChecksumOK(_tempFile, _param))
            {
                UpdateErrorEvent?.Invoke(new UpdateErrorEventArgs { Message = "CheckSum Error" });
                return;
            }
            var cd = _webClient.ResponseHeaders["Content-Disposition"] != null ? new ContentDisposition(_webClient.ResponseHeaders["Content-Disposition"]) : null;
            var filename = string.IsNullOrEmpty(cd?.FileName) ? Path.GetFileName(_webClient.ResponseUri.LocalPath) : cd.FileName;
            var tempPath = Path.Combine(Path.GetTempPath(), filename);
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            File.Move(_tempFile, tempPath);
            var ext = Path.GetExtension(tempPath);
            if (ext.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var zipExtractor = Path.Combine("ZipExtractor", "ZipExtractorCore.exe");
                // dotnet core will use dll, rename to exe
                var exeFile = Process.GetCurrentProcess().MainModule.FileName.Replace(".dll", ".exe", StringComparison.OrdinalIgnoreCase);
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
