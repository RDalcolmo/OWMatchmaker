using Newtonsoft.Json;

namespace Birthday_Bot.Models
{
    public partial class BlizzardUserStatsModel
    {
        [JsonProperty("rating")]
        public long Rating { get; set; }

        [JsonProperty("ratingIcon")]
        public string RatingIcon { get; set; }

        [JsonProperty("ratings")]
        public object Ratings { get; set; }
    }
}
