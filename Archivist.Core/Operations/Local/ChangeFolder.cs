using System;
using System.IO;
using System.Threading.Tasks;

namespace Archivist.Core.Operations.Local
{
    public class ChangeFolder : AbstractOperation
    {
        public ChangeFolder(Guid id) : base(id)
        {
        }

        public override string Name => "cd";

        public override string Description => "Changes local folder";

        public override Task ExecuteAsync(Util.Environment env, string[] parameters)
        {
            env.ReportStatus(Id, OperationStatus.InProgress);
            if (parameters.Length == 0)
            {
                env.ReportStatus(Id, OperationStatus.Failed, "Operation require new dir name suuplied");
            }
            else
            {
                var newDirName = parameters[0];
                if (newDirName == "..")
                {
                    var di = new DirectoryInfo(env.LocalPath);
                    if (di.Parent != null)
                    {
                        env.LocalPath = di.Parent.FullName;
                    }
                    env.ReportStatus(Id, OperationStatus.Success);
                }
                else
                {
                    var di = new DirectoryInfo(Path.Combine(env.LocalPath, newDirName));
                    if (di.Exists)
                    {
                        env.LocalPath = di.FullName;
                        env.ReportStatus(Id, OperationStatus.Success);
                    }
                    else
                    {
                        env.ReportStatus(Id, OperationStatus.Failed, $"Directory {newDirName} not found.");
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
