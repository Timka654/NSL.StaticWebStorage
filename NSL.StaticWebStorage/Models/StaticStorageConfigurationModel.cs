using NSL.WCS.Client;

namespace NSL.StaticWebStorage.Models
{
    public class StaticStorageConfigurationModel
    {
        public WCSClientConfiguration? WCS { get; set; }

        public StaticStorageModelEnum Model { get; set; }

        public Dictionary<string, string> MasterTokens { get; set; }

        public StaticStorageStaticConfigurationModel StaticConfiguration { get; set; }

        public string EndPoint { get; set; }
    }

    public class StaticStorageStaticConfigurationModel
    {
        public string[]? Domains { get; set; }

        public bool WCSRoute { get; set; }

        public bool WCSAcme { get; set; }

        public string[]? DefaultFiles { get; set; }

        public string? DefaultFileMimeType { get; set; }

        public Dictionary<string, string> MimeTypes { get; set; }

    }
}
