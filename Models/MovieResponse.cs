using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace lndev_flix.Models
{
    public class MovieResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
        [JsonProperty("genre")]
        public string Genre { get; set; } = string.Empty;
        [JsonProperty("imageUri")]
        public string ImageUri { get; set; } = string.Empty;
        [JsonProperty("videoUri")]
        public string VideoUri { get; set; } = string.Empty;
    }
}