using Newtonsoft.Json;

namespace Mayhem.Dal.Dto.Dtos
{
    public class LatestVersionDto
    {
        [JsonProperty(PropertyName = "latestGameVersion")]
        public string Version { get; set; }
        public string BuildURL { get; set; }
    }
}
