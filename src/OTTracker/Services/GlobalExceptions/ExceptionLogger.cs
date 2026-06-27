using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace OTTracker.Services.GlobalExceptions;

public sealed class ExceptionLogger(ILogger<ExceptionLogger> logger) : IExceptionLogger
{
    private const long MaxLogFileBytes = 1024 * 1024;
    private static readonly object FileLock = new();

    public void Log(Exception exception, string source, bool isTerminating)
    {
        var details = exception.ToString();

        if (isTerminating)
        {
            logger.LogCritical(
                exception,
                "Unhandled exception from {Source}. The process may terminate. Complete exception: {ExceptionDetails}",
                source,
                details);
        }
        else
        {
            logger.LogError(
                exception,
                "Unhandled exception from {Source}. Complete exception: {ExceptionDetails}",
                source,
                details);
        }

        // This fallback remains useful when only the platform debug stream is available.
        Debug.WriteLine($"[{source}] {details}");
        WriteLocalLog(source, isTerminating, details);
    }

    private static void WriteLocalLog(string source, bool isTerminating, string details)
    {
        try
        {
            lock (FileLock)
            {
                var directory = FileSystem.AppDataDirectory;
                var path = Path.Combine(directory, "exceptions.log");
                RotateIfNeeded(path);

                File.AppendAllText(
                    path,
                    $"{DateTimeOffset.UtcNow:O} | {source} | Terminating={isTerminating}{Environment.NewLine}" +
                    $"{details}{Environment.NewLine}{Environment.NewLine}");
            }
        }
        catch (Exception fileException)
        {
            // A failing fallback logger must not recursively trigger global handling.
            Debug.WriteLine($"Local exception log write failed: {fileException}");
        }
    }

    private static void RotateIfNeeded(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length < MaxLogFileBytes)
        {
            return;
        }

        var previousPath = $"{path}.previous";
        File.Delete(previousPath);
        File.Move(path, previousPath);
    }
}
