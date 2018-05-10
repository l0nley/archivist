using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Archivist.Core.Operations.Remote
{
    public class DeleteFolder : AbstractCloudOperation
    {
        public DeleteFolder(string connectionString, string containerName, Guid id) : base(connectionString, containerName, id)
        {
        }

        public override string Name => "rrd";

        public override string Description => "Removes remote folder and its contents";

        public override async Task ExecuteAsync(Util.Environment env, string[] parameters)
        {
            if (parameters.Length == 0)
            {
                env.ReportStatus(Id, OperationStatus.Failed, "Operation require folder name as parameter");
            }
            else
            {
                string remotePrefix;
                if (string.IsNullOrEmpty(env.RemotePath))
                {
                    remotePrefix = parameters[0];
                }
                else
                {
                    remotePrefix = string.Join('/', env.RemotePath, parameters[0]);
                }
                int chunkSize;
                if (parameters.Length >= 2 && int.TryParse(parameters[1], out int maxThreads))
                {
                    chunkSize = maxThreads;
                }
                else
                {
                    chunkSize = Environment.ProcessorCount;
                }
                var entities = new List<string>();
                env.ReportStatus(Id, OperationStatus.InProgress);
                try
                {
                    var container = GetContainer();
                    var dir = container.GetDirectoryReference(remotePrefix);
                    BlobContinuationToken token = null;
                    var counter = 0;
                    do
                    {
                        var result = await dir.ListBlobsSegmentedAsync(true, BlobListingDetails.Metadata, 200, token, new BlobRequestOptions
                        {
                            ServerTimeout = TimeSpan.FromSeconds(7)
                        }, null);
                        token = result.ContinuationToken;
                        foreach(var item in result.Results.OfType<CloudBlockBlob>())
                        {
                            entities.Add(item.Name);
                            counter++;
                        }
                        env.ReportProgress(Id, counter, 0);
                    } while (token != null);
                }
                catch (Exception ex)
                {
                    env.ReportStatus(Id, OperationStatus.Failed, ex.Message);
                    return;
                }
                RunOperations(entities, chunkSize, RunDelete, env);
            }
        }

        private async Task RunDelete(string entity)
        {
            var container = GetContainer();
            var reference = container.GetBlockBlobReference(entity);
            await reference.DeleteAsync();
        }
    }
}
