namespace Archivist.Core.Models
{
    public class Entity
    {
        public string RemoteName { get; set; }
        public string LocalName { get; set; }
        public bool IsDirectory { get; set; }
        public EntityStateRemote RemoteState { get; set; }
        public string DisplayName { get; set; }
    }
}
