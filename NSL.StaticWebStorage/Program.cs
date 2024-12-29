using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using NSL.Utils;
using NSL.WCS.Client;
using System.IO;
using System.Net.Http.Headers;

namespace NSL.StaticWebStorage
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ConfigureHttpsDefaults(httpsOptions =>
                {
                    //httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
                    httpsOptions.ServerCertificateSelector = CertificateStorage.GetOrLoad;
                });
            });

            var staticStorageConfiguration = builder.Configuration.GetSection("StaticStorage");

            var staticStorageData = staticStorageConfiguration.Get<StaticStorageConfigurationModel>();

            builder.Services.Configure<StaticStorageConfigurationModel>(staticStorageConfiguration);

            if (staticStorageData?.WCS != null)
            {
                builder.Services.Configure<WCSClientConfiguration>(c =>
                {
                    c.ProjectName = staticStorageData.WCS.ProjectName;
                    c.Host = staticStorageData.WCS.Host;
                    c.Routes = staticStorageData.Domains.Select(x => new WCS.Shared.Models.ProxyRouteDataModel()
                    {
                        Name = x.Key,
                        MatchHosts = new List<string>() { x.Key },
                        Destinations = new List<WCS.Shared.Models.ProxyRouteDestinationDataModel>() {
                        new WCS.Shared.Models.ProxyRouteDestinationDataModel() {
                            Name = x.Key,
                            Address = "http://@@container_name:5000"
                        }
                        }
                    }).ToArray();
                });

                builder.Services.AddWCSClient();
            }

            var app = builder.Build();

            CertificateStorage.Load(app);

            if (staticStorageData?.WCS != null)
                app.MapWCSHealthCheckPoint();

            app.UseMiddleware<DomainRoutingMiddleware>();

            app.UseRouting();


            FileServerOptions ConfigureFileServer(FileServerOptions options, string? domainPath)
            {
                options.StaticFileOptions.ServeUnknownFileTypes = true;
                options.StaticFileOptions.DefaultContentType = "application/octet-stream";

                return options;
            }

            foreach (var _item in staticStorageData.Domains)
            {
                var item = _item;

                var path = Path.GetFullPath(item.Value.Path ?? Path.Combine("wwwroot", item.Key));

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var domainPath = $"/{item.Key}/storage";

                var domainPathLength = domainPath.Length;

                var accessToken = item.Value.AccessToken;

                var uploadToken = item.Value.UploadToken;

                app.Map(domainPath, true, appBuilder =>
                {
                    appBuilder.Use(async (r, n) =>
                    {
                        r.Request.Path = r.Request.Path.Value.Substring(domainPathLength);

                        await n();
                    });

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        appBuilder.Use(async (r, n) =>
                        {
                            if (r.Request.Headers.TryGetValue("access-token", out var auth) && auth == accessToken)
                            {
                                await n(r);

                                return;
                            }
                            r.Response.StatusCode = 404;
                        });
                    }

                    appBuilder.UseFileServer(ConfigureFileServer(new FileServerOptions()
                    {
                        EnableDirectoryBrowsing = true,
                        FileProvider = new PhysicalFileProvider(path),
                    }, domainPath));

                    if (item.Value.CanUpload)
                    {
                        appBuilder.UseRouting();
                        appBuilder.UseEndpoints(endpoints =>
                        {
                            endpoints.MapPost("/upload", async ([FromHeader(Name = "upload-path")] string uploadPath, [FromForm(Name = "file")] IFormFile file) =>
                            {
                                var fullUploadPath = Path.Combine(path, uploadPath);

                                var ufi = new FileInfo(fullUploadPath);

                                if (!ufi.Directory.Exists)
                                    ufi.Directory.Create();

                                using var f = ufi.Create();
                                using var uf = file.OpenReadStream();
                                await uf.CopyToAsync(f);

                                return Results.Ok();
                            })
                            .AddEndpointFilter(new UploadPointFilter(uploadToken))
                            .DisableAntiforgery();
                        });
                    }
                });
            }

            if (staticStorageData.HaveBaseRoute)
            {
                app.UseFileServer(ConfigureFileServer(new FileServerOptions()
                {
                    EnableDirectoryBrowsing = true,
                    FileProvider = new PhysicalFileProvider(Path.GetFullPath("wwwroot")),
                }, null));
            }

            app.Run();
        }
    }
}
