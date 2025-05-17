using Microsoft.Extensions.Options;
using NSL.ASPNET.Attributes;
using NSL.StaticWebStorage.Models;

namespace NSL.StaticWebStorage.Services
{
    [RegisterService(ServiceLifetime.Singleton)]
    public class MasterTokensService(IOptionsMonitor<StaticStorageConfigurationModel> configurationOptions) : BasicTokensService(configurationOptions)
    {
        public override string StoragePath => "data/tokens";
    }
}
