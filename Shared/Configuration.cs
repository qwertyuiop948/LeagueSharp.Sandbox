using System.Runtime.Serialization;
using System.Security;

namespace LeagueSharp.Loader.Service
{
    [DataContract]
    public class Configuration
    {
        [DataMember]
        public bool AntiAfk { get; set; }

        [DataMember]
        public bool Console { get; set; }

        [DataMember]
        public string DataDirectory { get; set; }

        [DataMember]
        public bool ExtendedZoom { get; set; }

        [DataMember]
        public uint MenuKey { get; set; }

        [DataMember]
        public uint MenuToggleKey { get; set; }

        [DataMember]
        public PermissionSet Permissions { get; set; }

        [DataMember]
        public uint ReloadAndRecompileKey { get; set; }

        [DataMember]
        public uint ReloadKey { get; set; }

        [DataMember]
        public bool TowerRange { get; set; }

        [DataMember]
        public uint UnloadKey { get; set; }
    }
}