using System.Threading.Tasks;

namespace OTTracker.Services;

public interface IUpdateService
{
    Task CheckAndPromptUpdateAsync(bool showNoUpdateAlert = false);
}
