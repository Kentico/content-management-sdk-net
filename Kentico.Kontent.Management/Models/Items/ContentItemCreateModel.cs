using System.Collections.Generic;

using Newtonsoft.Json;

namespace Kentico.Kontent.Management.Models.Items
{
    /// <summary>
    /// Represents content item create model.
    /// </summary>
    public sealed class ContentItemCreateModel
    {
        /// <summary>
        /// Gets or sets name of the content item.
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets codename of the content item.
        /// </summary>
        [JsonProperty("codename")]
        public string CodeName { get; set; }

        /// <summary>
        /// Gets or sets type of the content item.
        /// </summary>
        [JsonProperty("type", Required = Required.Always)]
        public ContentTypeIdentifier Type { get; set; }

        /// <summary>
        /// Gets or sets exernal identifier of the content item.
        /// </summary>
        [JsonProperty("external_id")]
        public string ExternalId { get; set; }
    }
}
