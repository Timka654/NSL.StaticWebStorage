using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using NSL.Utils;
using NSL.WCS.Shared.Models;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;

namespace NSL.StaticWebStorage.Utils
{
    internal class CertificateStorage
    {
        static IOptionsMonitor<CertificateConfigurationModel>? Configuration { get; set; }

        static IDisposable? ChangeEvent { get; set; }

        static string CertStorageFullPath => Path.GetFullPath(Configuration.CurrentValue.CertStoragePath);

        static ConcurrentDictionary<string, X509Certificate2> loadedCerts;

        static ConcurrentDictionary<string, Action<bool>> removeCertFileHandles = new ConcurrentDictionary<string, Action<bool>>();

        static ConcurrentDictionary<string, X509Certificate2> cache = new ConcurrentDictionary<string, X509Certificate2>();

        static X509Certificate2? defaultCert;
        static string DefaultCertPath => Path.GetFullPath(Path.Combine(CertStorageFullPath, "default.zip"));

        static FSWatcher? certStorageWatch;

        static ILogger? logger;

        public static void Configure(IServiceCollection services, CertificateConfigurationModel configuration)
        {
            services.Configure<CertificateConfigurationModel>(c =>
            {
                c.CanDefault = configuration.CanDefault;
                c.CertStoragePath = configuration.CertStoragePath;
            });
        }

        public static void Load(WebApplication app)
        {
            logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("CertificateStorage");

            loadedCerts = new ConcurrentDictionary<string, X509Certificate2>();

            Configuration = app.Services.GetRequiredService<IOptionsMonitor<CertificateConfigurationModel>>();

            loadCerts();
        }

        static void loadCerts()
        {
            ChangeEvent = Configuration.OnChange((config) =>
            {
                ChangeEvent?.Dispose();
                certStorageWatch?.Dispose();
                certStorageWatch = null;

                loadCerts();
            });

            if (!Directory.Exists(CertStorageFullPath))
                Directory.CreateDirectory(CertStorageFullPath);

            foreach (var item in Directory.GetFiles(CertStorageFullPath, "*.zip", SearchOption.AllDirectories))
            {
                if (DefaultCertPath == item)
                    continue;

                loadCertificate(item, true);
            }

            if (Configuration.CurrentValue.CanDefault && File.Exists(DefaultCertPath))
            {
                defaultCert = loadCertificate(DefaultCertPath, false);
            }

            certStorageWatch = new FSWatcher(() => new FileSystemWatcher(CertStorageFullPath, "*.zip") { IncludeSubdirectories = true })
            {
                OnAnyChanges = OnCertChanged
            };
        }


        static X509Certificate2? loadCertificate(string path, bool add)
        {
            try
            {
                using ZipArchive zip = ZipFile.OpenRead(path);

                var pemContent = ReadAllText(zip.Entries.First(e => e.FullName.EndsWith(".pem")));
                var keyContent = ReadAllText(zip.Entries.First(e => e.FullName.EndsWith(".key")));

                var pks = X509Certificate2.CreateFromPem(pemContent, keyContent);

                if (add)
                {
                    var mapContent = ReadAllText(zip.Entries.First(e => e.FullName.EndsWith(".map")));

                    loadedCerts.TryAdd(mapContent, pks);

                    Action<bool> onRemove = (reload) =>
                    {
                    };

                    foreach (var _item in mapContent.Split(Environment.NewLine))
                    {
                        var item = _item;

                        onRemove += (reload) =>
                        {
                            if (!reload)
                                logger?.LogInformation($"Certificate remove '{item}'('{path}')");

                            loadedCerts.TryRemove(item, out _);
                        };

                        loadedCerts.AddOrUpdate(item, pks, (_, _) => pks);

                        logger?.LogInformation($"Certificate load '{item}' -> '{path}'");
                    }

                    removeCertFileHandles.AddOrUpdate(path, onRemove, (key, action) => { action(true); return onRemove; });
                }

                cache.Clear();

                return pks;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error while loading certificate \"{path}\" - {ex}");
            }

            return null;
        }

        static void OnCertChanged(FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Changed)
            {
                logger?.LogInformation($"Certificate file '{e.FullPath}' {(e.ChangeType == WatcherChangeTypes.Created ? "appended" : "changed")}. Try load");

                bool isDefaultCert = e.FullPath == DefaultCertPath;

                isDefaultCert = Configuration.CurrentValue.CanDefault && isDefaultCert;

                var cert = loadCertificate(e.FullPath, !isDefaultCert);

                if (cert == null)
                {
                    logger?.LogError($"Certificate '{e.FullPath}' load failed");

                    throw new IOException($"Error while loading certificate \"{e.Name}\"");
                }

                if (isDefaultCert)
                {
                    defaultCert = cert;
                    cache.Clear();
                }
            }

            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                logger?.LogInformation($"Certificate file '{e.FullPath}' removed. Try clear");

                if (removeCertFileHandles.TryRemove(e.FullPath, out var h))
                {
                    h(false);
                    cache.Clear();
                }
            }
        }

        public static X509Certificate2? GetOrLoad(ConnectionContext? context, string? domain)
            => GetOrLoad(domain);

        public static X509Certificate2 GetOrLoad(string domain)
        {
            if (cache.TryGetValue(domain, out var cert))
                return cert;

            foreach (var item in loadedCerts)
            {
                if (IsSubdomainMatch(domain, item.Key))
                {
                    cache.TryAdd(domain, item.Value);
                    return item.Value;
                }
            }

            if (defaultCert != null)
            {
                cache.TryAdd(domain, defaultCert);
                return defaultCert;
            }

            return null;
        }

        private static bool IsSubdomainMatch(string inputDomain, string pattern)
        {
            inputDomain = inputDomain.ToLowerInvariant();
            pattern = pattern.ToLowerInvariant();

            if (pattern.StartsWith("*."))
            {
                string baseDomain = pattern.Substring(2);
                return inputDomain.EndsWith("." + baseDomain) || inputDomain == baseDomain;
            }

            return string.Equals(inputDomain, pattern, StringComparison.OrdinalIgnoreCase);
        }

        static string ReadAllText(ZipArchiveEntry item)
        {
            using var stream = item.Open();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public class CertificateConfigurationModel
    {
        public bool CanDefault { get; set; } = true;

        public string CertStoragePath { get; set; } = "data/certs/";
    }

    public static class RouteExtensions
    {
        public static ProxyRouteDataModel ToProxyRoute(this string domain)
            => new ProxyRouteDataModel()
            {
                Name = domain,
                MatchHosts = new List<string>() { domain },
                Destinations = new List<ProxyRouteDestinationDataModel>() {
                            new ProxyRouteDestinationDataModel() {
                                Name = domain,
                                Address = "http://@@container_name:5000"
                            }
                        }
            };
    }
}
