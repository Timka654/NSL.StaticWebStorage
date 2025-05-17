namespace NSL.StaticWebStorage.Models
{
    public class StorageTokenModel
    {
        public string Code { get; set; } = string.Join("", Enumerable.Range(0, 3).Select(x => Guid.NewGuid()));

        public bool CanDownload { get; set; } = false;

        public bool CanUpload { get; set; } = false;

        public bool CanShareAccess { get; set; } = false;

        public string[]? Storages { get; set; } = null;

        public string? Path { get; set; } = null;

        public DateTime? Expired { get; set; } = null;


        public bool IsExpired() => Expired != null && Expired < DateTime.UtcNow;

        public bool CanStorage(string name) => Storages == null || Storages.Contains(name.ToLower());

        public bool CanPath(string path) => Path == null || string.Equals(path, Path, StringComparison.InvariantCultureIgnoreCase);

        public bool CheckAccess(string code)
        {
            if (IsExpired())
                return false;

            if (Code != code)
                return false;

            return true;
        }

        public bool CheckUploadAccess(string path, string storage)
        {
            if (!CanUpload)
                return false;

            if (IsExpired())
                return false;

            if (!CanStorage(storage))
                return false;

            if (!CanPath(path))
                return false;

            return true;
        }

        public bool CheckDownloadAccess(string path, string storage)
        {
            if (!CanDownload)
                return false;

            if (IsExpired())
                return false;

            if (!CanStorage(storage))
                return false;

            if (!CanPath(path))
                return false;

            return true;
        }

        public bool CheckShareAccess(string path, string storage)
        {
            if (!CanShareAccess)
                return false;

            if (IsExpired())
                return false;

            if (!CanStorage(storage))
                return false;

            if (!CanPath(path))
                return false;

            return true;
        }
    }

    public class StorageTokenBuilder
    {
        List<string>? storages = null;

        StorageTokenModel token = new StorageTokenModel();

        private StorageTokenBuilder action(Action a)
        {
            a();
            return this;
        }

        public StorageTokenBuilder AllowDownload(bool value = true)
            => action(() => token.CanDownload = value);

        public StorageTokenBuilder AllowUpload(bool value = true)
            => action(() => token.CanUpload = value);

        public StorageTokenBuilder AllowShareAccess(bool value = true)
            => action(() => token.CanShareAccess = value);

        public StorageTokenBuilder AddStorage(string value)
            => action(() => (storages ??= new()).Add(value));

        public StorageTokenBuilder AllowPath(string value)
            => action(() => token.Path = value);

        public StorageTokenBuilder SetExpiredMinutes(int value)
            => action(() => token.Expired = DateTime.UtcNow.AddMinutes(value));

        public StorageTokenBuilder SetExpiredTime(TimeSpan value)
            => action(() => token.Expired = DateTime.UtcNow.Add(value));

        public StorageTokenBuilder SetExpiredTime(DateTime value)
            => action(() => token.Expired = value);

        public StorageTokenModel Build()
        {
            if (storages != null)
                token.Storages = storages.ToArray();
            
            return token;
        }
    }
}
