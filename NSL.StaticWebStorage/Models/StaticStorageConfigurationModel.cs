namespace NSL.StaticWebStorage.Models
{
    public class StaticStorageConfigurationModel
    {
        public StaticStorageWCSConfigurationModel WCS { get; set; } = new();

        //public Dictionary<string, StaticStorageTokenConfigurationModel> Tokens { get; set; } = new();

        //public Dictionary<string, StaticStorageDomainConfigurationModel> Domains { get; set; } = new();

        public StaticStorageModelEnum Model { get; set; }

        public string[]? Domains { get; set; }
    }
}
