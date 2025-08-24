
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BatchApp.Services
{
    public class GcsFileService
    {
        public async Task DeleteFileAsync(string objectName)
        {
            await _storageClient.DeleteObjectAsync(_bucketName, objectName);
            _logger.LogInformation("Deleted {ObjectName} from bucket {Bucket}", objectName, _bucketName);
        }
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;
        private readonly ILogger<GcsFileService> _logger;

        public GcsFileService(string bucketName, ILogger<GcsFileService> logger)
        {
            _storageClient = StorageClient.Create();
            _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
            _logger = logger;
        }

        public async Task<List<string>> ListFilesAsync(string prefix = "")
        {
            var files = new List<string>();
            await foreach (var obj in _storageClient.ListObjectsAsync(_bucketName, prefix))
            {
                if (!obj.Name.EndsWith("/")) // skip folders
                    files.Add(obj.Name);
            }
            return files;
        }

        public async Task<Stream> ReadFileAsync(string objectName)
        {
            var ms = new MemoryStream();
            await _storageClient.DownloadObjectAsync(_bucketName, objectName, ms);
            ms.Position = 0;
            return ms;
        }

        public async Task UploadFileAsync(string objectName, Stream data, string contentType = "application/octet-stream")
        {
            await _storageClient.UploadObjectAsync(_bucketName, objectName, contentType, data);
            _logger.LogInformation("Uploaded {ObjectName} to bucket {Bucket}", objectName, _bucketName);
        }
    }
}
