namespace NSL.StaticWebStorage.Models
{
    public class StorageMetaDataModel
    {
        public bool Shared { get; set; } = false;
    }

    public class TempStorageData
    {
        public StorageMetaDataModel? Storage { get; set; }

        public DateTime AccessTime { get; set; } = DateTime.UtcNow;
    }
}
