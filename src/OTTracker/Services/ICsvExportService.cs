using OTTracker.Models;

namespace OTTracker.Services;

public interface ICsvExportService
{
    Task<string> ExportAsync(IEnumerable<OtEntry> entries);
}
