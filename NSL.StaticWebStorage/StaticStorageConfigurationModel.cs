namespace NSL.StaticWebStorage
{

    public class StaticStorageWCSConfigurationModel
    {
        public string Host { get; set; } = "tcp://nsl.wcs.yarp:44560";

        public string ProjectName { get; set; } = "NSL.StaticWebStorage";
    }

    public class StaticStorageDomainConfigurationModel
    {
        public string? AccessToken { get; set; }

        public bool CanUpload { get; set; } = false;

        public string? UploadToken { get; set; }

        public string? Path { get; set; }
    }

    public class StaticStorageConfigurationModel
    {
        public StaticStorageWCSConfigurationModel WCS { get; set; } = new();

        public Dictionary<string, StaticStorageDomainConfigurationModel> Domains { get; set; } = new();

        public bool HaveBaseRoute { get; set; } = false;
    }
}
