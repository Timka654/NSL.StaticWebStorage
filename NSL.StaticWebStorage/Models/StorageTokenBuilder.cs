namespace NSL.StaticWebStorage.Models
{
    public class StorageTokenBuilder
    {
        List<string>? storages = null;
        List<string>? paths = null;

        StorageTokenModel token = new StorageTokenModel();

        private StorageTokenBuilder action(Action a)
        {
            a();
            return this;
        }

        private StorageTokenBuilder()
        {
                
        }

        public static StorageTokenBuilder Create()
            => new StorageTokenBuilder();

        public StorageTokenBuilder AllowDownload(bool value = true)
            => action(() => token.CanDownload = value);

        public StorageTokenBuilder AllowUpload(bool value = true)
            => action(() => token.CanUpload = value);

        public StorageTokenBuilder AllowShareAccess(bool value = true)
            => action(() => token.CanShareAccess = value);

        public StorageTokenBuilder AddStorage(string[] values)
        {
            if (values != null)
            {
                storages ??= new();

                foreach (var value in values)
                {
                    storages.Add(value.ToLower().Trim());
                }
            }

            return this;
        }

        public StorageTokenBuilder AddStorage(string value)
            => action(() => (storages ??= new()).Add(value.ToLower().Trim()));

        public StorageTokenBuilder AddPath(string[]? values)
        {
            if (values != null)
            {
                paths ??= new();

                foreach (var value in values)
                {
                    paths.Add(value);
                }
            }

            return this;
        }

        public StorageTokenBuilder AddPath(string? value)
            => value == null ? this : action(() => (paths ??= new()).Add(value));

        public StorageTokenBuilder SetExpiredMinutes(int value)
            => SetExpiredTime(DateTime.UtcNow.AddMinutes(value));

        public StorageTokenBuilder SetExpiredTime(TimeSpan value)
            => SetExpiredTime(DateTime.UtcNow.Add(value));

        public StorageTokenBuilder SetExpiredTime(DateTime? value)
            => action(() => token.Expired = value);

        public StorageTokenBuilder SetCode(string? value)
            => action(() => token.Code = value ?? token.Code);

        public StorageTokenBuilder SetStorageOwner(string value)
            => action(() => token.StorageOwner = value);

        public StorageTokenBuilder SetPathOwner(string value)
            => action(() => token.PathOwner = value);

        public StorageTokenModel Build()
        {
            if (storages != null)
                token.Storages = storages.ToArray();
            if (paths != null)
                token.Paths = paths.ToArray();
            
            return token;
        }
    }
}
