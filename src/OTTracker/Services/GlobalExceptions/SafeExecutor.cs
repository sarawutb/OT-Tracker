using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OTTracker.Services.GlobalExceptions;

public static class SafeExecutor
{
    public static async void SafeFireAndForget(this Task task, Action<Exception>? onException = null)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SafeFireAndForget caught exception: {ex}");
            if (onException is not null)
            {
                try
                {
                    onException(ex);
                }
                catch (Exception handlerEx)
                {
                    Debug.WriteLine($"SafeFireAndForget exception handler threw exception: {handlerEx}");
                }
            }
        }
    }
}
