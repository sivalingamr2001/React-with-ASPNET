namespace Janatics.DataEngine
{
    using System.Collections.Generic;
    using Janatics.DataEngine.Models;

    public interface IDataEngineService
    {
        IEnumerable<Profile> GetProfiles();
        IEnumerable<EntityConfig> GetEntities(string profileId);
        IEnumerable<ColumnConfig> GetColumns(string entityId);
        IEnumerable<string> DiscoverTables(string profileId);
    }
}
