using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OTTracker_Avalonia.AppServices.Interfaces.Services;

namespace OTTracker_Avalonia.Infrastructure.Services;

public sealed class SupabaseSessionService : ISupabaseSessionService
{
    private const string SessionFileName = "session.dat";
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OTTracker",
        SessionFileName
    );

    public Task SaveSessionAsync(string accessToken, string refreshToken)
    {
        var raw = $"{accessToken};{refreshToken}";
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

        return File.WriteAllBytesAsync(FilePath, encryptedBytes);
    }

    public async Task<(string AccessToken, string RefreshToken)?> LoadSessionAsync()
    {
        if (!File.Exists(FilePath))
        {
            return null;
        }

        try
        {
            var encryptedBytes = await File.ReadAllBytesAsync(FilePath);
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
                return (parts[0], parts[1]);
            }
        }
        catch
        {
            // Ignore exception and return null
        }

        return null;
    }

    public Task ClearSessionAsync()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
        return Task.CompletedTask;
    }
}
