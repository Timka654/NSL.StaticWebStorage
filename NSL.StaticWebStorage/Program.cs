using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSL.StaticWebStorage.Models;
using NSL.StaticWebStorage.Utils;
using NSL.StaticWebStorage.Utils.Route;
using NSL.WCS.Client;
using NSL.WCS.Shared.Models;
using System.IO.Compression;
using System.Text.Json;
using NSL.ASPNET;
using Microsoft.Extensions.DependencyInjection;
using NSL.StaticWebStorage.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.Intrinsics.X86;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Http.Features;
using NSL.SocketCore;
using NSL.HostOrchestrator.Client;
using NSL.HostOrchestrator.Client.Metrics;
using NSL.HostOrchestrator.Client.Logger;

namespace NSL.StaticWebStorage
{
    public class Program
    {
        static CancellationTokenSource cts = new CancellationTokenSource();
        static CancellationTokenSource appLockToken = new CancellationTokenSource();

        public static async Task Main(string[] args)
        {
            appInstance = await buildApplication(args);

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
                await app.RunAsync(cancellationToken).ContinueWith(t =>
                {
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

        static async Task<WebApplication> buildApplication(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //#if RELEASE
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            if (args.Contains("development"))
                builder.Configuration.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
            //#endif
            builder.Logging.AddHOLogger();

            builder.Services.AddHOMetricsProvider();

            builder.Services.AddDefaultNodeHOClient(builder.Configuration, true);

            var staticStorageConfiguration = builder.Configuration.GetRequiredSection("StaticStorage");

            var staticStorageData = staticStorageConfiguration.Get<StaticStorageConfigurationModel>();

            builder.Services.Configure<StaticStorageConfigurationModel>(staticStorageConfiguration);

            if (staticStorageData?.WCS != null)
            {
                builder.Services.Configure<WCSClientConfiguration>(staticStorageConfiguration.GetRequiredSection("WCS"));

                builder.Services.AddWCSConfiguration(c =>
                {
                    c.Routes ??= Array.Empty<ProxyRouteDataModel>();

                    if (staticStorageData.StaticConfiguration.WCSRoute)
                    {
                        foreach (var item in staticStorageData.StaticConfiguration.Domains)
                        {
                            c.Routes = c.Routes.Append(new ProxyRouteDataModel()
                            {
                                ACMECert = staticStorageData.StaticConfiguration.WCSAcme,
                                Destinations = new List<ProxyRouteDestinationDataModel>() {
                                    new ProxyRouteDestinationDataModel() {
                                        Name = item,
                                        Address = staticStorageData.EndPoint
                                    }
                            },
                                MatchHosts = [item],
                                Name = item
                            }).ToArray();
                        }
                    }


                    if (Directory.Exists(StoragesService.StoragePath))
                    {
                        foreach (var item in Directory.GetFiles(StoragesService.StoragePath, "*.meta"))
                        {
                            var data = JsonSerializer.Deserialize<StorageMetaDataModel>(File.ReadAllText(item), JsonSerializerOptions.Web);

                            c.Routes = c.Routes.Append(new ProxyRouteDataModel()
                            {
                                ACMECert = data.AcmeCert,
                                Destinations = new List<ProxyRouteDestinationDataModel>() {
                                    new ProxyRouteDestinationDataModel() {
                                        Name = data.Id,
                                        Address = staticStorageData.EndPoint
                                    }
                            },
                                MatchHosts = [data.Id],
                                Name = data.Id
                            }).ToArray();
                        }
                    }
                });

                builder.Services.AddWCSTcpClient(true);
            }

            builder.Services.AddControllers();

            builder.Services.RegisterServices();

            var app = builder.Build();

            var storageOptions = app.Services.GetRequiredService<IOptionsMonitor<StaticStorageConfigurationModel>>();

            IDisposable? changeEvent = default;

            changeEvent = storageOptions.OnChange(async (_) =>
            {
                changeEvent?.Dispose();

                var cToken = cts;

                var ccts = cts = new CancellationTokenSource();

                appInstance = await buildApplication(args);

                cToken.Cancel();
            });

            if (storageOptions.CurrentValue?.WCS != null)
                app.MapWCSHealthCheckRoute();

            app.MapControllers();

            if (storageOptions.CurrentValue.Model == StaticStorageModelEnum.Domains)
                app.UseMiddleware<DomainRoutingMiddleware>();


            var storagePath = Path.Combine(builder.Environment.ContentRootPath, "data", "storages");

            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);

            var fileProvider = new PhysicalFileProvider(storagePath);

            app.UseDefaultFiles(new DefaultFilesOptions()
            {
                FileProvider = fileProvider,
                DefaultFileNames = staticStorageData.StaticConfiguration.DefaultFiles ?? [],
                RequestPath = "/storage"
            });

            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = fileProvider,
                RequestPath = "/storage",
                ServeUnknownFileTypes = true,
                DefaultContentType = staticStorageData.StaticConfiguration.DefaultFileMimeType ?? "application/octet-stream",
                ContentTypeProvider = new FileExtensionContentTypeProvider(staticStorageData.StaticConfiguration.MimeTypes)
            });

            if (app.Services.GetService(typeof(HOClient)) != null)
            {
                await app.Services.LoadHOMetrics();
                await app.Services.LoadHOLogger();
            }


            return app;
        }
    }
}
