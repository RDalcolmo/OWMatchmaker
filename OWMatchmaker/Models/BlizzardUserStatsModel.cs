using Newtonsoft.Json;
using System.Collections.Generic;

namespace OWMatchmaker.Models
{
    public partial class BlizzardUserStatsModel
    {
        [JsonProperty("rating")]
        public int? Rating { get; set; }

        [JsonProperty("ratingIcon")]
        public string RatingIcon { get; set; }

        [JsonProperty("ratings")]
        public IList<Rating> ratings { get; set; }
    }

    public class Rating
    {

        [JsonProperty("level")]
        public int level { get; set; }

        [JsonProperty("role")]
        public string role { get; set; }

        [JsonProperty("roleIcon")]
        public string roleIcon { get; set; }

        [JsonProperty("rankIcon")]
        public string rankIcon { get; set; }
    }
}
