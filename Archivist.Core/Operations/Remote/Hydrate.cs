using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Archivist.Core.Operations.Remote
{
    public class Hydrate : AbstractCloudOperation
    {
        public Hydrate(string connectionString, string containerName, Guid id) : base(connectionString, containerName, id)
        {
        }

        public override string Name => "hyd";

        public override string Description => "Hydrate directory or file wich match the mask";

        public override async Task ExecuteAsync(Util.Environment env, string[] parameters)
        {
            if (parameters.Length == 0)
            {
                env.ReportStatus(Id, OperationStatus.Failed, "Operation require mask as parameter");
            }
            else
            {
                var mask = parameters[0];
                var remotePrefix = env.RemotePath;
                var toSkip = remotePrefix.Split("/").Length + 2;
                var entities = new List<string>();
                var regex = new Regex(mask.Replace(".", "[.]").Replace("*", ".*").Replace("?", "."));
                int chunkSize;
                if (parameters.Length >= 2 && int.TryParse(parameters[1], out int maxThreads))
                {
                    chunkSize = maxThreads;
                }
                else
                {
                    chunkSize = Environment.ProcessorCount;
                }
                env.ReportStatus(Id, OperationStatus.InProgress);
                try
                {
                    var container = GetContainer();
                    BlobContinuationToken token = null;
                    var counter = 0;
                    do
                    {
                        var result = await container.ListBlobsSegmentedAsync(remotePrefix, true, BlobListingDetails.Metadata, 200, token, new BlobRequestOptions
                        {
                            ServerTimeout = TimeSpan.FromSeconds(7)
                        }, null);
                        token = result.ContinuationToken;
                        foreach (var item in result.Results.OfType<CloudBlockBlob>())
                        {
                            var itemOrDirectoryName = Uri.UnescapeDataString(item.Uri.Segments.Skip(toSkip).First()).TrimEnd('/');
                            if (regex.IsMatch(itemOrDirectoryName) && item.Properties.StandardBlobTier != StandardBlobTier.Archive)
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

                RunOperations(entities, chunkSize, RunHydration, env);
            }
        }

        private async Task RunHydration(string entity)
        {
            var container = GetContainer();
            await container.GetBlockBlobReference(entity).SetStandardBlobTierAsync(StandardBlobTier.Archive);
        }
    }
}
