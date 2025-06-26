namespace NSL.StaticWebStorage.Shared.Models
{
    public class CreateStorageTokenRequestModel
    {
        public string? Token { get; set; }
        public string? Code { get; set; }

        public bool CanDownload { get; set; } = false;

        public bool CanUpload { get; set; } = false;

        public bool CanShareAccess { get; set; } = false;

        public string[]? Storages { get; set; } = null;

        public string[]? Paths { get; set; } = null;

        public DateTime? Expired { get; set; } = null;
    }
}
