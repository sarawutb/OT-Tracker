using System.Collections.Generic;
using System.Threading.Tasks;
using OTTracker_Avalonia.Domain.Entities;

namespace OTTracker_Avalonia.AppServices.Interfaces.Services;

public interface ICsvExportService
{
    Task<bool> ExportAsync(IEnumerable<OtEntry> entries);

    Task<IReadOnlyList<OtEntry>?> ImportAsync();
}
