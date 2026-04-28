namespace Janatics.DataEngine.Models
{
    public class Profile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LastTested { get; set; } = string.Empty;
    }
}
