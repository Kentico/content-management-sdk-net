using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kentico.Kontent.Management.Models.Items
{
    /// <summary>
    /// Represents content item variant upsert model.
    /// </summary>
    public sealed class ContentItemVariantUpsertModel
    {
        /// <summary>
        /// Gets or sets elements of the variant.
        /// </summary>
        [JsonProperty("elements", Required = Required.Always)]
        public IEnumerable<dynamic> Elements { get; set; }

        /// <summary>
        /// Creates instance of content item variant upsert model.
        /// </summary>
        public ContentItemVariantUpsertModel()
        {
        }

        internal ContentItemVariantUpsertModel(ContentItemVariantModel contentItemVariant)
        {
            Elements = contentItemVariant.Elements;
        }
    }
}
