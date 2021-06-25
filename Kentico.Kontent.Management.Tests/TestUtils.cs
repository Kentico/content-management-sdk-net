using System.Threading.Tasks;

using Kentico.Kontent.Management.Models.Items;
using Kentico.Kontent.Management.Tests.Mocks;
using Kentico.Kontent.Management.Modules.ActionInvoker;
using System.Collections.Generic;
using Kentico.Kontent.Management.Tests.Data;

namespace Kentico.Kontent.Management.Tests
{
    public class TestUtils
    {
        /// <summary>
        /// Allows to define whether to run the data against the live endpoint or mocked data or whether to regenerate the mocks.
        /// </summary>
        internal enum TestRunType
        {
            /// <summary>
            /// Runs the test against the live production Management API endpoint.
            /// </summary>
            LiveEndPoint,

            /// <summary>
            /// Runs the test against the live production Management API endpoint and records the data in the /bin/ folder.
            /// To use the newly generated data with the <see cref="TestRunType.MockFromFileSystem"/> configuration, 
            /// it needs to be synced to the /Data/ folder in the project.
            /// Copy to output directory = Copy always is automatically ensured by a wildcard in the .csproj file
            /// </summary>
            LiveEndPoint_SaveToFileSystem,

            /// <summary>
            /// Runs the tests against data stored in the /Data/ folder in the file system.
            /// </summary>
            MockFromFileSystem
        }
        
        internal static ManagementClient CreateManagementClient(ManagementOptions options, TestRunType runType, string testName)
        {
            var elementProvider = new CustomElementProvider();
            if (runType != TestRunType.LiveEndPoint)
            {
                var saveToFileSystem = runType == TestRunType.LiveEndPoint_SaveToFileSystem;
                var httpClient = new FileSystemHttpClientMock(options, saveToFileSystem, testName);

                var urlBuilder = new EndpointUrlBuilder(options);
                var actionInvoker = new ActionInvoker(httpClient, new MessageCreator(options.ApiKey));

                return new ManagementClient(elementProvider, urlBuilder, actionInvoker);
            }

            return new ManagementClient(elementProvider, options);
        }

        internal static async Task<ContentItemModel> PrepareTestItem(ManagementClient client, string typeCodename, string externalId = null)
        {
            var type = ContentTypeIdentifier.ByCodename(typeCodename);
            if (externalId != null)
            {
                // We use upsert for preparing item by external ID in order to be able to recover from situation when previous test run didn't clean up properly
                var item = new ContentItemUpsertModel()
                {
                    Name = "Hooray!",
                    Type = type,
                };

                return await client.UpsertContentItemByExternalIdAsync(externalId, item);
            }
            else
            {
                var item = new ContentItemCreateModel() {
                    Name = "Hooray!",
                    Type = type,
                };

                return await client.CreateContentItemAsync(item);
            }
        }

        internal static async Task<ContentItemVariantModel> PrepareTestVariant(ManagementClient client, string languageCodename, IEnumerable<dynamic> elements, ContentItemModel item)
        {
            var addedItemIdentifier = ContentItemIdentifier.ByCodename(item.CodeName);
            var addedLanguageIdentifier = LanguageIdentifier.ByCodename(languageCodename);
            var addedContentItemLanguageIdentifier = new ContentItemVariantIdentifier(addedItemIdentifier, addedLanguageIdentifier);
            var variantUpdateModel = new ContentItemVariantUpsertModel() { Elements = elements };

            return await client.UpsertContentItemVariantAsync(addedContentItemLanguageIdentifier, variantUpdateModel);
        }
    }
}
