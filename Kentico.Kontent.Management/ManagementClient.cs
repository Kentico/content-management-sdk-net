using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Kentico.Kontent.Management.Modules.ActionInvoker;
using Kentico.Kontent.Management.Modules.HttpClient;
using Kentico.Kontent.Management.Models;
using Kentico.Kontent.Management.Models.Assets;
using Kentico.Kontent.Management.Models.Items;
using Kentico.Kontent.Management.Models.StronglyTyped;
using Kentico.Kontent.Management.Modules.ModelBuilders;
using Kentico.Kontent.Management.Models.ProjectReport;
using Kentico.Kontent.Management.Modules.ResiliencePolicy;

namespace Kentico.Kontent.Management
{
    /// <summary>
    /// Executes requests against the Kentico Kontent Content Management API.
    /// </summary>
    public sealed class ManagementClient
    {
        private const int MAX_FILE_SIZE_MB = 100;

        private readonly ActionInvoker _actionInvoker;
        private readonly EndpointUrlBuilder _urlBuilder;
        private readonly IModelProvider _modelProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagementClient"/> class for managing content of the specified project.
        /// </summary>
        /// <param name="elementProvider"></param>
        /// <param name="ManagementOptions">The settings of the Kentico Kontent project.</param>
        public ManagementClient(IElementProvider elementProvider, ManagementOptions ManagementOptions)
        {
            if (ManagementOptions == null)
            {
                throw new ArgumentNullException(nameof(ManagementOptions));
            }

            if (string.IsNullOrEmpty(ManagementOptions.ProjectId))
            {
                throw new ArgumentException("Kentico Kontent project identifier is not specified.", nameof(ManagementOptions.ProjectId));
            }

            if (!Guid.TryParse(ManagementOptions.ProjectId, out _))
            {
                throw new ArgumentException($"Provided string is not a valid project identifier ({ManagementOptions.ProjectId}). Haven't you accidentally passed the API key instead of the project identifier?", nameof(ManagementOptions.ProjectId));
            }

            if (string.IsNullOrEmpty(ManagementOptions.ApiKey))
            {
                throw new ArgumentException("The API key is not specified.", nameof(ManagementOptions.ApiKey));
            }

            _urlBuilder = new EndpointUrlBuilder(ManagementOptions);
            _actionInvoker = new ActionInvoker(
                new ManagementHttpClient(new DefaultResiliencePolicyProvider(ManagementOptions.MaxRetryAttempts), ManagementOptions.EnableResilienceLogic),
                new MessageCreator(ManagementOptions.ApiKey));
            _modelProvider = ManagementOptions.ModelProvider ?? new ModelProvider(elementProvider);
        }

        internal ManagementClient(IElementProvider elementProvider, EndpointUrlBuilder urlBuilder, ActionInvoker actionInvoker, IModelProvider modelProvider = null)
        {
            _urlBuilder = urlBuilder ?? throw new ArgumentNullException(nameof(urlBuilder));
            _actionInvoker = actionInvoker ?? throw new ArgumentNullException(nameof(actionInvoker));
            _modelProvider = modelProvider ?? new ModelProvider(elementProvider);
        }

        #region Variants

        /// <summary>
        /// Returns strongly typed listing of content item variants for specified content item.
        /// </summary>
        /// <param name="identifier">The identifier of the content item.</param>
        /// <returns>The <see cref="IEnumerable{ContentItemVariantModel}"/> instance that represents the listing of content item variants.</returns>
        public async Task<IEnumerable<ContentItemVariantModel>> ListContentItemVariantsAsync(ContentItemIdentifier identifier)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            var endpointUrl = _urlBuilder.BuildListVariantsUrl(identifier);
            var response = await _actionInvoker.InvokeReadOnlyMethodAsync<IEnumerable<ContentItemVariantModel>>(endpointUrl, HttpMethod.Get);

            return response;
        }

        /// <summary>
        /// Returns strongly typed content item variant.
        /// </summary>
        /// <param name="identifier">The identifier of the content item variant.</param>
        /// <returns>The <see cref="ContentItemVariantModel"/> instance that represents content item variant.</returns>
        public async Task<ContentItemVariantModel> GetContentItemVariantAsync(ContentItemVariantIdentifier identifier)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            var endpointUrl = _urlBuilder.BuildVariantsUrl(identifier);
            var response = await _actionInvoker.InvokeReadOnlyMethodAsync<ContentItemVariantModel>(endpointUrl, HttpMethod.Get);

            return response;
        }

        /// <summary>
        /// Inserts or updates given content item variant.
        /// </summary>
        /// <param name="identifier">The identifier of the content item variant.</param>
        /// <param name="contentItemVariantUpsertModel">Represents inserted or updated content item variant.</param>
        /// <returns>The <see cref="ContentItemVariantModel"/> instance that represents inserted or updated content item variant.</returns>
        public async Task<ContentItemVariantModel> UpsertContentItemVariantAsync(ContentItemVariantIdentifier identifier, ContentItemVariantUpsertModel contentItemVariantUpsertModel)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (contentItemVariantUpsertModel == null)
            {
                throw new ArgumentNullException(nameof(contentItemVariantUpsertModel));
            }

            var endpointUrl = _urlBuilder.BuildVariantsUrl(identifier);
            var response = await _actionInvoker.InvokeMethodAsync<ContentItemVariantUpsertModel, ContentItemVariantModel>(endpointUrl, HttpMethod.Put, contentItemVariantUpsertModel);

            return response;
        }

        /// <summary>
        /// Deletes given content item variant.
        /// </summary>
        /// <param name="identifier">The identifier of the content item variant.</param>
        public async Task DeleteContentItemVariantAsync(ContentItemVariantIdentifier identifier)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            var endpointUrl = _urlBuilder.BuildVariantsUrl(identifier);
            await _actionInvoker.InvokeMethodAsync(endpointUrl, HttpMethod.Delete);
        }

        #endregion

        #region Strongly typed Variants

        /// <summary>
        /// Returns strongly typed listing of content item variants with strongly typed elements for specified content item.
        /// </summary>
        /// <typeparam name="T">Type of the content item elements</typeparam>
        /// <param name="identifier">The identifier of the content item.</param>
        /// <returns>A strongly-typed collection with content item variants.</returns>
        public async Task<List<ContentItemVariantModel<T>>> ListContentItemVariantsAsync<T>(ContentItemIdentifier identifier) where T : new()
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            var endpointUrl = _urlBuilder.BuildListVariantsUrl(identifier);
            var response = await _actionInvoker.InvokeReadOnlyMethodAsync<List<ContentItemVariantModel>>(endpointUrl, HttpMethod.Get);

            return response.Select(x => _modelProvider.GetContentItemVariantModel<T>(x)).ToList();
        }

        /// <summary>
        /// Returns strongly typed content item variant with strongly typed elements.
        /// </summary>
        /// <typeparam name="T">Type of the content item elements</typeparam>
        /// <param name="identifier">The identifier of the content item variant.</param>
        /// <returns>The <see cref="ContentItemVariantModel{T}"/> instance that represents content item variant.</returns>
        public async Task<ContentItemVariantModel<T>> GetContentItemVariantAsync<T>(ContentItemVariantIdentifier identifier) where T : new()
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            var endpointUrl = _urlBuilder.BuildVariantsUrl(identifier);
            var response = await _actionInvoker.InvokeReadOnlyMethodAsync<ContentItemVariantModel>(endpointUrl, HttpMethod.Get);

            return _modelProvider.GetContentItemVariantModel<T>(response);
        }

        /// <summary>
        /// Inserts or updates given content item variant.
        /// </summary>
        /// <typeparam name="T">Type of the content item elements</typeparam>
        /// <param name="identifier">The identifier of the content item variant.</param>
        /// <param name="variantElements">Represents inserted or updated  strongly typed content item variant elements.</param>
        /// <returns>The <see cref="ContentItemVariantModel{T}"/> instance that represents inserted or updated content item variant.</returns>
        public async Task<ContentItemVariantModel<T>> UpsertContentItemVariantAsync<T>(ContentItemVariantIdentifier identifier, T variantElements) where T : new()
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (variantElements == null)
            {
                throw new ArgumentNullException(nameof(variantElements));
            }

            var endpointUrl = _urlBuilder.BuildVariantsUrl(identifier);
            var variantUpsertModel = _modelProvider.GetContentItemVariantUpsertModel(variantElements);
            var response = await _actionInvoker.InvokeMethodAsync<ContentItemVariantUpsertModel, ContentItemVariantModel>(endpointUrl, HttpMethod.Put, variantUpsertModel);

            return _modelProvider.GetContentItemVariantModel<T>(response);
        }

        #endregion

        #region Items

        /// <summary>
        /// Updates given content item.
        /// </summary>
        /// <param name="identifier">The identifier of the content item.</param>
        /// <param name="contentItem">Represents updated content item.</param>
        /// <returns>The <see cref="ContentItemModel"/> instance that represents updated content item.</returns>
        public async Task<ContentItemModel> UpdateContentItemAsync(ContentItemIdentifier identifier, ContentItemUpdateModel contentItem)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (contentItem == null)
            {
                throw new ArgumentNullException(nameof(contentItem));
            }

            var endpointUrl = _urlBuilder.BuildItemUrl(identifier);
            var response = await _actionInvoker.InvokeMethodAsync<ContentItemUpdateModel, ContentItemModel>(endpointUrl, HttpMethod.Put, contentItem);

            return response;
        }

        /// <summary>
        /// Inserts or updates content item according to external identifier.
        /// </summary>
        /// <param name="externalId">The external identifier of the content item.</param>
        /// <param name="contentItem">Represents inserted or updated content item.</param>
        /// <returns>The <see cref="ContentItemModel"/> instance that represents inserted or updated content item.</returns>
        public async Task<ContentItemModel> UpsertContentItemByExternalIdAsync(string externalId, ContentItemUpsertModel contentItem)
        {
            if (string.IsNullOrEmpty(externalId))
            {
                throw new ArgumentException("The external id is not specified.", nameof(externalId));
            }

            if (contentItem == null)
            {
                throw new ArgumentNullException(nameof(contentItem));
            }

            var endpointUrl = _urlBuilder.BuildItemUrl(ContentItemIdentifier.ByExternalId(externalId));
            var response = await _actionInvoker.InvokeMethodAsync<ContentItemUpsertModel, ContentItemModel>(endpointUrl, HttpMethod.Put, contentItem);

            return response;
        }

        /// <summary>
        /// Creates content item.
        /// </summary>
        /// <param name="contentItem">Represents content item which will be created.</param>
        /// <returns>The <see cref="ContentItemModel"/> instance that represents created content item.</returns>
        public async Task<ContentItemModel> CreateContentItemAsync(ContentItemCreateModel contentItem)
        {
            if (contentItem == null)
            {
                throw new ArgumentNullException(nameof(contentItem));
            }

            var endpointUrl = _urlBuilder.BuildItemsUrl();
            var response = await _actionInvoker.InvokeMethodAsync<ContentItemCreateModel, ContentItemModel>(endpointUrl, HttpMethod.Post, contentItem);

            return response;
        }

        /// <summary>
        /// Returns strongly typed content item.
        /// </summary>
        /// <param name="identifier">The identifier of the content item.</param>
        /// <returns>The <see cref="ContentItemModel"/> instance that represents requested content item.</returns>
        public async Task<ContentItemModel> GetContentItemAsync(ContentItemIdentifier identifier)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            var endpointUrl = _urlBuilder.BuildItemUrl(identifier);
            var response = await _actionInvoker.InvokeReadOnlyMethodAsync<ContentItemModel>(endpointUrl, HttpMethod.Get);

            return response;
        }

        /// <summary>
        /// Deletes given content item.
        /// </summary>
        /// <param name="identifier">The identifier of the content item.</param>
        public async Task DeleteContentItemAsync(ContentItemIdentifier identifier)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            var endpointUrl = _urlBuilder.BuildItemUrl(identifier);

            await _actionInvoker.InvokeMethodAsync(endpointUrl, HttpMethod.Delete);
        }

        /// <summary>
        /// Returns strongly typed listing of content items. 
        /// The Content management API returns a dynamically paginated listing response limited to up to 100 objects. 
        /// To check if the next page is available use <see cref="ListingResponseModel{T}.HasNextPage"/>,
        /// For getting next page use <see cref="ListingResponseModel{T}.GetNextPage"/>,
        /// </summary>
        /// <returns>The <see cref="ListingResponseModel{ContentItemModel}"/> instance that represents the listing of content items.</returns>
        public async Task<ListingResponseModel<ContentItemModel>> ListContentItemsAsync()
        {
            var endpointUrl = _urlBuilder.BuildItemsUrl();
            var response = await _actionInvoker.InvokeReadOnlyMethodAsync<ContentItemListingResponseServerModel>(endpointUrl, HttpMethod.Get);

            return new ListingResponseModel<ContentItemModel>(GetNextItemsListingPageAsync, response.Pagination?.Token, response.Items);
        }

        private async Task<IListingResponse<ContentItemModel>> GetNextItemsListingPageAsync(string continuationToken)
        {
            var endpointUrl = _urlBuilder.BuildItemsListingUrl(continuationToken);
            return await _actionInvoker.InvokeReadOnlyMethodAsync<ContentItemListingResponseServerModel>(endpointUrl, HttpMethod.Get);
        }

        #endregion

        #region Assets

        /// <summary>
        /// Returns strongly typed listing of assets.
        /// The Content management API returns a dynamically paginated listing response limited to up to 100 objects. 
        /// To check if the next page is available use <see cref="ListingResponseModel{T}.HasNextPage"/>,
        /// For getting next page use <see cref="ListingResponseModel{T}.GetNextPage"/>,
        /// </summary>
        /// <returns>The <see cref="ListingResponseModel{AssetModel}"/> instance that represents the listing of assets.</returns>
        public async Task<ListingResponseModel<AssetModel>> ListAssetsAsync()
        {
            var endpointUrl = _urlBuilder.BuildAssetsUrl();
            var response = await _actionInvoker.InvokeReadOnlyMethodAsync<AssetListingResponseServerModel>(endpointUrl, HttpMethod.Get);

            return new ListingResponseModel<AssetModel>(GetNextAssetListingPageAsync, response.Pagination?.Token, response.Assets);
        }

        private async Task<IListingResponse<AssetModel>> GetNextAssetListingPageAsync(string continuationToken)
        {
            var endpointUrl = _urlBuilder.BuildAssetsUrl();
            var headers = new Dictionary<string, string>();
            headers.Add("x-continuation", continuationToken);
            var response = await _actionInvoker.InvokeReadOnlyMethodAsync<AssetListingResponseServerModel>(endpointUrl, HttpMethod.Get, headers);

            return response;
        }

        /// <summary>
        /// Returns strongly typed asset.
        /// </summary>
        /// <param name="identifier">The identifier of the asset.</param>
        /// <returns>The <see cref="AssetModel"/> instance that represents requested asset.</returns>
        public async Task<AssetModel> GetAssetAsync(AssetIdentifier identifier)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            var endpointUrl = _urlBuilder.BuildAssetsUrl(identifier);
            var response = await _actionInvoker.InvokeReadOnlyMethodAsync<AssetModel>(endpointUrl, HttpMethod.Get);

            return response;
        }

        /// <summary>
        /// Get the Asset Folders
        /// </summary>
        /// <returns>Returns the hierarchy of asset folders beginning with the root level</returns>
        public async Task<AssetFolderList> GetAssetFoldersAsync()
        {
            var endpointUrl = _urlBuilder.BuildAssetFoldersUrl();
            var response = await _actionInvoker.InvokeReadOnlyMethodAsync<AssetFolderList>(endpointUrl, HttpMethod.Get);

            return response;
        }

        /// <summary>
        /// Updates given asset.
        /// </summary>
        /// <param name="identifier">The identifier of the asset.</param>
        /// <param name="asset">Represents updated asset.</param>
        /// <returns>The <see cref="AssetModel"/> instance that represents updated asset.</returns>
        public async Task<AssetModel> UpdateAssetAsync(AssetIdentifier identifier, AssetUpdateModel asset)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            var endpointUrl = _urlBuilder.BuildAssetsUrl(identifier);
            var response = await _actionInvoker.InvokeMethodAsync<AssetUpdateModel, AssetModel>(endpointUrl, HttpMethod.Put, asset);

            return response;
        }

        /// <summary>
        /// Deletes given asset.
        /// </summary>
        /// <param name="identifier">The identifier of the asset.</param>
        public async Task DeleteAssetAsync(AssetIdentifier identifier)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            var endpointUrl = _urlBuilder.BuildAssetsUrl(identifier);
            await _actionInvoker.InvokeMethodAsync(endpointUrl, HttpMethod.Delete);
        }

        /// <summary>
        /// Creates asset.
        /// </summary>
        /// <param name="asset">Represents asset which will be created.</param>
        /// <returns>The <see cref="AssetModel"/> instance that represents created asset.</returns>
        public async Task<AssetModel> CreateAssetAsync(AssetUpsertModel asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            var endpointUrl = _urlBuilder.BuildAssetsUrl();
            var response = await _actionInvoker.InvokeMethodAsync<AssetUpsertModel, AssetModel>(endpointUrl, HttpMethod.Post, asset);

            return response;
        }

        /// <summary>
        /// Inserts or updates asset according to external identifier.
        /// </summary>
        /// <param name="externalId">The external identifier of the content item.</param>
        /// <param name="asset">Represents asset which will be created.</param>
        /// <returns>The <see cref="AssetModel"/> instance that represents inserted or updated asset.</returns>
        public async Task<AssetModel> UpsertAssetByExternalIdAsync(string externalId, AssetUpsertModel asset)
        {
            if (string.IsNullOrEmpty(externalId))
            {
                throw new ArgumentException("The external id is not specified.", nameof(externalId));
            }

            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            var endpointUrl = _urlBuilder.BuildAssetsUrlFromExternalId(externalId);
            var response = await _actionInvoker.InvokeMethodAsync<AssetUpsertModel, AssetModel>(
                endpointUrl,
                HttpMethod.Put,
                asset
            );

            return response;
        }

        #endregion

        #region Binary files

        /// <summary>
        /// Uploads the given file.
        /// </summary>
        /// <param name="fileContent">Represents the content of the file</param>
        /// <returns>The <see cref="FileReference"/> instance that represents the reference to created file.</returns>
        public async Task<FileReference> UploadFileAsync(FileContentSource fileContent)
        {
            if (fileContent == null)
            {
                throw new ArgumentNullException(nameof(fileContent));
            }

            var stream = fileContent.OpenReadStream();
            try
            {
                if (stream.Length > MAX_FILE_SIZE_MB * 1024 * 1024)
                {
                    throw new ArgumentException($"Maximum supported file size is {MAX_FILE_SIZE_MB} MB.", nameof(stream));
                }

                var endpointUrl = _urlBuilder.BuildUploadFileUrl(fileContent.FileName);
                var response = await _actionInvoker.UploadFileAsync<FileReference>(endpointUrl, stream, fileContent.ContentType);

                return response;
            }
            finally
            {
                // Dispose the stream only in case new stream was created
                if (fileContent.CreatesNewStream)
                {
                    stream.Dispose();
                }
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates the project.
        /// </summary>
        /// <returns><see cref="ProjectReportModel"/></returns>
        public async Task<ProjectReportModel> ValidateProjectAsync()
        {
            var endpointUrl = _urlBuilder.BuildValidationUrl();
            return await _actionInvoker.InvokeReadOnlyMethodAsync<ProjectReportModel>(endpointUrl, HttpMethod.Post);
        }

        #endregion
    }
}
