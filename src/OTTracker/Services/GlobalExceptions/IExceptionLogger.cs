namespace OTTracker.Services.GlobalExceptions;

public interface IExceptionLogger
{
    void Log(Exception exception, string source, bool isTerminating);
}
