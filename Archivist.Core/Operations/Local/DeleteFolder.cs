using System;
using System.IO;
using System.Threading.Tasks;

namespace Archivist.Core.Operations.Local
{
    public class DeleteFolder : AbstractOperation
    {
        public DeleteFolder(Guid id) : base(id)
        {
        }

        public override string Name => "rd";

        public override string Description => "Removes local directory and its contents";

        public override Task ExecuteAsync(Util.Environment env, string[] parameters)
        {
            env.ReportStatus(Id, OperationStatus.InProgress);
            if (parameters.Length == 0)
            {
                env.ReportStatus(Id, OperationStatus.Failed, "Operation require directory name or mask");
            }
            else
            {
                var mask = parameters[0];
                var localDir = new DirectoryInfo(env.LocalPath);
                var subdirs = localDir.GetDirectories(mask);
                env.ReportProgress(Id, 0, subdirs.Length);
                for(var i=0; i<subdirs.Length;i++)
                {
                    subdirs[i].Delete(true);
                    env.ReportProgress(Id, i + 1, subdirs.Length);
                }
                env.ReportStatus(Id, OperationStatus.Success);
            }
            return Task.CompletedTask;
        }
    }
}
