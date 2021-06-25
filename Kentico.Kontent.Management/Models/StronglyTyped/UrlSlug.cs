using Newtonsoft.Json;

namespace Kentico.Kontent.Management.Models.StronglyTyped
{
    /// <summary>
    /// Represents strongly typed url  slug element
    /// </summary>
    public class UrlSlug
    {
        /// <summary>
        /// Gets or sets mode of the url slug.
        /// </summary>
        [JsonProperty("mode")]
        public string Mode { get; set; }

        /// <summary>
        /// Gets or sets value of the url slug.
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
