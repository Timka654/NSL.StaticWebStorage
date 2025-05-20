namespace NSL.StaticWebStorage.Models
{
    public class TempTokenData
    {
        public StorageTokenModel? Token { get; set; }

        public DateTime AccessTime { get; set; } = DateTime.UtcNow;
    }
}
