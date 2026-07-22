using System.Text.Json.Serialization;

namespace OTTracker.Services;

public class UpdateInfo
{
    [JsonPropertyName("versionCode")]
    public int VersionCode { get; set; }

    [JsonPropertyName("versionName")]
    public string VersionName { get; set; } = string.Empty;

    [JsonPropertyName("apkUrl")]
    public string ApkUrl { get; set; } = string.Empty;

    [JsonPropertyName("changeLog")]
    public string ChangeLog { get; set; } = string.Empty;
}
