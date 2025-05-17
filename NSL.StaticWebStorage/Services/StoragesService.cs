using Microsoft.Extensions.Options;
using NSL.ASPNET.Attributes;
using NSL.StaticWebStorage.Models;
using NSL.StaticWebStorage.Utils;
using NSL.WCS.Client;

namespace NSL.StaticWebStorage.Services
{
    [RegisterService(ServiceLifetime.Singleton)]
    public class StoragesService(IOptionsMonitor<StaticStorageConfigurationModel> configurationOptions, WCSDockerClient wcs, IOptions<WCSClientConfiguration> wcsConfiguration)
    {
        string sharedTokensPath = "data/storages";

        StaticStorageConfigurationModel configuration => configurationOptions.CurrentValue;

        public bool ExistsStorage(string? id)
        {
            if (id == default)
                return false;

            var epath = Path.Combine(sharedTokensPath, id);

            return Directory.Exists(epath);
        }

        public async Task<string?> CreateStorageAsync(string? id, bool shared)
        {
            if (id == default)
                id = Guid.NewGuid().ToString();

            var epath = Path.Combine(sharedTokensPath, id);

            if (Directory.Exists(epath))
                return default;

            Directory.CreateDirectory(epath);

            if (configuration.Model == StaticStorageModelEnum.Domains)
            {
                await wcs.StopAsync(CancellationToken.None);

                wcsConfiguration.Value.Routes = wcsConfiguration.Value.Routes.Append(id.ToProxyRoute()).ToArray();

                await wcs.StartAsync(CancellationToken.None);
            }

            return id;
        }
    }
}
