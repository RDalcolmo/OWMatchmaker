using Newtonsoft.Json;

namespace OWMatchmaker.Models
{
    public partial class BlizzardUserStatsModel
    {
        [JsonProperty("rating")]
        public int? Rating { get; set; }

        [JsonProperty("ratingIcon")]
        public string RatingIcon { get; set; }

        [JsonProperty("ratings")]
        public object Ratings { get; set; }
    }
}
