using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OTTracker_Avalonia.AppServices.Interfaces.Services;

namespace OTTracker_Avalonia.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private const string PinFileName = "pin.dat";
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OTTracker",
        PinFileName
    );

    public Task<bool> HasPinAsync()
    {
        return Task.FromResult(File.Exists(FilePath));
    }

    public async Task SetPinAsync(string pin)
    {
        var hashStr = Hash(pin);
        var bytes = Encoding.UTF8.GetBytes(hashStr);
        
        // Encrypt bytes via OS User Profile (DPAPI)
        byte[] encryptedBytes;
        if (OperatingSystem.IsWindows())
        {
            encryptedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        }
        else
        {
            // For cross-platform fallback (e.g. Linux/Mac in debug) use plain or simple obfuscation
            encryptedBytes = bytes;
        }
        
        var dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) 
        {
            Directory.CreateDirectory(dir);
        }
        
        await File.WriteAllBytesAsync(FilePath, encryptedBytes);
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        if (!File.Exists(FilePath)) return false;

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
            
            var savedHash = Encoding.UTF8.GetString(decryptedBytes);
            return savedHash == Hash(pin);
        }
        catch
        {
            return false;
        }
    }

    public Task ClearPinAsync()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
        return Task.CompletedTask;
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
