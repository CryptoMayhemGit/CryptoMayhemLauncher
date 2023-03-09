using Newtonsoft.Json;

namespace Mayhem.Dal.Dto.Dtos
{
    public class LivepeerAssetDto
    {
        [JsonProperty(PropertyName = "playbackUrl")]
        public string url { get; set; }
    }
}
