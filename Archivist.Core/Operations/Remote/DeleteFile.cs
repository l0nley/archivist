using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Archivist.Core.Operations.Remote
{
    public class DeleteFile : AbstractCloudOperation
    {
        public DeleteFile(string connectionString, string containerName, Guid id) : base(connectionString, containerName, id)
        {
        }

        public override string Name => "rdel";

        public override string Description => "Deletes remote file from storage";

        public override async Task ExecuteAsync(Util.Environment env, string[] parameters)
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
                var regex = new Regex(mask.Replace(".", "[.]").Replace("*", ".*").Replace("?", "."));
                var entities = new List<string>();
                try
                {
                    var container = GetContainer();
                    var di = container.GetDirectoryReference(env.RemotePath);
                    BlobContinuationToken token = null;
                    var counter = 0;
                    do
                    {
                        var result = await di.ListBlobsSegmentedAsync(false, BlobListingDetails.Metadata, 200, token, new BlobRequestOptions
                        {
                            ServerTimeout = TimeSpan.FromSeconds(7)
                        }, null);
                        token = result.ContinuationToken;
                        foreach (var item in result.Results.OfType<CloudBlockBlob>())
                        {

                            var fileName = item.Name.Substring(env.RemotePath.Length).TrimStart('/');
                            if (regex.IsMatch(fileName))
                            {
                                entities.Add(item.Name);
                                counter++;
                                env.ReportProgress(Id, counter, 0);
                            }
                        }
                    } while (token != null);
                }
                catch (Exception e)
                {
                    env.ReportStatus(Id, OperationStatus.Failed, e.Message);
                    return;
                }

                RunOperations(entities, chunkSize, RunDelete, env);
            }
        }

        private async Task RunDelete(string entity)
        {
            var container = GetContainer();
            await container.GetBlockBlobReference(entity).DeleteAsync();
        }
    }
}
