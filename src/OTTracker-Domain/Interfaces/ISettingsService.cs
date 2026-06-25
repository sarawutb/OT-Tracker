using System.Threading.Tasks;
using OTTracker.Domain.Entities;

namespace OTTracker.Domain.Interfaces;

public interface ISettingsService
{
    Task<AppSettings> GetAsync();

    Task SaveAsync(AppSettings settings);
}
