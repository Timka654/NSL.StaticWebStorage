namespace NSL.StaticWebStorage.Models
{
    public class StaticStorageDomainConfigurationModel
    {
        public string? AccessToken { get; set; }

        public bool CanDownload { get; set; } = false;

        public bool CanUpload { get; set; } = false;

        public string? UploadToken { get; set; }

        public string? Path { get; set; }
    }
}
