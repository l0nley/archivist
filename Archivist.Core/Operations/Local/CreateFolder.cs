using System;
using System.IO;
using System.Threading.Tasks;

namespace Archivist.Core.Operations.Local
{
    public class CreateFolder : AbstractOperation
    {
        public CreateFolder(Guid id) : base(id)
        {
        }

        public override string Name => "mkdir";

        public override string Description => "Creates new local directory";

        public override Task ExecuteAsync(Util.Environment env, string[] parameters)
        {
            env.ReportStatus(Id, OperationStatus.InProgress);
            if (parameters.Length == 0)
            {
                env.ReportStatus(Id, OperationStatus.Failed, "Operation require new directory name as parameter");
            }
            else
            {
                var newDirName = parameters[0];
                var di = new DirectoryInfo(Path.Combine(env.LocalPath, newDirName));
                if (di.Exists)
                {
                    env.ReportStatus(Id, OperationStatus.Failed, $"Directory {newDirName} already exists");
                }
                else
                {
                    di.Create();
                    env.ReportStatus(Id, OperationStatus.Success);
                }
            }
            return Task.CompletedTask;
        }
    }
}
