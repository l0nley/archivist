using System;
using System.IO;
using System.Threading.Tasks;

namespace Archivist.Core.Operations.Local
{
    public class DeleteFile : AbstractOperation
    {
        public DeleteFile(Guid id) : base(id)
        {
        }

        public override string Name => "del";

        public override string Description => "deletes files by mask";

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
                var di = new DirectoryInfo(env.LocalPath);
                if (di.Exists)
                {
                    var files = di.GetFiles(mask, SearchOption.TopDirectoryOnly);
                    for (int i = 0; i < files.Length; i++)
                    {
                        files[i].Delete();
                        env.ReportProgress(Id, i + 1, files.Length);
                    }
                }
                env.ReportStatus(Id, OperationStatus.Success);
            }
            return Task.CompletedTask;
        }
    }
}
