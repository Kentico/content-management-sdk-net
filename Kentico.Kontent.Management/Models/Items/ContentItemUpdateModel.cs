using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace Kentico.Kontent.Management.Models.Items
{
    /// <summary>
    /// Represents content item update model.
    /// </summary>
    public sealed class ContentItemUpdateModel
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
        /// A default constructor.
        /// </summary>
        public ContentItemUpdateModel()
        {
        }

        /// <summary>
        /// Instantiates the <see cref="ContentItemUpdateModel"/> using an instance of the <see cref="ContentItemModel"/>.
        /// </summary>
        /// <param name="contentItem"></param>
        public ContentItemUpdateModel(ContentItemModel contentItem)
        {
            Name = contentItem.Name;
        }
    }
}
