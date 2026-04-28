using System.Collections.Generic;

namespace Janatics.DataEngine.Models
{
    public class EntityConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public string PkColumn { get; set; } = string.Empty;
        public bool IsReadOnly { get; set; }
        public List<string> Roles { get; set; } = new();
        public int ColumnCount { get; set; }
    }
}
