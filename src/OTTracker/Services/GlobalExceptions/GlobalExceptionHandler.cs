using System.Diagnostics;

namespace OTTracker.Services.GlobalExceptions;

public sealed class GlobalExceptionHandler(
    IExceptionLogger exceptionLogger,
    IUserExceptionNotifier userNotifier) : IDisposable
{
    private int _isRegistered;
    private int _isNotificationPending;

    public void Register()
    {
        if (Interlocked.Exchange(ref _isRegistered, 1) == 1)
        {
            return;
        }

        // Last-chance reporting for exceptions escaping a managed thread.
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

        // Observes faulted Tasks that were never awaited before they are finalized.
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

#if ANDROID
        // Captures exceptions crossing the managed/Android runtime boundary.
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += OnAndroidUnhandledException;
#endif
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isRegistered, 0) == 0)
        {
            return;
        }

        AppDomain.CurrentDomain.UnhandledException -= OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

#if ANDROID
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser -= OnAndroidUnhandledException;
#endif
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        var exception = args.ExceptionObject as Exception
            ?? new InvalidOperationException($"A non-Exception object was thrown: {args.ExceptionObject}");

        // AppDomain offers no SetHandled API. A terminating exception can only be logged.
        Handle(exception, nameof(AppDomain.CurrentDomain.UnhandledException), args.IsTerminating, !args.IsTerminating);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        Handle(args.Exception.Flatten(), nameof(TaskScheduler.UnobservedTaskException), false, true);

        // Prevents the runtime from applying escalation policy to this unobserved Task fault.
        args.SetObserved();
    }

#if ANDROID
    private void OnAndroidUnhandledException(
        object? sender,
        Android.Runtime.RaiseThrowableEventArgs args)
    {
        var isFatal = IsFatal(args.Exception);
        Handle(args.Exception, "AndroidEnvironment.UnhandledExceptionRaiser", isFatal, !isFatal);

        // Continuing after fatal runtime corruption is unsafe; allow Android to terminate then.
        args.Handled = !isFatal;
    }
#endif

    private void Handle(
        Exception exception,
        string source,
        bool isTerminating,
        bool canNotifyUser)
    {
        try
        {
            exceptionLogger.Log(exception, source, isTerminating);
        }
        catch (Exception loggingException)
        {
            // Exception handling must never throw another exception back into the runtime.
            Debug.WriteLine($"Global exception logging failed: {loggingException}");
            Debug.WriteLine(exception);
        }

        if (canNotifyUser && !IsFatal(exception))
        {
            QueueUserNotification();
        }
    }

    private void QueueUserNotification()
    {
        // Avoid stacking several alerts when one failure produces multiple global events.
        if (Interlocked.Exchange(ref _isNotificationPending, 1) == 1)
        {
            return;
        }

        _ = NotifyUserSafelyAsync();
    }

    private async Task NotifyUserSafelyAsync()
    {
        try
        {
            await userNotifier.ShowAsync();
        }
        catch (Exception notificationException)
        {
            try
            {
                exceptionLogger.Log(
                    notificationException,
                    "Global exception user notification",
                    false);
            }
            catch
            {
                Debug.WriteLine(notificationException);
            }
        }
        finally
        {
            Volatile.Write(ref _isNotificationPending, 0);
        }
    }

    private static bool IsFatal(Exception exception)
    {
        if (exception is OutOfMemoryException
            or StackOverflowException
            or AccessViolationException
            or BadImageFormatException)
        {
            return true;
        }

        if (exception is AggregateException aggregate)
        {
            return aggregate.Flatten().InnerExceptions.Any(IsFatal);
        }

        return exception.InnerException is not null && IsFatal(exception.InnerException);
    }
}
