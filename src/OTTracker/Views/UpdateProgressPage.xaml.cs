using System;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace OTTracker.Views;

public partial class UpdateProgressPage : ContentPage
{
    public UpdateProgressPage()
    {
        InitializeComponent();
    }

    public void UpdateProgress(double progressRatio, long bytesRead, long totalBytes)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            DownloadProgressBar.Progress = Math.Clamp(progressRatio, 0, 1);
            int percent = (int)(progressRatio * 100);
            ProgressPercentLabel.Text = $"{percent}%";

            double readMb = bytesRead / 1024.0 / 1024.0;
            if (totalBytes > 0)
            {
                double totalMb = totalBytes / 1024.0 / 1024.0;
                ProgressDetailLabel.Text = $"{readMb:F1} / {totalMb:F1} MB";
            }
            else
            {
                ProgressDetailLabel.Text = $"{readMb:F1} MB";
            }
        });
    }
}
