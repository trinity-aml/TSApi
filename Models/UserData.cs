using Newtonsoft.Json;

namespace TSApi.Models
{
    public class UserData
    {
        public string login { get; set; }

        [JsonIgnore]
        public string passwd { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string torPath { get; set; }

        public bool allowedToChangeSettings { get; set; }

        public bool IsShared { get; set; }
    }
}
