using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OTTracker.Domain.Interfaces;

namespace OTTracker.Infrastructure.Services;

public sealed class SupabaseConfigService : ISupabaseConfigService
{
    private const string DbConfigFileName = "db.dat";
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OTTracker",
        DbConfigFileName
    );

    private (string Url, string AnonKey)? _cachedCredentials;

    public (string Url, string AnonKey) GetCredentials()
    {
        if (_cachedCredentials != null)
        {
            return _cachedCredentials.Value;
        }

        if (!File.Exists(FilePath))
        {
            _cachedCredentials = GetDefaultCredentials();
            return _cachedCredentials.Value;
        }

        try
        {
            var encryptedBytes = File.ReadAllBytes(FilePath);
            byte[] decryptedBytes;
            if (OperatingSystem.IsWindows())
            {
                decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            }
            else
            {
                decryptedBytes = encryptedBytes;
            }
            var raw = Encoding.UTF8.GetString(decryptedBytes);
            var parts = raw.Split(';', 2);
            if (parts.Length == 2)
            {
                _cachedCredentials = (parts[0], parts[1]);
            }
            else
            {
                _cachedCredentials = GetDefaultCredentials();
            }
        }
        catch
        {
            _cachedCredentials = GetDefaultCredentials();
        }

        return _cachedCredentials.Value;
    }

    private (string Url, string AnonKey) GetDefaultCredentials()
    {
        try
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(configPath))
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                var config = builder.Build();
                var url = config["Supabase:Url"];
                var key = config["Supabase:AnonKey"];
                if (!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(key))
                {
                    return (url, key);
                }
            }
        }
        catch
        {
            // Ignore config read exceptions and fall back
        }
        return (string.Empty, string.Empty);
    }

    public async Task SaveCredentialsAsync(string url, string anonKey)
    {
        _cachedCredentials = (url, anonKey);
        var raw = $"{url};{anonKey}";
        var bytes = Encoding.UTF8.GetBytes(raw);
        byte[] encryptedBytes;
        if (OperatingSystem.IsWindows())
        {
            encryptedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        }
        else
        {
            encryptedBytes = bytes;
        }

        var dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllBytesAsync(FilePath, encryptedBytes);
    }
}
