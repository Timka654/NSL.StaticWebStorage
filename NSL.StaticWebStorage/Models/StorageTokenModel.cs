using NSL.StaticWebStorage.Services;
using System;
using System.Text.RegularExpressions;

namespace NSL.StaticWebStorage.Models
{
    public class StorageTokenModel
    {
        public string? Code { get; set; }

        public bool CanDownload { get; set; } = false;

        public bool CanUpload { get; set; } = false;

        public bool CanShareAccess { get; set; } = false;

        public string[]? Storages { get; set; } = null;

        public string[]? Paths { get; set; } = null;

        public DateTime? Expired { get; set; } = null;

        public string StorageOwner { get; set; }

        public string? PathOwner { get; set; }


        public bool IsExpired() { if (Expired == null) return false; return Expired < DateTime.UtcNow; }

        public bool CanStorage(string name) => Storages == null || Storages.Contains(name.ToLower());

        public bool CanPath(string path) => Paths == null || Paths.Any(p => Regex.IsMatch(path, path));

        public bool CheckCode(string code)
        {
            if (IsExpired())
            {
                _removeAction();
                return false;
            }

            if (Code != default && !string.Equals(Code, code))
                return false;

            return true;
        }

        public bool CheckUploadAccess(string path, string storage, string code)
        {
            if (!CanUpload)
                return false;

            return CheckBaseAccess(path, storage, code);
        }

        public bool CheckDownloadAccess(string path, string storage, string code)
        {
            if (!CanDownload)
                return false;

            return CheckBaseAccess(path, storage, code);
        }

        public bool CheckShareAccess(string path, string storage, string code)
        {
            if (!CanShareAccess)
                return false;

            return CheckBaseAccess(path, storage, code);
        }

        private bool CheckBaseAccess(string path, string storage, string code)
        {
            if (IsExpired())
            {
                _removeAction();
                return false;
            }

            if (!CheckCode(code))
                return false;

            if (!CanStorage(storage))
                return false;

            if (!CanPath(path))
                return false;

            return true;
        }

        private Action _removeAction = ()=> { };

        public StorageTokenModel SetRemoveDelegate(Action removeAction)
        {
            _removeAction = removeAction;
            return this;
        }

    }
}
