using System;
using System.Collections.Generic;
using System.Linq;
using AzureDataLakeClient.Authentication;
using ADL=Microsoft.Azure.Management.DataLake;
using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.Azure.Management.DataLake.Store.Models;

namespace AzureDataLakeClient.Store
{
    public class StoreFileSystemClient : AccountClientBase
    {
        private ADL.Store.DataLakeStoreFileSystemManagementClient _adls_filesys_rest_client;

        public StoreFileSystemClient(string account, AuthenticatedSession authSession) :
            base(account,authSession)
        {
            _adls_filesys_rest_client = new ADL.Store.DataLakeStoreFileSystemManagementClient(this.AuthenticatedSession.Credentials);
        }

        public IEnumerable<FsFileStatusPage> ListFilesRecursivePaged(FsPath path, ListFilesOptions options)
        {
            var queue = new Queue<FsPath>();
            queue.Enqueue(path);

            while (queue.Count > 0)
            {
                FsPath cur_path = queue.Dequeue();

                foreach (var page in ListFilesPaged(cur_path, options))
                {
                    yield return page;

                    foreach (var item in page.FileItems)
                    {
                        if (item.Type == ADL.Store.Models.FileType.DIRECTORY)
                        {
                            var new_path = cur_path.Append(item.PathSuffix);
                            queue.Enqueue(new_path);
                        }
                    }
                }
            }
        }

        public IEnumerable<FsFileStatusPage> ListFilesPaged(FsPath path, ListFilesOptions options)
        {
            string after = null;
            while (true)
            {
                var result = _adls_filesys_rest_client.FileSystem.ListFileStatus(this.Account, path.ToString(), options.PageSize, after);

                if (result.FileStatuses.FileStatus.Count > 0)
                {
                    var page = new FsFileStatusPage();
                    page.Path = path;

                    page.FileItems = result.FileStatuses.FileStatus.Select(i=>new FsFileStatus(i)).ToList();
                    yield return page;
                    after = result.FileStatuses.FileStatus[result.FileStatuses.FileStatus.Count - 1].PathSuffix;
                }
                else
                {
                    break;
                }

            }
        }
        
        public void CreateDirectory(FsPath path)
        {
            var result = _adls_filesys_rest_client.FileSystem.Mkdirs(this.Account, path.ToString());
        }

        public void Delete(FsPath path)
        {
            var result = _adls_filesys_rest_client.FileSystem.Delete(this.Account, path.ToString());
        }

        public void Delete(FsPath path, bool recursive)
        {
            var result = _adls_filesys_rest_client.FileSystem.Delete(this.Account, path.ToString(), recursive );
        }

        public void CreateFileWithContent(FsPath path, byte[] bytes, CreateFileOptions options)
        {
            var memstream = new System.IO.MemoryStream(bytes);
            _adls_filesys_rest_client.FileSystem.Create(this.Account, path.ToString(),memstream,options.Overwrite);
        }

        public void CreateFileWithContent(FsPath path, string content, CreateFileOptions options)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            this.CreateFileWithContent(path, bytes, options);
        }

        public FsFileStatus GetFileStatus(FsPath path)
        {
            var info = _adls_filesys_rest_client.FileSystem.GetFileStatus(this.Account, path.ToString());
            return new FsFileStatus(info.FileStatus);
        }

        public FsFileStatus TryGetFileInformation(FsPath path)
        {
            try
            {
                var info = _adls_filesys_rest_client.FileSystem.GetFileStatus(this.Account, path.ToString());
                return new FsFileStatus(info.FileStatus);
            }
            catch (Microsoft.Azure.Management.DataLake.Store.Models.AdlsErrorException ex)
            {
                if (ex.Body.RemoteException is Microsoft.Azure.Management.DataLake.Store.Models.AdlsFileNotFoundException || 
                    ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                throw;
            }
        }

        public bool Exists(FsPath path)
        {
            var info = this.TryGetFileInformation(path);
            return (info != null);
        }

        public bool ExistsFile(FsPath path)
        {
            var filestat = this.TryGetFileInformation(path);
            if (filestat == null)
            {
                return false;
            }

            if (filestat.Type == FileType.DIRECTORY)
            {
                return false;
            }

            return true;

        }

        public bool ExistsFolder(FsPath path)
        {
            var info = this.TryGetFileInformation(path);
            if (info == null)
            {
                return false;
            }


            if (info.Type == FileType.FILE)
            {
                return false;
            }

            return true;

        }


        public FsAcl GetPermissions(FsPath path)
        {
            var acl_result = this._adls_filesys_rest_client.FileSystem.GetAclStatus(this.Account, path.ToString());
            var acl_status = acl_result.AclStatus;

            var fs_acl = new FsAcl(acl_status);

            return fs_acl;
        }

        public void ModifyACLs(FsPath path, FsAclEntry entry)
        {
            this._adls_filesys_rest_client.FileSystem.ModifyAclEntries(this.Account,  path.ToString(), entry.ToString());
        }

        public void ModifyACLs(FsPath path, IEnumerable<FsAclEntry> entries)
        {
            var s = FsAclEntry.EntriesToString(entries);
            this._adls_filesys_rest_client.FileSystem.ModifyAclEntries(this.Account, path.ToString(), s);
        }

        public void SetACLs(FsPath path, IEnumerable<FsAclEntry> entries)
        {
            var s = FsAclEntry.EntriesToString(entries);
            this._adls_filesys_rest_client.FileSystem.SetAcl(this.Account, path.ToString(), s);
        }

        public void RemoveAcl(FsPath path)
        {
            this._adls_filesys_rest_client.FileSystem.RemoveAcl(this.Account, path.ToString());
        }

        public void RemoveDefaultAcl(FsPath path)
        {
            this._adls_filesys_rest_client.FileSystem.RemoveDefaultAcl(this.Account, path.ToString());
        }

        public System.IO.Stream OpenFileForReadBinary(FsPath path)
        {
            return this._adls_filesys_rest_client.FileSystem.Open(this.Account, path.ToString());
        }

        public System.IO.StreamReader OpenFileForReadText(FsPath path)
        {
            var s = this._adls_filesys_rest_client.FileSystem.Open(this.Account, path.ToString());
            return new System.IO.StreamReader(s);
        }

        public System.IO.Stream OpenFileForReadBinary(FsPath path, long offset, long bytesToRead)
        {
            return this._adls_filesys_rest_client.FileSystem.Open(this.Account, path.ToString(), bytesToRead, offset);
        }

        public void Upload(LocalPath src_path, FsPath dest_path, UploadOptions options)
        {
            var parameters = new ADL.StoreUploader.UploadParameters(src_path.ToString(), dest_path.ToString(), this.Account, isOverwrite: options.Force);
            var frontend = new ADL.StoreUploader.DataLakeStoreFrontEndAdapter(this.Account, this._adls_filesys_rest_client);
            var uploader = new ADL.StoreUploader.DataLakeStoreUploader(parameters, frontend);
            uploader.Execute();
        }

        public void Download(FsPath src_path, LocalPath dest_path, DownloadOptions options)
        {
            using (var stream = this._adls_filesys_rest_client.FileSystem.Open(this.Account, src_path.ToString()))
            {
                var filemode = options.Append ? System.IO.FileMode.Append : System.IO.FileMode.Create;
                using (var fileStream = new System.IO.FileStream(dest_path.ToString(), filemode))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }

        public void AppendString(FsFileStatusPage file,string content)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            using (var stream = new System.IO.MemoryStream(bytes))
            {
                this._adls_filesys_rest_client.FileSystem.Append(this.Account, file.ToString(), stream);
            }
        }

        public void AppendBytes(FsFileStatusPage file, byte[] bytes)
        {
            using (var stream = new System.IO.MemoryStream(bytes))
            {
                this._adls_filesys_rest_client.FileSystem.Append(this.Account, file.ToString(), stream);
            }
        }

        public void Concatenate(IEnumerable<FsPath> src_paths, FsPath dest_path)
        {
            var src_file_strings = src_paths.Select(i => i.ToString()).ToList();
            this._adls_filesys_rest_client.FileSystem.Concat(this.Account, dest_path.ToString(), src_file_strings);
        }

        public void ClearFileExpiry(FsPath path)
        {
            this._adls_filesys_rest_client.FileSystem.SetFileExpiry(this.Account, path.ToString(), ExpiryOptionType.NeverExpire, null);
        }

        public void SetFileExpiryAbsolute(FsPath path, System.DateTimeOffset expiretime)
        {
            var ut = new FsUnixTime(expiretime);
            var unix_time = ut.MillisecondsSinceEpoch;
            this._adls_filesys_rest_client.FileSystem.SetFileExpiry(this.Account, path.ToString(), ExpiryOptionType.Absolute, unix_time);
        }

        public void SetFileExpiryRelativeToNow(FsPath path, System.TimeSpan timespan)
        {
            this._adls_filesys_rest_client.FileSystem.SetFileExpiry(this.Account, path.ToString(), ExpiryOptionType.RelativeToNow, (long) timespan.TotalMilliseconds);
        }

        public void SetFileExpiryRelativeToCreationDate(FsPath path, System.TimeSpan timespan)
        {
            this._adls_filesys_rest_client.FileSystem.SetFileExpiry(this.Account, path.ToString(), ExpiryOptionType.RelativeToCreationDate, (long) timespan.TotalMilliseconds);
        }

        public ContentSummary GetContentSummary(FsPath path)
        {
            var summary = this._adls_filesys_rest_client.FileSystem.GetContentSummary(this.Account, path.ToString());
            return summary.ContentSummary;
        }

        public void SetOwner(FsPath path, string owner, string group)
        {
            this._adls_filesys_rest_client.FileSystem.SetOwner(this.Account, path.ToString(), owner, group);
        }
        

        public void Move(FsPath src_path, FsPath dest_path)
        {
            this._adls_filesys_rest_client.FileSystem.Rename(this.Account, src_path.ToString(), dest_path.ToString());
        }
    }
}