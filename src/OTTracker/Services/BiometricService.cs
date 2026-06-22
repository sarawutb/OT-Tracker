#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Biometrics;
using Android.Hardware.Fingerprints;
using Android.OS;
using Java.Lang;
using Java.Util.Concurrent;
#elif IOS
using Foundation;
using LocalAuthentication;
#endif

namespace OTTracker.Services;

public sealed class BiometricService : IBiometricService
{
    public string? LastError { get; private set; }

    public Task<bool> IsAvailableAsync()
    {
        LastError = null;

#if ANDROID
        var available = AndroidBiometricAuthenticator.IsAvailable(out var error);
        LastError = error;
        return Task.FromResult(available);
#elif IOS
        var available = IosBiometricAuthenticator.IsAvailable(out var error);
        LastError = error;
        return Task.FromResult(available);
#else
        LastError = "Fingerprint unlock is available only on Android and iOS.";
        return Task.FromResult(false);
#endif
    }

    public async Task<bool> AuthenticateAsync()
    {
        LastError = null;

#if ANDROID
        return await AndroidBiometricAuthenticator.AuthenticateAsync(SetLastError);
#elif IOS
        return await IosBiometricAuthenticator.AuthenticateAsync(SetLastError);
#else
        LastError = "Fingerprint unlock is available only on Android and iOS.";
        return false;
#endif
    }

    private void SetLastError(string? error)
    {
        LastError = error;
    }
}

#if ANDROID
internal static class AndroidBiometricAuthenticator
{
    public static bool IsAvailable(out string? error)
    {
        error = null;
        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            error = "Fingerprint unlock is not ready yet. Use your PIN.";
            return false;
        }

        if (Build.VERSION.SdkInt < BuildVersionCodes.P)
        {
            error = "Fingerprint unlock requires Android 9 or newer.";
            return false;
        }

        var packageManager = activity.PackageManager;
        if (packageManager is null || !packageManager.HasSystemFeature(PackageManager.FeatureFingerprint))
        {
            error = "This device does not support fingerprint unlock.";
            return false;
        }

#pragma warning disable CA1416
        var fingerprintManager = activity.GetSystemService(Context.FingerprintService) as FingerprintManager;
        if (fingerprintManager is null || !fingerprintManager.IsHardwareDetected)
        {
            error = "This device does not support fingerprint unlock.";
            return false;
        }

        if (!fingerprintManager.HasEnrolledFingerprints)
        {
            error = "Add a fingerprint in your phone settings before using fingerprint unlock.";
            return false;
        }
#pragma warning restore CA1416

        return true;
    }

    public static Task<bool> AuthenticateAsync(Action<string?> setLastError)
    {
        if (!IsAvailable(out var availabilityError))
        {
            setLastError(availabilityError);
            return Task.FromResult(false);
        }

        var activity = Platform.CurrentActivity!;
        var completion = new TaskCompletionSource<bool>();
        var executor = Executors.NewSingleThreadExecutor()!;
        var cancellation = new CancellationSignal();
        var callback = new AuthenticationCallback(completion, setLastError);
        var cancelListener = new NegativeButtonClickListener(cancellation, completion, setLastError);

        var prompt = new BiometricPrompt.Builder(activity)
            .SetTitle("Unlock OT Tracker")
            .SetSubtitle("Use your fingerprint to unlock the app")
            .SetNegativeButton("Use PIN", executor, cancelListener)
            .Build();

        prompt.Authenticate(cancellation, executor, callback);
        return completion.Task;
    }

    private sealed class AuthenticationCallback(TaskCompletionSource<bool> completion, Action<string?> setLastError)
        : BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult? result)
        {
            setLastError(null);
            completion.TrySetResult(true);
        }

        public override void OnAuthenticationFailed()
        {
            setLastError("Fingerprint was not recognized. Use your PIN or try again.");
        }

        public override void OnAuthenticationError(BiometricErrorCode errorCode, ICharSequence? errString)
        {
            setLastError(errString?.ToString() ?? "Fingerprint unlock was cancelled. Use your PIN.");
            completion.TrySetResult(false);
        }
    }

    private sealed class NegativeButtonClickListener(CancellationSignal cancellation, TaskCompletionSource<bool> completion, Action<string?> setLastError)
        : Java.Lang.Object, IDialogInterfaceOnClickListener
    {
        public void OnClick(IDialogInterface? dialog, int which)
        {
            cancellation.Cancel();
            setLastError("Fingerprint unlock was cancelled. Use your PIN.");
            completion.TrySetResult(false);
        }
    }
}
#endif

#if IOS
internal static class IosBiometricAuthenticator
{
    public static bool IsAvailable(out string? error)
    {
        using var context = new LAContext();
        var available = context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out var nsError);
        error = available ? null : ToMessage(nsError);
        return available;
    }

    public static async Task<bool> AuthenticateAsync(Action<string?> setLastError)
    {
        using var context = new LAContext
        {
            LocalizedFallbackTitle = "Use PIN"
        };

        if (!context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out var nsError))
        {
            setLastError(ToMessage(nsError));
            return false;
        }

        var result = await context.EvaluatePolicyAsync(
            LAPolicy.DeviceOwnerAuthenticationWithBiometrics,
            "Unlock OT Tracker");

        if (result.Item1)
        {
            setLastError(null);
            return true;
        }

        setLastError(ToMessage(result.Item2));
        return false;
    }

    private static string ToMessage(NSError? error)
    {
        return error?.LocalizedDescription ?? "Fingerprint unlock is not available. Use your PIN.";
    }
}
#endif


