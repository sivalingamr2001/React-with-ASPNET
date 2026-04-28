namespace Janatics.DataEngine.Models
{
    public class ColumnConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsPK { get; set; }
        public bool IsFk { get; set; }
        public string FkRef { get; set; } = string.Empty;
    }
}
