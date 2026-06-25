using System.Collections.Generic;
using System.Threading.Tasks;
using OTTracker.Domain.Entities;

namespace OTTracker.Domain.Interfaces;

public interface ICsvExportService
{
    Task<bool> ExportAsync(IEnumerable<OtEntry> entries);

    Task<IReadOnlyList<OtEntry>?> ImportAsync();
}
