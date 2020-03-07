using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AutoUpdaterCore
{
    public class DownloadParameters
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
#nullable enable
        [JsonPropertyName("checksum")]
        public string? CheckSum { get; set; }
        [JsonPropertyName("hash")]
        public string? Hash { get; set; }
#nullable disable
    }
}
