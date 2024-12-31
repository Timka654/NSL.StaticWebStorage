using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSL.WCS.Client;
using NSL.WCS.Shared.Models;
using System.IO.Compression;
using System.Text.Json;

namespace NSL.StaticWebStorage
{
    public class Program
    {
        static CancellationTokenSource cts = new CancellationTokenSource();
        static CancellationTokenSource appLockToken = new CancellationTokenSource();

        public static async Task Main(string[] args)
        {
            appInstance = buildApplication(args);

            runApplication(cts.Token);

            try
            {
                await Task.Delay(Timeout.Infinite, appLockToken.Token);
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception)
            {

                throw;
            }
        }
        
        static WebApplication appInstance;

        static async void runApplication(CancellationToken cancellationToken)
        {
            Console.WriteLine("runApp");
            var app = appInstance;

            appInstance = null;
            try
            {
                await app.RunAsync(cancellationToken).ContinueWith(t => {
                    if (appInstance != null && appInstance != app)
                        runApplication(cts.Token);
                });
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                // really app close without reload configuration
                if (!cancellationToken.IsCancellationRequested)
                    appLockToken.Cancel();
            }
        }

        static WebApplication buildApplication(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

//#if RELEASE
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
//#endif

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ConfigureHttpsDefaults(httpsOptions =>
                {
                    //httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
                    httpsOptions.ServerCertificateSelector = CertificateStorage.GetOrLoad;
                });
            });

            var staticStorageConfiguration = builder.Configuration.GetRequiredSection("StaticStorage");

            var staticStorageData = staticStorageConfiguration.Get<StaticStorageConfigurationModel>();

            builder.Services.Configure<StaticStorageConfigurationModel>(staticStorageConfiguration);

            if (staticStorageData?.WCS != null)
            {
                builder.Services.Configure<WCSClientConfiguration>(c =>
                {
                    c.ProjectName = staticStorageData.WCS.ProjectName;
                    c.Host = staticStorageData.WCS.Host;
                    c.Routes = staticStorageData.Domains.Select(x => new ProxyRouteDataModel()
                    {
                        Name = x.Key,
                        MatchHosts = new List<string>() { x.Key },
                        Destinations = new List<ProxyRouteDestinationDataModel>() {
                        new ProxyRouteDestinationDataModel() {
                            Name = x.Key,
                            Address = "http://@@container_name:5000"
                        }
                        }
                    }).ToArray();
                });

                builder.Services.AddWCSClient();
            }

            var app = builder.Build();

            var storageOptions = app.Services.GetRequiredService<IOptionsMonitor<StaticStorageConfigurationModel>>();

            IDisposable? changeEvent = default;

            changeEvent = storageOptions.OnChange(async (_) =>
            {
                changeEvent?.Dispose();

                var cToken = cts;

                var ccts = cts = new CancellationTokenSource();

                appInstance = buildApplication(args);

                cToken.Cancel();
            });

            CertificateStorage.Load(app);

            if (storageOptions.CurrentValue?.WCS != null)
                app.MapWCSHealthCheckPoint();

            app.UseMiddleware<DomainRoutingMiddleware>();

            app.UseRouting();


            FileServerOptions ConfigureFileServer(FileServerOptions options, string? domainPath)
            {
                options.StaticFileOptions.ServeUnknownFileTypes = true;
                options.StaticFileOptions.DefaultContentType = "application/octet-stream";

                return options;
            }

            foreach (var _item in storageOptions.CurrentValue.Domains)
            {
                var item = _item;

                var path = Path.GetFullPath(item.Value.Path ?? Path.Combine("wwwroot", item.Key));

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var domainPath = $"/{item.Key}/storage";

                app.Logger.LogInformation($"Map {domainPath} -> {path}");

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
                            endpoints.MapPost("/upload", async (
                                [FromHeader(Name = "upload-path")] string uploadPath,
                                [FromHeader(Name = "upload-type")] string? uploadType,
                                [FromForm(Name = "file")] IFormFile file) =>
                            {
                                var fullUploadPath = Path.Combine(path, uploadPath);

                                using var uf = file.OpenReadStream();

                                if (uploadType == default)
                                {
                                    var ufi = new FileInfo(fullUploadPath);

                                    if (!ufi.Directory.Exists)
                                        ufi.Directory.Create();

                                    using var f = ufi.Create();

                                    await uf.CopyToAsync(f);
                                }
                                else if (uploadType == "extract")
                                {
                                    using ZipArchive za = new ZipArchive(uf, ZipArchiveMode.Read);

                                    foreach (var f in za.Entries)
                                    {
                                        if (string.IsNullOrEmpty(f.Name)) //folder
                                            continue;

                                        var filePath = Path.Combine(fullUploadPath, f.FullName);

                                        var dirPath = Path.GetDirectoryName(filePath);

                                        if (!Directory.Exists(dirPath))
                                            Directory.CreateDirectory(dirPath);

                                        f.ExtractToFile(filePath, true);
                                    }
                                }
                                else
                                    return Results.BadRequest($"invalid upload type - {uploadType}");

                                return Results.Ok();
                            })
                            .AddEndpointFilter(new UploadPointFilter(uploadToken))
                            .DisableAntiforgery();
                        });
                    }
                });
            }

            if (storageOptions.CurrentValue.HaveBaseRoute)
            {
                app.UseFileServer(ConfigureFileServer(new FileServerOptions()
                {
                    EnableDirectoryBrowsing = true,
                    FileProvider = new PhysicalFileProvider(Path.GetFullPath("wwwroot")),
                }, null));
            }

            return app;
        }
    }
}
