using System;
using System.Linq;
using System.Threading.Tasks;

namespace Archivist.Core.Operations.Remote
{
    public class ChangeFolder : AbstractCloudOperation
    {
        public ChangeFolder(string connectionString, string containerName, Guid id) : base(connectionString, containerName, id)
        {
        }

        public override string Name => "rcd";

        public override string Description => "Change remote directory";

        public override Task ExecuteAsync(Util.Environment env, string[] parameters)
        {
            env.ReportStatus(Id, OperationStatus.InProgress);
            if (parameters.Length == 0)
            {
                env.ReportStatus(Id, OperationStatus.Failed, "Operation require directory name parameter");
                return Task.CompletedTask;
            }
            if (parameters[0] == "..")
            {
                if(false == string.IsNullOrWhiteSpace(env.RemotePath))
                {
                    var splitted = env.RemotePath.Split("/");
                    env.RemotePath = string.Join("/", splitted.Take(splitted.Length - 1).ToArray());
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(env.RemotePath))
                {
                    env.RemotePath = parameters[0];
                }
                else
                {
                    env.RemotePath = string.Join("/", env.RemotePath, parameters[0]);
                }
            }
            env.ReportStatus(Id, OperationStatus.Success);
            return Task.CompletedTask;
        }
    }
}
