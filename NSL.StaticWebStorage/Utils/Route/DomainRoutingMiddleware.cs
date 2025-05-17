using Microsoft.Extensions.Options;
using NSL.StaticWebStorage.Models;

namespace NSL.StaticWebStorage.Utils.Route
{
    public class DomainRoutingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOptions<StaticStorageConfigurationModel> configuration;

        public DomainRoutingMiddleware(RequestDelegate next, IOptions<StaticStorageConfigurationModel> configuration)
        {
            _next = next;
            this.configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var host = context.Request.Host.Host;

            context.Request.Path = $"/{host}{context.Request.Path}";

            //$"/{item.Key}/storage"
            //if (host == "domain1.com")
            //{
            //}
            //else if (host == "domain2.com")
            //{
            //    context.Request.Path = "/domain2" + context.Request.Path;
            //}

            await _next(context);
        }
    }
}
