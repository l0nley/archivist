using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Archivist.Core.Operations.Remote
{
    public class Download : AbstractCloudOperation
    {
        public Download(string connectionString, string containerName, Guid id) : base(connectionString, containerName, id)
        {
        }

        public override string Name => "down";

        public override string Description => "Downloads files and directories";

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
                var entities = new List<Tuple<string,string,string>>();
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
                                var remoteVirtualPath = item.Uri.Segments.Skip(toSkip).Select(_ => Uri.UnescapeDataString(_)).ToArray();
                                var itemFileName = remoteVirtualPath.Last();
                                var localFolder = env.LocalPath;
                                if (remoteVirtualPath.Length > 1)
                                {
                                    localFolder = Path.Combine(localFolder, string.Join(Path.DirectorySeparatorChar, remoteVirtualPath.Take(remoteVirtualPath.Length - 1)));
                                }
                                entities.Add(new Tuple<string, string, string>(item.Name, localFolder, itemFileName));
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

                RunOperations(entities, chunkSize, RunDownload, env);
            }
        }

        public async Task RunDownload(Tuple<string, string, string> entity)
        {
            var container = GetContainer();
            var blob = container.GetBlockBlobReference(entity.Item1);
            var di = new DirectoryInfo(entity.Item2);
            if(di.Exists == false)
            {
                di.Create();
            }
            await blob.DownloadToFileAsync(Path.Combine(entity.Item2, entity.Item3), FileMode.Create, null, new BlobRequestOptions
            {
                UseTransactionalMD5 = true,
                DisableContentMD5Validation = false
            }, null);
        }
    }
}
