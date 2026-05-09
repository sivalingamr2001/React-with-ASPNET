using Janatics.DataEngine.ProcessService.Infrastructure.Models;
using Janatics.DataEngine.ProcessService.Models.Metadata;

namespace Janatics.DataEngine.ProcessService.Abstractions;

public interface IProviderCapabilityRegistry
{
    ProviderCapabilities Get(DatabaseProvider provider);
}
