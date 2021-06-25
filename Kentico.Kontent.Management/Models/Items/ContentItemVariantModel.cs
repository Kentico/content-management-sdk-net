using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kentico.Kontent.Management.Models.Items
{
    /// <summary>
    /// Represents content item variant model.
    /// </summary>
    public sealed class ContentItemVariantModel
    {
        /// <summary>
        /// Gets or sets item of the variant.
        /// </summary>
        [JsonProperty("item")]
        public ObjectIdentifier Item { get; set; }

        /// <summary>
        /// Gets or sets elements of the variant.
        /// </summary>
        [JsonProperty("elements")]
        public IEnumerable<dynamic> Elements { get; set; }

        /// <summary>
        /// Gets or sets language of the variant.
        /// </summary>
        [JsonProperty("language")]
        public LanguageIdentifier Language { get; set; }

        /// <summary>
        /// Gets or sets last modified timestamp of the content item.
        /// </summary>
        [JsonProperty("last_modified")]
        public DateTime? LastModified { get; set; }
    }
}
