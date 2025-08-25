using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NSL.StaticWebStorage.Services;
using NSL.StaticWebStorage.Shared;
using NSL.StaticWebStorage.Utils.Route;
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace NSL.StaticWebStorage.Controllers
{
    public class FilesController(StoragesService storagesService) : ControllerBase
    {
        [TokenAccessFilter(uploadCheck: true)]
        [HttpGet("/__sws_api/{storage}/catalog/{*path}")]
        public IActionResult Catalog([FromRoute] string storage, [FromRoute] string path = ".")
        {
            storage = storage.Trim().ToLower();

            var storageFullPath = Path.GetFullPath(Path.Combine("data", "storages", storage));

            var epath = Path.Combine(storageFullPath, path);

            if (epath.IndexOf(storageFullPath) == -1)
                return Forbid();

            var directory = new DirectoryInfo(epath);

            if (!directory.Exists)
                return Ok(Array.Empty<object>());

            var dirs = directory.GetDirectories("*", SearchOption.TopDirectoryOnly);

            var files = directory.GetDirectories("!*._sws__*", SearchOption.TopDirectoryOnly);

            return Ok(dirs.Select(x => x.Name).Concat(files.Select(x => x.Name)));
        }

        [TokenAccessFilter(downloadCheck: true)]
        [HttpGet("/__sws_api/{storage}/download/{token}/{*path}")]
        [HttpGet("/__sws_api/{storage}/download/{*path}")]
        public IActionResult Download([FromRoute] string storage, [FromRoute] string token, [FromRoute] string path = ".")
        {
            storage = storage.Trim().ToLower();

            var storageFullPath = Path.GetFullPath(Path.Combine("data", "storages", storage));

            var epath = Path.Combine(storageFullPath, path);

            if (epath.IndexOf(storageFullPath) == -1)
                return Forbid();

            if (!System.IO.File.Exists(epath))
                return NotFound();

            return File(System.IO.File.OpenRead(epath), "application/octet-stream", Path.GetFileName(epath));
        }

        [TokenAccessFilter(downloadCheck: true)]
        [HttpGet("/__sws_api/{storage}/hash/{*path}")]
        public IActionResult Hash([FromRoute] string storage, [FromRoute] string path = ".")
        {
            storage = storage.Trim().ToLower();

            var storageFullPath = Path.GetFullPath(Path.Combine("data", "storages", storage));

            var epath = Path.Combine(storageFullPath, $"{path}._sws__hash");

            if (epath.IndexOf(storageFullPath) == -1)
                return Forbid();

            if (!System.IO.File.Exists(epath))
                return NotFound();

            return Ok(System.IO.File.ReadAllText(epath));
        }

        [TokenAccessFilter(uploadCheck: true)]
        [HttpPost("/__sws_api/{storage}/delete/{*path}")]
        public IActionResult Delete([FromRoute] string storage, [FromRoute] string path = ".")
        {
            storage = storage.Trim().ToLower();

            var storageFullPath = Path.GetFullPath(Path.Combine("data", "storages", storage));

            var epath = Path.Combine(storageFullPath, path);

            if (epath.IndexOf(storageFullPath) == -1)
                return Forbid();

            if (!System.IO.File.Exists(epath))
                return NotFound();

            System.IO.File.Delete(epath);
            System.IO.File.Delete($"{epath}._sws__hash");

            return Ok();
        }

        //[TokenAccessFilter(uploadCheck: true)]
        //[HttpPost("/__sws_api/{storage}/start_upload/{*path}")]
        //public async Task<IActionResult> StartUpload([FromRoute] string storage
        //    , [FromHeader(Name = "upload-type")] string? uploadType
        //    , [FromHeader(Name = "overwrite")] string? overwrite
        //    , [FromForm] IFormFile file
        //    , [FromRoute] string path = ".")
        //{
        //}



        [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue)]
        [RequestSizeLimit(int.MaxValue)]
        [TokenAccessFilter(uploadCheck: true)]
        [HttpPost("/__sws_api/{storage}/upload/{*path}")]
        public async Task<IActionResult> Upload([FromRoute] string storage
            , [FromHeader(Name = "upload-type")] string? uploadType
            , [FromHeader(Name = "overwrite")] string? overwrite
            , [FromForm] IFormFile file
            , [FromRoute] string path = ".")
        {
            if (file == null)
                return BadRequest("required field \"file\" does not set");

            storage = storage.Trim().ToLower();

            var storageFullPath = Path.GetFullPath(Path.Combine("data", "storages", storage));

            var fullUploadPath = Path.Combine(storageFullPath, path);

            var hasOverwrite = overwrite == StorageOverwriteType.Overwrite;
            var hasSkipExists = overwrite == StorageOverwriteType.SkipExists;

            using var uf = file.OpenReadStream();

            if (uploadType == default)
            {
                var ufi = new FileInfo(fullUploadPath);

                if (ufi.FullName.IndexOf(storageFullPath) == -1)
                    return Forbid();

                if (!ufi.Exists || (ufi.Exists && hasOverwrite) || (ufi.Exists && !hasSkipExists))
                {
                    if (!ufi.Directory.Exists)
                        ufi.Directory.Create();

                    using var f = ufi.Create();

                    await uf.CopyToAsync(f);

                    f.Position = 0; // reset position after copy
                    await System.IO.File.WriteAllTextAsync($"{ufi.FullName}._sws__hash",
                        string.Join("", SHA256.HashData(f).Select(x => x.ToString("x2"))));
                }

                return Ok(Enumerable.Repeat(Path.GetRelativePath(storageFullPath, fullUploadPath), 1));
            }
            else if (uploadType == StorageUploadType.Extract)
            {
                using ZipArchive za = new ZipArchive(uf, ZipArchiveMode.Read);

                var list = new List<string>();

                foreach (var f in za.Entries)
                {
                    if (string.IsNullOrEmpty(f.Name)) //folder
                        continue;

                    var ufi = new FileInfo(Path.Combine(fullUploadPath, f.FullName));

                    if (ufi.FullName.IndexOf(storageFullPath) == -1)
                        return Forbid();

                    list.Add(Path.GetRelativePath(storageFullPath, ufi.FullName));

                    if (!ufi.Exists || (ufi.Exists && hasOverwrite) || (ufi.Exists && !hasSkipExists))
                    {
                        if (!ufi.Directory.Exists)
                            ufi.Directory.Create();

                        f.ExtractToFile(ufi.FullName, hasOverwrite);

                        using var fileStream = ufi.OpenRead();
                        await System.IO.File.WriteAllTextAsync($"{ufi.FullName}._sws__hash",
                            string.Join("", SHA256.HashData(fileStream).Select(x => x.ToString("x2"))));
                    }
                }

                return Ok(list);
            }

            return BadRequest($"invalid upload type - '{uploadType}'");
        }
    }
}
