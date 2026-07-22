using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using OTTracker.Views;

#if ANDROID
using Android.Content;
using AndroidX.Core.Content;
#endif

namespace OTTracker.Services;

public class UpdateService : IUpdateService
{
    private const string UpdateJsonUrl = "https://raw.githubusercontent.com/sarawutb/OT-Tracker/main/update.json";
    private readonly HttpClient _httpClient;

    public UpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "OTTracker-App");
    }

    public async Task CheckAndPromptUpdateAsync(bool showNoUpdateAlert = false)
    {
        try
        {
            var updateInfo = await _httpClient.GetFromJsonAsync<UpdateInfo>(UpdateJsonUrl);
            if (updateInfo == null) return;

            int currentVersionCode = 0;
            if (int.TryParse(AppInfo.Current.BuildString, out int code))
            {
                currentVersionCode = code;
            }

            if (updateInfo.VersionCode > currentVersionCode)
            {
                Page? mainPage = Shell.Current ?? Application.Current?.Windows.FirstOrDefault()?.Page;
                if (mainPage == null) return;

                string message = $"พบเวอร์ชันใหม่ v{updateInfo.VersionName}\n\nรายละเอียดการอัปเดต:\n{updateInfo.ChangeLog}\n\nคุณต้องการอัปเดตตอนนี้หรือไม่?";
                bool confirm = await mainPage.DisplayAlert("อัปเดตแอปพลิเคชัน", message, "อัปเดตเลย", "ไว้ทีหลัง");

                if (confirm)
                {
                    await DownloadAndInstallApkAsync(updateInfo.ApkUrl, mainPage);
                }
            }
            else if (showNoUpdateAlert)
            {
                Page? mainPage = Shell.Current ?? Application.Current?.Windows.FirstOrDefault()?.Page;
                if (mainPage != null)
                {
                    await mainPage.DisplayAlert("การอัปเดต", "คุณกำลังใช้งานเวอร์ชันล่าสุดแล้ว", "ตกลง");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateService Error]: {ex.Message}");
        }
    }

    private async Task DownloadAndInstallApkAsync(string apkUrl, Page page)
    {
        UpdateProgressPage? progressPage = null;
        INavigation? navigation = page.Navigation ?? Shell.Current?.Navigation;

        try
        {
            progressPage = new UpdateProgressPage();
            if (navigation != null)
            {
                await navigation.PushModalAsync(progressPage, false);
            }

            string fileName = "app-update.apk";
            string filePath = Path.Combine(FileSystem.Current.CacheDirectory, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (var response = await _httpClient.GetAsync(apkUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                byte[] buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        double ratio = (double)totalRead / totalBytes;
                        progressPage?.UpdateProgress(ratio, totalRead, totalBytes);
                    }
                    else
                    {
                        progressPage?.UpdateProgress(0, totalRead, -1);
                    }
                }
            }

            if (navigation != null && progressPage != null)
            {
                await navigation.PopModalAsync(false);
            }

#if ANDROID
            InstallApkAndroid(filePath);
#else
            await page.DisplayAlert("แจ้งเตือน", "ระบบอัปเดตอัตโนมัติรองรับเฉพาะระบบปฏิบัติการ Android", "ตกลง");
#endif
        }
        catch (Exception ex)
        {
            if (navigation != null && progressPage != null)
            {
                try
                {
                    await navigation.PopModalAsync(false);
                }
                catch { }
            }
            await page.DisplayAlert("เกิดข้อผิดพลาด", $"ไม่สามารถดาวน์โหลดไฟล์อัปเดตได้: {ex.Message}", "ตกลง");
        }
    }

#if ANDROID
    private void InstallApkAndroid(string filePath)
    {
        var context = Android.App.Application.Context;
        var file = new Java.IO.File(filePath);

        if (!file.Exists()) return;

        Android.Net.Uri apkUri = AndroidX.Core.Content.FileProvider.GetUriForFile(
            context,
            $"{context.PackageName}.fileprovider",
            file);

        var intent = new Intent(Intent.ActionView);
        intent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
        intent.SetFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);

        context.StartActivity(intent);
    }
#endif
}
