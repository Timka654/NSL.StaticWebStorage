using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSL.StaticWebStorage.Shared
{
    public static class StorageUploadType
    {
        /// <summary>
        /// Extract the files from the archive.
        /// </summary>
        public const string Extract = "extract";
        
        public const string? None = default;
    }

    public static class StorageOverwriteType
    {

        public const string? None = default;

        /// <summary>
        /// Overwrite the file(/s) if it already exists.
        /// </summary>
        public const string Overwrite = "overwrite";

        /// <summary>
        /// Skip the file(/s) if it already exists.
        /// </summary>
        public const string SkipExists = "skip-exists";
    }
}
