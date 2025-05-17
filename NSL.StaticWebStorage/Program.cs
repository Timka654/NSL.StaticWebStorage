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
                    c.Routes = staticStorageData.Model == StaticStorageModelEnum.Tokens ? staticStorageData.Domains.Select(x => x.ToProxyRoute()).ToArray() : [];
                });

                builder.Services.AddWCSDockerClient(true);
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

                appInstance = buildApplication(args);

                cToken.Cancel();
            });

            CertificateStorage.Load(app);

            if (storageOptions.CurrentValue?.WCS != null)
                app.MapWCSHealthCheckRoute();

            if (storageOptions.CurrentValue.Model == StaticStorageModelEnum.Domains)
                app.UseMiddleware<DomainRoutingMiddleware>();

            app.UseRouting();

            app.UseMvc();

            //if (storageOptions.CurrentValue.HaveBaseRoute)
            //{
            //    app.UseFileServer(ConfigureFileServer(new FileServerOptions()
            //    {
            //        EnableDirectoryBrowsing = true,
            //        FileProvider = new PhysicalFileProvider(Path.GetFullPath("wwwroot")),
            //    }, null));
            //}

            return app;
        }

        static FileServerOptions ConfigureFileServer(FileServerOptions options, string? domainPath)
        {
            options.StaticFileOptions.ServeUnknownFileTypes = true;
            options.StaticFileOptions.DefaultContentType = "application/octet-stream";

            return options;
        }
    }
}
