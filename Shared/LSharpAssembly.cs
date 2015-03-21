using System.Runtime.Serialization;

namespace LeagueSharp.Loader.Service
{
    [DataContract]
    public class LSharpAssembly
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string PathToBinary { get; set; }
    }
}