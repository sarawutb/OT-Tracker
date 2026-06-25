using System.Threading.Tasks;
using OTTracker_Avalonia.Domain.Entities;

namespace OTTracker_Avalonia.AppServices.Interfaces.Services;

public interface ISettingsService
{
    Task<AppSettings> GetAsync();

    Task SaveAsync(AppSettings settings);
}
