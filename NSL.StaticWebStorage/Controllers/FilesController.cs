using Microsoft.AspNetCore.Mvc;
using NSL.StaticWebStorage.Services;
using NSL.StaticWebStorage.Utils.Route;
using System.IO;
using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace NSL.StaticWebStorage.Controllers
{
    public class FilesController(StoragesService storagesService) : ControllerBase
    {
        [TokenAccessFilter(downloadCheck: true)]
        [HttpGet("/{storage}/download/{*path}")]
        public IActionResult Download([FromRoute] string storage, [FromRoute] string path)
        {
            storage = storage.Trim().ToLower();

            var epath = Path.Combine("data", "storage", storage, path);

            if (!System.IO.File.Exists(epath))
                return NotFound();

            return File(System.IO.File.OpenRead(epath), "application/octet-stream", Path.GetFileName(epath));
        }

        [TokenAccessFilter(uploadCheck: true)]
        [HttpPost("/{storage}/delete/{*path}")]
        public IActionResult Delete([FromRoute] string storage, [FromRoute] string path)
        {
            storage = storage.Trim().ToLower();

            var epath = Path.Combine("data", "storage", storage, path);

            if (!System.IO.File.Exists(epath))
                return NotFound();

            System.IO.File.Delete(epath);

            return Ok();
        }

        [TokenAccessFilter(uploadCheck: true)]
        [HttpPost("/{storage}/upload/{*path}")]
        public async Task<IActionResult> Upload([FromRoute] string storage
            , [FromRoute] string path
            , [FromHeader(Name = "upload-type")] string? uploadType
            , [FromForm] IFormFile file)
        {
            storage = storage.Trim().ToLower();

            var fullUploadPath = Path.Combine("data", "storage", storage, path);

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
                return BadRequest($"invalid upload type - {uploadType}");

            return Ok();
        }
    }
}
