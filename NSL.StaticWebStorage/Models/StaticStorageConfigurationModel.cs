namespace NSL.StaticWebStorage.Models
{
    public class StaticStorageConfigurationModel
    {
        public StaticStorageWCSConfigurationModel WCS { get; set; } = new();

        public StaticStorageModelEnum Model { get; set; }

        public string[]? Domains { get; set; }

        public Dictionary<string, string> MasterTokens { get; set; }
    }
}
