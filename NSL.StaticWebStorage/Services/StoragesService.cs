using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSL.ASPNET.Attributes;
using NSL.StaticWebStorage.Models;
using NSL.StaticWebStorage.Shared.Models;
using NSL.StaticWebStorage.Utils;
using NSL.WCS.Client;
using NSL.WCS.Shared.Models;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace NSL.StaticWebStorage.Services
{
    [RegisterService(ServiceLifetime.Singleton)]
    public class StoragesService(IOptionsMonitor<StaticStorageConfigurationModel> configurationOptions
        , IServiceProvider serviceProvider
        , ILogger<StoragesService> logger)
    {
        WCSDockerClient? wcs => serviceProvider.GetService<WCSDockerClient>();

        IOptionsMonitor<WCSClientConfiguration> wcsConfiguration => serviceProvider.GetService<IOptionsMonitor<WCSClientConfiguration>>()!;

        public const string StoragePath = "data/storages";

        private ConcurrentDictionary<string, TempStorageData> storage = new ConcurrentDictionary<string, TempStorageData>();

        protected StaticStorageConfigurationModel Configuration => configurationOptions.CurrentValue;

        public StorageMetaDataModel? TryGetStorage(string id)
        {
            id = id.ToLower().Trim();

            var info = storage.GetOrAdd(id, id =>
            {
                var epath = Path.Combine(StoragePath, $"{id}.meta");

                if (!File.Exists(epath))
                {
                    logger.LogWarning("Storage metadata file not found: {Path}", epath);
                    return new();
                }

                logger.LogInformation("Loading storage metadata from file: {Path}", epath);

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

        public async Task<bool> CreateStorageAsync(CreateStorageRequestModel storage)
        {
            var epath = Path.Combine(StoragePath, storage.Id);

            if (!Directory.Exists(epath))
                Directory.CreateDirectory(epath);

            var data = new StorageMetaDataModel
            {
                Id = storage.Id,
                Shared = storage.Shared,
                AcmeCert = storage.AcmeCert
            };

            await File.WriteAllTextAsync($"{epath}.meta"
                , JsonSerializer.Serialize(data, JsonSerializerOptions.Web)
                , CancellationToken.None);

            this.storage[storage.Id] = new TempStorageData() { Storage = data };

            if (Configuration.Model == StaticStorageModelEnum.Domains && wcs != null && wcsConfiguration != null)
            {
                await wcs.StopAsync(CancellationToken.None);

                wcsConfiguration.CurrentValue.Routes = wcsConfiguration.CurrentValue.Routes.Append(new WCS.Shared.Models.ProxyRouteDataModel()
                {
                    ACMECert = storage.AcmeCert,
                    MatchHosts = [storage.Id],
                    Name = storage.Id,
                    Destinations = new List<ProxyRouteDestinationDataModel>() {
                            new ProxyRouteDestinationDataModel() {
                                Name = storage.Id,
                                Address = Configuration.EndPoint
                            }
                        }
                }).ToArray();

                await wcs.StartAsync(CancellationToken.None);
            }

            return true;
        }
    }
}
