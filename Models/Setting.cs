using Newtonsoft.Json;

namespace TSApi.Models
{
    public class Setting
    {
        public int port { get; set; } = 8090;

        public bool IPAddressAny { get; set; } = true;

        [JsonIgnore]
        public string appfolder { get; set; }

        public int worknodetominutes { get; set; } = 4;

        public bool AuthorizationRequired { get; set; }
    }
}
