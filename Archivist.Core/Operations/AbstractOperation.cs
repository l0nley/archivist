using System;
using System.Threading.Tasks;
using Environment = Archivist.Core.Util.Environment;

namespace Archivist.Core.Operations
{
    public abstract class AbstractOperation
    {
        protected AbstractOperation(Guid id)
        {
            Id = id;
        }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public Guid Id { get; }

        public abstract Task ExecuteAsync(Environment env, string[] parameters);
    }
}
