using Microsoft.Extensions.Options;
using NSL.StaticWebStorage.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace NSL.StaticWebStorage.Services
{
    public abstract class BasicTokensService(IOptionsMonitor<StaticStorageConfigurationModel> configurationOptions)
    {
        public abstract string StoragePath { get; }

        private ConcurrentDictionary<string, TempTokenData> storage = new ConcurrentDictionary<string, TempTokenData>();

        protected StaticStorageConfigurationModel Configuration => configurationOptions.CurrentValue;

        public StorageTokenModel? TryGetToken(string? token)
        {
            if (token == null)
                return default;

            token = token.ToLower().Trim();

            var info = storage.GetOrAdd(token, token =>
            {
                var epath = StoragePath;

                epath = Path.Combine(epath, token);

                if (!File.Exists(epath))
                {
                    if (Configuration.MasterTokens.TryGetValue(token, out var code))
                    {
                        return new TempTokenData()
                        {
                            Token = new StorageTokenModel
                            {
                                Code = code,
                                CanDownload = true,
                                CanUpload = true,
                                CanShareAccess = true,
                            }
                        };
                    }

                    return new();
                }

                return new()
                {
                    Token = JsonSerializer.Deserialize<StorageTokenModel>(File.ReadAllText(epath), JsonSerializerOptions.Web).SetRemoveDelegate(() =>
                    {
                        TryRemoveToken(token);
                    })
                };
            });

            info.AccessTime = DateTime.UtcNow;

            return info.Token;
        }


        public bool TryRemoveToken(string token)
        {
            token = token.ToLower().Trim();

            var epath = StoragePath;

            epath = Path.Combine(epath, token);

            if (File.Exists(epath))
            {
                File.Delete(epath);
                storage.TryRemove(token, out _);
            }

            return true;
        }


        public bool CreateToken(string token, StorageTokenModel data)
        {
            var epath = StoragePath;

            if (!Directory.Exists(epath))
                Directory.CreateDirectory(epath);

            epath = Path.Combine(epath, token);

            if (File.Exists(epath))
                return false;

            File.WriteAllText(epath, JsonSerializer.Serialize(data, JsonSerializerOptions.Web));

            storage.TryRemove(token, out _);


            return true;
        }
    }
}
