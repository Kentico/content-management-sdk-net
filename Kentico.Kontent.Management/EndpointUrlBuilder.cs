using Kentico.Kontent.Management.Models.Assets;
using Kentico.Kontent.Management.Models.Items;
using System;
using System.Net;

namespace Kentico.Kontent.Management
{
    internal sealed class EndpointUrlBuilder
    {
        private const int URI_MAX_LENGTH = 65519;

        private const string URL_ITEM = "/items";
        private const string URL_TEMPLATE_ITEM_ID = "/items/{0}";
        private const string URL_TEMPLATE_ITEM_EXTERNAL_ID = "/items/external-id/{0}";
        private const string URL_TEMPLATE_ITEM_CODENAME = "/items/codename/{0}";

        private const string URL_VARIANT = "/variants";
        private const string URL_TEMPLATE_VARIANT_ID = "/variants/{0}";
        private const string URL_TEMPLATE_VARIANT_CODENAME = "/variants/codename/{0}";

        private const string URL_ASSET = "/assets";
        private const string URL_TEMPLATE_ASSET_ID = "/assets/{0}";
        private const string URL_TEMPLATE_ASSET_EXTERNAL_ID = "/assets/external-id/{0}";

        private const string URL_ASSET_FOLDERS = "/folders";

        private const string URL_TEMPLATE_FILE_FILENAME = "/files/{0}";

        private const string URL_VALIDATE = "/validate";

        private readonly ManagementOptions _options;

        internal EndpointUrlBuilder(ManagementOptions options)
        {
            _options = options;
        }

        #region Variants

        internal string BuildListVariantsUrl(ContentItemIdentifier identifier)
        {
            var itemSegment = GetItemUrlSegment(identifier);

            return GetUrl(string.Concat(itemSegment, URL_VARIANT));
        }

        internal string BuildVariantsUrl(ContentItemVariantIdentifier identifier)
        {
            var itemSegment = GetItemUrlSegment(identifier.ItemIdentifier);
            var variantSegment = GetVariantUrlSegment(identifier.LanguageIdentifier);

            return GetUrl(string.Concat(itemSegment, variantSegment));
        }

        private string GetVariantUrlSegment(LanguageIdentifier identifier)
        {
            if (!string.IsNullOrEmpty(identifier.Codename))
            {
                return string.Format(URL_TEMPLATE_VARIANT_CODENAME, identifier.Codename);
            }

            return string.Format(URL_TEMPLATE_VARIANT_ID, identifier.Id);
        }

        #endregion

        #region Items

        internal string BuildItemsListingUrl(string continuationToken = null)
        {
            return (continuationToken != null) ? GetUrl(URL_ITEM, $"continuationToken={Uri.EscapeDataString(continuationToken)}") : GetUrl(URL_ITEM);
        }

        internal string BuildItemsUrl()
        {
            return GetUrl(URL_ITEM);
        }

        internal string BuildItemUrl(ContentItemIdentifier identifier)
        {
            var itemSegment = GetItemUrlSegment(identifier);
            return GetUrl(itemSegment);
        }

        private string GetItemUrlSegment(ContentItemIdentifier identifier)
        {
            if (identifier.Id != null)
            {
                return string.Format(URL_TEMPLATE_ITEM_ID, identifier.Id);
            }

            if (!string.IsNullOrEmpty(identifier.Codename))
            {
                return string.Format(URL_TEMPLATE_ITEM_CODENAME, identifier.Codename);
            }

            if (!string.IsNullOrEmpty(identifier.ExternalId))
            {
                return BuildItemUrlSegmentFromExternalId(identifier.ExternalId);
            }
            throw new ArgumentException("You must provide item's id, codename or externalId");
        }

        internal string BuildItemUrlSegmentFromExternalId(string externalId)
        {
            var escapedExternalId = WebUtility.UrlEncode(externalId);
            return string.Format(URL_TEMPLATE_ITEM_EXTERNAL_ID, escapedExternalId);
        }

        #endregion

        #region Assets

        public string BuildAssetListingUrl(string continuationToken = null)
        {
            return (continuationToken != null) ? GetUrl(URL_ASSET, $"continuationToken={Uri.EscapeDataString(continuationToken)}") : GetUrl(URL_ASSET);
        }

        public string BuildAssetsUrl()
        {
            return GetUrl(URL_ASSET);
        }

        public string BuildAssetFoldersUrl()
        {
            return GetUrl(URL_ASSET_FOLDERS);
        }

        public string BuildAssetsUrl(AssetIdentifier identifier)
        {
            if (identifier.Id != null)
            {
                return GetUrl(string.Format(URL_TEMPLATE_ASSET_ID, identifier.Id));
            }

            if (!string.IsNullOrEmpty(identifier.ExternalId))
            {
                return BuildAssetsUrlFromExternalId(identifier.ExternalId);
            }

            throw new ArgumentException("You must provide asset's id, or externalId");
        }

        public string BuildAssetsUrlFromExternalId(string externalId)
        {
            var escapedExternalId = WebUtility.UrlEncode(externalId);
            return GetUrl(string.Format(URL_TEMPLATE_ASSET_EXTERNAL_ID, escapedExternalId));
        }

        #endregion

        #region Binary files

        public string BuildUploadFileUrl(string fileName)
        {
            return GetUrl(string.Format(URL_TEMPLATE_FILE_FILENAME, fileName));
        }

        #endregion

        #region Validation

        public string BuildValidationUrl()
        {
            return GetUrl(URL_VALIDATE);
        }

        #endregion

        private string GetUrl(string path, params string[] parameters)
        {
            var projectSegment = $"projects/{_options.ProjectId}";

            var endpointUrl = string.Format(_options.EndpointV2, projectSegment);
            var url = string.Concat(endpointUrl, path);

            if ((parameters != null) && (parameters.Length > 0))
            {
                var joinedQuery = string.Join("&", parameters);

                if (!string.IsNullOrEmpty(joinedQuery))
                {
                    url = $"{url}?{joinedQuery}";
                }
            }

            if (url.Length > URI_MAX_LENGTH)
            {
                throw new UriFormatException("The request url is too long. Split your query into multiple calls.");
            }

            return url;
        }
    }
}
