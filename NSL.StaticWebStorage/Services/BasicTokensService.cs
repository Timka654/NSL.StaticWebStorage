using Microsoft.Extensions.Options;
using NSL.StaticWebStorage.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace NSL.StaticWebStorage.Services
{
    public abstract class BasicTokensService(IOptionsMonitor<StaticStorageConfigurationModel> configurationOptions)
    {
        public abstract string StoragePath { get; }

        public record StorageToken(StorageTokenModel? token, DateTime loadTime);

        private ConcurrentDictionary<string, StorageToken> storage = new ConcurrentDictionary<string, StorageToken>();

        protected StaticStorageConfigurationModel Configuration => configurationOptions.CurrentValue;

        public StorageTokenModel? TryGetToken(string token)
        {
            token = token.ToLower().Trim();

            var info = storage.GetOrAdd(token, token =>
            {
                var epath = Path.Combine(StoragePath, token);

                if (!File.Exists(epath))
                    return new StorageToken(null, DateTime.UtcNow);

                return new StorageToken(JsonSerializer.Deserialize<StorageTokenModel>(File.ReadAllText(epath), JsonSerializerOptions.Web), DateTime.UtcNow);
            });

            return info.token;
        }


        protected bool CreateToken(string token, StorageTokenModel data)
        {
            if(!Directory.Exists(StoragePath))
                Directory.CreateDirectory(StoragePath);

            var epath = Path.Combine(StoragePath, token);

            File.WriteAllText(epath, JsonSerializer.Serialize(data, JsonSerializerOptions.Web));

            return true;
        }
    }
}
