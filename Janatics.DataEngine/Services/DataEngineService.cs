namespace Janatics.DataEngine.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Janatics.DataEngine.Models;

    public class DataEngineService : Janatics.DataEngine.IDataEngineService
    {
        private readonly List<Profile> _profiles = new();
        private readonly Dictionary<string, List<EntityConfig>> _entities = new();
        private readonly Dictionary<string, List<ColumnConfig>> _columns = new();

        public DataEngineService()
        {
            var p1 = new Profile
            {
                Id = "p1",
                Name = "Local DB",
                Provider = "SqlServer",
                ConnectionString = "Server=(local);Database=Sample",
                Status = "connected",
                LastTested = "just now"
            };

            var p2 = new Profile
            {
                Id = "p2",
                Name = "Remote DB",
                Provider = "Postgres",
                ConnectionString = "Host=example;Database=Demo",
                Status = "disconnected",
                LastTested = "never"
            };

            _profiles.Add(p1);
            _profiles.Add(p2);

            _entities[p1.Id] = new List<EntityConfig>
            {
                new EntityConfig { Id = "e1", Name = "Users", Schema = "dbo", PkColumn = "Id", IsReadOnly = false, Roles = new List<string>{"admin"}, ColumnCount = 3 },
                new EntityConfig { Id = "e2", Name = "Products", Schema = "dbo", PkColumn = "Id", IsReadOnly = false, Roles = new List<string>{"admin","user"}, ColumnCount = 5 }
            };

            _entities[p2.Id] = new List<EntityConfig>
            {
                new EntityConfig { Id = "e3", Name = "Orders", Schema = "public", PkColumn = "OrderId", IsReadOnly = false, Roles = new List<string>{"admin"}, ColumnCount = 6 }
            };

            _columns["e1"] = new List<ColumnConfig>
            {
                new ColumnConfig { Id = "c1", Name = "Id", Type = "int", IsPK = true },
                new ColumnConfig { Id = "c2", Name = "Name", Type = "varchar(100)" },
                new ColumnConfig { Id = "c3", Name = "Email", Type = "varchar(200)" }
            };

            _columns["e2"] = new List<ColumnConfig>
            {
                new ColumnConfig { Id = "c4", Name = "Id", Type = "int", IsPK = true },
                new ColumnConfig { Id = "c5", Name = "Title", Type = "varchar(200)" },
                new ColumnConfig { Id = "c6", Name = "Price", Type = "decimal" }
            };

            _columns["e3"] = new List<ColumnConfig>
            {
                new ColumnConfig { Id = "c7", Name = "OrderId", Type = "int", IsPK = true },
                new ColumnConfig { Id = "c8", Name = "Amount", Type = "decimal" }
            };
        }

        public IEnumerable<Profile> GetProfiles() => _profiles;

        public IEnumerable<EntityConfig> GetEntities(string profileId)
        {
            return _entities.TryGetValue(profileId, out var list) ? list : Enumerable.Empty<EntityConfig>();
        }

        public IEnumerable<ColumnConfig> GetColumns(string entityId)
        {
            return _columns.TryGetValue(entityId, out var list) ? list : Enumerable.Empty<ColumnConfig>();
        }

        public IEnumerable<string> DiscoverTables(string profileId)
        {
            var known = _entities.ContainsKey(profileId) ? _entities[profileId].Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase) : new HashSet<string>();
            var candidates = new[] { "Audit", "Logs", "Sessions", "Activities", "Events" };
            return candidates.Where(c => !known.Contains(c));
        }
    }
}
