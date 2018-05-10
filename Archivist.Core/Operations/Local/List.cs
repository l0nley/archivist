using System;
using System.IO;
using System.Threading.Tasks;

namespace Archivist.Core.Operations.Local
{
    public class List : AbstractOperation
    {
        public List(Guid id) : base(id)
        {
        }

        public override string Name => "ls";

        public override string Description => "Lists current directory contents";

        public override Task ExecuteAsync(Util.Environment env, string[] parameters)
        {
            env.ReportStatus(Id, OperationStatus.InProgress);
            var di = new DirectoryInfo(env.LocalPath);
            var dirs = di.GetDirectories();
            var files = di.GetFiles();
            foreach(var dir in dirs)
            {
                env.WriteOut($"<DIR>\t{dir.Name}");
            }
            foreach(var file in files)
            {
                env.WriteOut($"{file.Name}");
            }
            env.WriteOut($"{files.LongLength} file(s), {dirs.LongLength} directory(ies).");
            env.ReportStatus(Id, OperationStatus.Success);
            return Task.CompletedTask;
        }
    }
}
