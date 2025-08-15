namespace NSL.StaticWebStorage.Models
{
    public class StorageMetaDataModel
    {
        public string Id { get; set; }

        public bool Shared { get; set; } = false;

        public bool AcmeCert { get; set; } = false;
    }

    public class TempStorageData
    {
        public StorageMetaDataModel? Storage { get; set; }

        public DateTime AccessTime { get; set; } = DateTime.UtcNow;
    }
}
