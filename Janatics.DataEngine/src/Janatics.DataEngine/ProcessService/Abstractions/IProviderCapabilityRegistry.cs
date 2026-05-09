using Janatics.DataEngine.Infrastructure.Models;
using Janatics.DataEngine.Models.Metadata;

namespace Janatics.DataEngine.Abstractions;

public interface IProviderCapabilityRegistry
{
    ProviderCapabilities Get(DatabaseProvider provider);
}
