using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Archivist.Core.Operations.Remote
{
    public class List : AbstractCloudOperation
    {
        public List(string connectionString, string containerName, Guid id) : base(connectionString, containerName, id)
        {
        }

        public override string Name => "rls";

        public override string Description => "Lists remote directory";

        public override async Task ExecuteAsync(Util.Environment env, string[] parameters)
        {
            env.ReportStatus(Id, OperationStatus.InProgress);
            var container = GetContainer();
            var directory = container.GetDirectoryReference(env.RemotePath);
            BlobContinuationToken token = null;
            var amount = 200;
            var progress = 0;
            var dirs = new List<string>();
            var files = new List<string>();
            do
            {
                var result = await directory.ListBlobsSegmentedAsync(false, BlobListingDetails.Metadata, amount, token, new BlobRequestOptions
                {
                    ServerTimeout = TimeSpan.FromSeconds(7)
                }, null);
                token = result.ContinuationToken;
                var counter = 0;
                var subIndex = 0;
                if (false == string.IsNullOrEmpty(env.RemotePath))
                {
                    subIndex = env.RemotePath.Length + 1;
                }
                foreach (var item in result.Results)
                {
                    counter++;
                    if (item is CloudBlobDirectory)
                    {
                        var dir = item as CloudBlobDirectory;
                        dirs.Add(dir.Prefix.Substring(subIndex));
                    }
                    if (item is CloudBlockBlob)
                    {
                        var file = item as CloudBlockBlob;
                        files.Add($"{ConvertBlobStatus(file)}\t{file.Name.Substring(subIndex)}");
                    }
                }
                progress += counter;
                env.ReportProgress(Id, progress, 0);
            } while (token != null);
            foreach (var item in dirs)
            {
                env.WriteOut($"<RDIR>\t{item}");
            }
            foreach (var item in files)
            {
                env.WriteOut($"{item}");
            }
            env.WriteOut($"{files.Count} file(s), {dirs.Count} directory(ies).");
            env.ReportStatus(Id, OperationStatus.Success);
        }

        private string ConvertBlobStatus(CloudBlockBlob file)
        {
            if (null != file.Properties.RehydrationStatus)
            {
                return file.Properties.RehydrationStatus.GetValueOrDefault().ToString().ToUpper();
            }
            else
            {
                return file.Properties.StandardBlobTier.GetValueOrDefault().ToString().ToUpper();
            }
        }
    }
}
