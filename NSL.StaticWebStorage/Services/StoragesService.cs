using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSL.ASPNET.Attributes;
using NSL.StaticWebStorage.Models;
using NSL.StaticWebStorage.Utils;
using NSL.WCS.Client;
using System.Collections.Concurrent;
using System.Text.Json;

namespace NSL.StaticWebStorage.Services
{
    [RegisterService(ServiceLifetime.Singleton)]
    public class StoragesService(IOptionsMonitor<StaticStorageConfigurationModel> configurationOptions, IServiceProvider serviceProvider)
    {
        WCSDockerClient? wcs => serviceProvider.GetService<WCSDockerClient>();

        IOptions<WCSClientConfiguration> wcsConfiguration => serviceProvider.GetService<IOptions<WCSClientConfiguration>>()!;

        string StoragePath = "data/storages";

        private ConcurrentDictionary<string, TempStorageData> storage = new ConcurrentDictionary<string, TempStorageData>();

        protected StaticStorageConfigurationModel Configuration => configurationOptions.CurrentValue;

        public StorageMetaDataModel? TryGetStorage(string id)
        {
            id = id.ToLower().Trim();

            var info = storage.GetOrAdd(id, id =>
            {
                var epath = StoragePath;

                epath = Path.Combine(epath, $"{id}.meta");

                if (!File.Exists(epath))
                {
                    return new();
                }

                return new()
                {
                    Storage = JsonSerializer.Deserialize<StorageMetaDataModel>(File.ReadAllText(epath), JsonSerializerOptions.Web)
                };
            });

            info.AccessTime = DateTime.UtcNow;

            return info.Storage;
        }

        public bool ExistsStorage(string? id)
        {
            if (id == default)
                return false;

            return TryGetStorage(id) != null;
        }

        public async Task<bool> CreateStorageAsync(string id, bool shared)
        {
            var epath = Path.Combine(StoragePath, id);

            if (Directory.Exists(epath))
                return false;

            Directory.CreateDirectory(epath);

            await File.WriteAllTextAsync($"{epath}.meta"
                , JsonSerializer.Serialize(new StorageMetaDataModel { Shared = shared }, JsonSerializerOptions.Web)
                , CancellationToken.None);

            if (Configuration.Model == StaticStorageModelEnum.Domains && wcs != null && wcsConfiguration != null)
            {
                await wcs.StopAsync(CancellationToken.None);

                wcsConfiguration.Value.Routes = wcsConfiguration.Value.Routes.Append(id.ToProxyRoute()).ToArray();

                await wcs.StartAsync(CancellationToken.None);
            }

            return true;
        }
    }
}
