using Newtonsoft.Json;
using System.Collections.Generic;

namespace TSApi.Models
{
    public class Setting
    {
        public int port { get; set; } = 8090;

        public bool IPAddressAny { get; set; } = true;

        [JsonIgnore]
        public string appfolder { get; set; }

        public int worknodetominutes { get; set; } = 4;

        public int maxiptoIsLockHostOrUser { get; set; } = 8;

        public bool AuthorizationRequired { get; set; } = true;

        public List<string> KnownProxies { get; set; } = new List<string>();
    }
}
