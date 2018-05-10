using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Archivist.Core.Operations.Remote
{
    public class Upload : AbstractCloudOperation
    {
        public Upload(string connectionString, string containerName, Guid id) : base(connectionString, containerName, id)
        {
        }

        public override string Name => "up";

        public override string Description => "Uploads entity set from local directory to the current cloud directory";

        public override Task ExecuteAsync(Util.Environment env, string[] parameters)
        {
            if (parameters.Length == 0)
            {
                env.ReportStatus(Id, OperationStatus.Failed, "Command require entity name or mask");
            }
            else
            {
                var mask = parameters[0];
                int chunkSize;
                if (parameters.Length >= 2 && int.TryParse(parameters[1], out int maxThreads))
                {
                    chunkSize = maxThreads;
                }
                else
                {
                    chunkSize = Environment.ProcessorCount;
                }
                var di = new DirectoryInfo(env.LocalPath);
                var fileNames = new List<string>();
                var dirs = di.GetDirectories(mask, SearchOption.TopDirectoryOnly);
                var files = di.GetFiles(mask, SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    fileNames.Add(file.FullName);
                }

                foreach (var dir in dirs)
                {
                    var subFiles = dir.GetFiles("*", SearchOption.AllDirectories);
                    foreach (var subFile in subFiles)
                    {
                        fileNames.Add(subFile.FullName);
                    }
                }

                var chunks = fileNames
                                .Select(_ =>
                                {
                                    var relativePath = _.Substring(env.LocalPath.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                                    string fullRemotePath;
                                    if (string.IsNullOrWhiteSpace(env.RemotePath))
                                    {
                                        fullRemotePath = relativePath;
                                    }
                                    else
                                    {
                                        fullRemotePath = string.Join("/", env.RemotePath, relativePath);
                                    }
                                    return new KeyValuePair<string, string>(fullRemotePath, _);
                                });

                RunOperations(chunks, chunkSize, RunUpload, env);
            }

            return Task.CompletedTask;
        }

        private async Task RunUpload(KeyValuePair<string, string> pair)
        {
            var container = GetContainer();
            var blob = container.GetBlockBlobReference(pair.Key);
            await blob.UploadFromFileAsync(pair.Value, null, new BlobRequestOptions
            {
                StoreBlobContentMD5 = true,
                RetryPolicy = new NoRetry()
            }, null);
        }

    }
}
