using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using System.Threading.Tasks;
using Kentico.Kontent.Management.Models.Assets;
using Kentico.Kontent.Management.Models.Items;
using Kentico.Kontent.Management.Tests.Data;
using Kentico.Kontent.Management.Exceptions;

using Xunit;
using Kentico.Kontent.Management.Models.ProjectReport;
using Kentico.Kontent.Management.Models.StronglyTyped;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Management.Tests
{
    public class ManagementClientTests
    {
        #region Tests configuration

        /// <summary>
        /// ID of the project the test data are generated from.
        /// </summary>
        private const string PROJECT_ID = "a9931a80-9af4-010b-0590-ecb1273cf1b8";

        private readonly ManagementOptions _options;

        /// <summary>
        /// Allows to adjust the test run type to achieve the desired behavior (see the <see cref="TestUtils.TestRunType"/> enum for more details).
        /// IMPORTANT: Commit always with TestRunType.MockFromFileSystem
        /// </summary>
        private static readonly TestUtils.TestRunType _runType = TestUtils.TestRunType.MockFromFileSystem;

        public ManagementClientTests()
        {
            // Load configuration from user secrets
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<ManagementClientTests>()
                .Build();

            // Configurations with TestRunType.LiveEndpoint* require the ApiKey property set in the user secrets
            // Dummy_API_key fallback is needed e.g. for running tests on mocked data - we need to properly init client
            _options = new ManagementOptions() { ApiKey = configuration.GetValue<string>("ApiKey") ?? "Dummy_API_key", ProjectId = configuration.GetValue<string>("ProjectId") ?? PROJECT_ID };
        }

        #endregion

        #region Helper methods and constants

        // Test constants, existing data references leverage the Dancing Goat sample site project that is generated for everyone
        protected static Guid EXISTING_ITEM_ID = Guid.Parse("4b628214-e4fe-4fe0-b1ff-955df33e1515");
        protected const string EXISTING_ITEM_CODENAME = "on_roasts";

        protected static Guid EXISTING_ITEM_ID2 = Guid.Parse("0f79b41f-53d3-41b5-8fa0-153c87f60bc1");

        protected static Guid EXISTING_LANGUAGE_ID = Guid.Parse("78dbefe8-831b-457e-9352-f4c4eacd5024");
        protected const string EXISTING_LANGUAGE_CODENAME = "es-ES";

        protected static Guid EXISTING_CONTENT_TYPE_ID = Guid.Parse("ba5cd79d-6d4b-5bed-a681-6aa3a366c8f7");
        protected const string EXISTING_CONTENT_TYPE_CODENAME = "article";

        protected static Guid EXISTING_TAXONOMY_TERM_ID = Guid.Parse("6e8b18d5-c5e3-5fc1-9014-44c18ef5f5d8");
        protected const string EXISTING_TAXONOMY_TERM_CODENAME = "barista";

        // Root -> 0ce98752-a614-51a9-bf69-9539deb6532d > 04bf910c-bcac-5faf-ac32-a1f7169fdc0f > e2fe0a21-eb4c-5fba-8a28-697aeab81f83 -> ae11f9dd-ec34-5ecc-9b83-d4a3ae1d8c6b
        protected const string ASSET_FOLDER_ID_1ST_LEVEL = "0ce98752-a614-51a9-bf69-9539deb6532d";
        protected const string ASSET_FOLDER_ID_2ND_LEVEL = "04bf910c-bcac-5faf-ac32-a1f7169fdc0f";
        protected const string ASSET_FOLDER_ID_3RD_LEVEL = "e2fe0a21-eb4c-5fba-8a28-697aeab81f83";
        protected const string ASSET_FOLDER_ID_4TH_LEVEL = "ae11f9dd-ec34-5ecc-9b83-d4a3ae1d8c6b";


        protected static IList<dynamic> _elements = new object[]
        {
            new
            {
                element = new {
                    id = "ba7c8840-bcbc-5e3b-b292-24d0a60f3977"
                },
                value = "On Roasts",
                codename = "title"
            },
            new {
                 element = new {
                    id = "0827e079-3754-5a1d-9381-8ff695a5bbf7"
                },
                value = new DateTime(2017, 7, 4),
                codename = "post_date"
            },
            new {
                element = new {
                    id = "55a88ab3-4009-5bf9-a590-f32162f09b92"
                },
                value = @"
<h1>Light Roasts</h1>
<p>Usually roasted for 6 - 8 minutes or simply until achieving a light brown color.This method is used for milder coffee varieties and for coffee tasting.This type of roasting allows the natural characteristics of each coffee to show.The aroma of coffees produced from light roasts is usually more intense.The cup itself is more acidic and the concentration of caffeine is higher.</p>
",
                codename = "body_copy"
            },
            new {
                element = new {
                    id = "77108990-3c30-5ffb-8dcd-8eb85fc52cb1"
                },
                value = new[] { ContentItemIdentifier.ById(EXISTING_ITEM_ID) },
                codename = "related_articles"
            },
            new {
                element = new {
                    id = "1f37e15b-27a0-5f48-b314-03b401c19cee"
                },
                mode = "custom",
                value = "on-roasts",
                codename = "url_pattern"
            },
            new {
                element = new {
                    id = "c1dc36b5-558d-55a2-8f31-787430a68e4d"
                },
                value = new[] { TaxonomyTermIdentifier.ByCodename(EXISTING_TAXONOMY_TERM_CODENAME) },
                codename = "personas"
            },
        };

        private static readonly ComplexTestModel StronglyTypedElements = new ComplexTestModel
        {
            Title = "On Roast",
            PostDate = new DateTime(2017, 7, 4),
            BodyCopy = "<h1>Light Roasts</h1> <p>Usually roasted for 6 - 8 minutes or simply until achieving a light brown color.This method is used for milder coffee varieties and for coffee tasting.This type of roasting allows the natural characteristics of each coffee to show.The aroma of coffees produced from light roasts is usually more intense.The cup itself is more acidic and the concentration of caffeine is higher.</p>",
            RelatedArticles = new[] { ContentItemIdentifier.ById(EXISTING_ITEM_ID) },
            UrlPattern = new UrlSlug{Value = "on-roasts", Mode = "custom"},
            Personas = new List<TaxonomyTermIdentifier> { TaxonomyTermIdentifier.ByCodename(EXISTING_TAXONOMY_TERM_CODENAME) },
            TeaserImage = new AssetIdentifier[] { }
        };

        private ManagementClient CreateManagementClient(string testName)
        {
            return TestUtils.CreateManagementClient(_options, _runType, testName);
        }

        private static (dynamic expected, dynamic actual) GetElementByCodename(string codename, IEnumerable<dynamic> actualElements)
        {
            var expected = _elements.Single(x => x.codename == codename);
            var actual = actualElements.Single(x => x.element.id == expected.element.id);

            return (expected, actual);
        }

        #endregion

        #region Item Variant

        private string UnifyWhitespace(string text)
        {
            return new Regex("\\s+", RegexOptions.Multiline).Replace(text, " ").Trim();
        }

        private void AssertResponseElements(ContentItemVariantModel responseVariant)
        {
            var (expected, actual) = GetElementByCodename("body_copy", responseVariant.Elements);
            Assert.Equal(UnifyWhitespace(expected.value), UnifyWhitespace(actual.value));

            (expected, actual) = GetElementByCodename("post_date", responseVariant.Elements);
            Assert.Equal(expected.value, actual.value);

            (expected, actual) = GetElementByCodename("url_pattern", responseVariant.Elements);
            Assert.Equal(expected.value, actual.value);

            (expected, actual) = GetElementByCodename("related_articles", responseVariant.Elements);
            Assert.Equal(EXISTING_ITEM_ID, actual.value[0].Id);

            (expected, actual) = GetElementByCodename("personas", responseVariant.Elements);
            Assert.Equal(EXISTING_TAXONOMY_TERM_ID, actual.value[0].Id);
        }

        private void AssertStronglyTypedResponseElements(ComplexTestModel elements)
        {
            Assert.Equal(StronglyTypedElements.Title, elements.Title);
            Assert.Equal(StronglyTypedElements.PostDate, elements.PostDate);
            Assert.Equal(UnifyWhitespace(StronglyTypedElements.BodyCopy), UnifyWhitespace(elements.BodyCopy));
            Assert.Equal(StronglyTypedElements.UrlPattern.Mode, elements.UrlPattern.Mode);
            Assert.Equal(StronglyTypedElements.UrlPattern.Value, elements.UrlPattern.Value);
            Assert.Single(elements.RelatedArticles);
            Assert.Equal(EXISTING_ITEM_ID, elements.RelatedArticles.First().Id);

            Assert.Single(elements.Personas);
            Assert.Equal(EXISTING_TAXONOMY_TERM_ID, elements.Personas[0].Id);
        }


        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void UpsertVariant_ById_LanguageId_UpdatesVariant()
        {
            var client = CreateManagementClient(nameof(UpsertVariant_ById_LanguageId_UpdatesVariant));

            var contentItemVariantUpsertModel = new ContentItemVariantUpsertModel() { Elements = _elements };

            var itemIdentifier = ContentItemIdentifier.ById(EXISTING_ITEM_ID);
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var responseVariant = await client.UpsertContentItemVariantAsync(identifier, contentItemVariantUpsertModel);

            Assert.Equal(EXISTING_ITEM_ID, responseVariant.Item.Id);
            Assert.Equal(EXISTING_LANGUAGE_ID, responseVariant.Language.Id);
            AssertResponseElements(responseVariant);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void UpsertVariant_ByCodename_LanguageId_UpdatesVariant()
        {
            var client = CreateManagementClient(nameof(UpsertVariant_ByCodename_LanguageId_UpdatesVariant));

            var contentItemVariantUpsertModel = new ContentItemVariantUpsertModel() { Elements = _elements };

            var itemIdentifier = ContentItemIdentifier.ByCodename(EXISTING_ITEM_CODENAME);
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var responseVariant = await client.UpsertContentItemVariantAsync(identifier, contentItemVariantUpsertModel);

            Assert.Equal(EXISTING_ITEM_ID, responseVariant.Item.Id);
            Assert.Equal(EXISTING_LANGUAGE_ID, responseVariant.Language.Id);
            AssertResponseElements(responseVariant);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void UpsertVariant_ById_LanguageCodename_UpdatesVariant()
        {
            var client = CreateManagementClient(nameof(UpsertVariant_ById_LanguageCodename_UpdatesVariant));

            var contentItemVariantUpsertModel = new ContentItemVariantUpsertModel() { Elements = _elements };

            var itemIdentifier = ContentItemIdentifier.ById(EXISTING_ITEM_ID);
            var languageIdentifier = LanguageIdentifier.ByCodename(EXISTING_LANGUAGE_CODENAME);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var responseVariant = await client.UpsertContentItemVariantAsync(identifier, contentItemVariantUpsertModel);

            Assert.Equal(EXISTING_ITEM_ID, responseVariant.Item.Id);
            Assert.Equal(EXISTING_LANGUAGE_ID, responseVariant.Language.Id);
            AssertResponseElements(responseVariant);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void UpsertVariant_ByCodename_LanguageCodename_UpdatesVariant()
        {
            var client = CreateManagementClient(nameof(UpsertVariant_ByCodename_LanguageCodename_UpdatesVariant));

            var contentItemVariantUpsertModel = new ContentItemVariantUpsertModel() { Elements = _elements };

            var itemIdentifier = ContentItemIdentifier.ByCodename(EXISTING_ITEM_CODENAME);
            var languageIdentifier = LanguageIdentifier.ByCodename(EXISTING_LANGUAGE_CODENAME);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var responseVariant = await client.UpsertContentItemVariantAsync(identifier, contentItemVariantUpsertModel);

            Assert.Equal(EXISTING_ITEM_ID, responseVariant.Item.Id);
            Assert.Equal(EXISTING_LANGUAGE_ID, responseVariant.Language.Id);
            AssertResponseElements(responseVariant);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void UpsertVariant_ByExternalId_LanguageCodename_UpdatesVariant()
        {
            var client = CreateManagementClient(nameof(UpsertVariant_ByExternalId_LanguageCodename_UpdatesVariant));

            // Arrange
            var externalId = "fe2e8c24f0794f01b36807919602625d";
            var preparedItem = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);
            await TestUtils.PrepareTestVariant(client, EXISTING_LANGUAGE_CODENAME, _elements, preparedItem);

            // Test
            var itemIdentifier = ContentItemIdentifier.ByExternalId(externalId);
            var languageIdentifier = LanguageIdentifier.ByCodename(EXISTING_LANGUAGE_CODENAME);

            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);
            var contentItemVariantUpsertModel = new ContentItemVariantUpsertModel() { Elements = _elements };
            var responseVariant = await client.UpsertContentItemVariantAsync(identifier, contentItemVariantUpsertModel);

            Assert.Equal(responseVariant.Language.Id, EXISTING_LANGUAGE_ID);
            AssertResponseElements(responseVariant);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByExternalId(externalId);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void UpsertVariant_ByExternalId_LanguageCodename_CreatesVariant()
        {
            var client = CreateManagementClient(nameof(UpsertVariant_ByExternalId_LanguageCodename_CreatesVariant));

            // Arrange
            var externalId = "348052a5ad8c44ddac1e9683923d74a5";
            var preparedItem = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);

            // Test
            var itemIdentifier = ContentItemIdentifier.ByExternalId(externalId);
            var languageIdentifier = LanguageIdentifier.ByCodename(EXISTING_LANGUAGE_CODENAME);

            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);
            var contentItemVariantUpsertModel = new ContentItemVariantUpsertModel() { Elements = _elements };
            var responseVariant = await client.UpsertContentItemVariantAsync(identifier, contentItemVariantUpsertModel);

            Assert.Equal(EXISTING_LANGUAGE_ID, responseVariant.Language.Id);
            AssertResponseElements(responseVariant);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByExternalId(externalId);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void UpsertVariant_ByExternalId_LanguageId_UpdatesVariant()
        {
            var client = CreateManagementClient(nameof(UpsertVariant_ByExternalId_LanguageId_UpdatesVariant));

            // Arrange
            var externalId = "d5e050980baa43b085b909cdea4c6d2b";
            var preparedItem = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);
            await TestUtils.PrepareTestVariant(client, EXISTING_LANGUAGE_CODENAME, _elements, preparedItem);

            // Test
            var contentItemVariantUpsertModel = new ContentItemVariantUpsertModel() { Elements = _elements };

            var itemIdentifier = ContentItemIdentifier.ByExternalId(externalId);
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var responseVariant = await client.UpsertContentItemVariantAsync(identifier, contentItemVariantUpsertModel);

            Assert.Equal(EXISTING_LANGUAGE_ID, responseVariant.Language.Id);
            AssertResponseElements(responseVariant);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByExternalId(externalId);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void UpsertVariant_ByExternalId_LanguageId_CreatesVariant()
        {
            var client = CreateManagementClient(nameof(UpsertVariant_ByExternalId_LanguageId_CreatesVariant));

            // Arrange
            var externalId = "73e02811b05f429284006ea94c68c8f7";
            var preparedItem = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);

            // Test
            var contentItemVariantUpsertModel = new ContentItemVariantUpsertModel() { Elements = _elements };

            var itemIdentifier = ContentItemIdentifier.ByExternalId(externalId);
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var responseVariant = await client.UpsertContentItemVariantAsync(identifier, contentItemVariantUpsertModel);

            Assert.Equal(EXISTING_LANGUAGE_ID, responseVariant.Language.Id);
            AssertResponseElements(responseVariant);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByExternalId(externalId);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void UpsertVariant_UsingResponseModel_UpdatesVariant()
        {
            var client = CreateManagementClient(nameof(UpsertVariant_UsingResponseModel_UpdatesVariant));

            // Arrange
            var externalId = "4357b71d21eb45369d54a635faf7672b";
            var preparedItem = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);
            var emptyElements = new List<object>();
            var preparedVariant = await TestUtils.PrepareTestVariant(client, EXISTING_LANGUAGE_CODENAME, emptyElements, preparedItem);

            // Test
            preparedVariant.Elements = _elements;
            var itemIdentifier = ContentItemIdentifier.ByExternalId(externalId);
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var responseVariant = await client.UpsertContentItemVariantAsync(identifier, contentItemVariant: preparedVariant);

            Assert.Equal(EXISTING_LANGUAGE_ID, responseVariant.Language.Id);
            AssertResponseElements(responseVariant);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByExternalId(externalId);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void UpsertVariant_UsingResponseModel_CreatesVariant()
        {
            var client = CreateManagementClient(nameof(UpsertVariant_UsingResponseModel_CreatesVariant));

            // Arrange
            var externalId = "5249f596a8be4d719bc9816e3d416d16";
            var preparedItem = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);
            var emptyElements = new List<object>();
            var preparedVariant = await TestUtils.PrepareTestVariant(client, EXISTING_LANGUAGE_CODENAME, emptyElements, preparedItem);

            // Test
            preparedVariant.Elements = _elements;
            var itemIdentifier = ContentItemIdentifier.ByExternalId(externalId);
            var languageIdentifier = LanguageIdentifier.DEFAULT_LANGUAGE;
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var responseVariant = await client.UpsertContentItemVariantAsync(identifier, contentItemVariant: preparedVariant);

            Assert.Equal(Guid.Empty, responseVariant.Language.Id);
            AssertResponseElements(responseVariant);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByExternalId(externalId);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void ListContentItemVariants_ById_ListsVariants()
        {
            var client = CreateManagementClient(nameof(ListContentItemVariants_ById_ListsVariants));

            var identifier = ContentItemIdentifier.ById(EXISTING_ITEM_ID);

            var responseVariants = await client.ListContentItemVariantsAsync(identifier);

            Assert.Equal(EXISTING_ITEM_ID, responseVariants.First().Item.Id);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void ListContentItemVariants_ByCodename_ListsVariants()
        {
            var client = CreateManagementClient(nameof(ListContentItemVariants_ByCodename_ListsVariants));

            var identifier = ContentItemIdentifier.ByCodename(EXISTING_ITEM_CODENAME);

            var responseVariants = await client.ListContentItemVariantsAsync(identifier);

            Assert.Equal(EXISTING_ITEM_ID, responseVariants.First().Item.Id);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void ListContentItemVariants_ByExternalId_ListsVariants()
        {
            var client = CreateManagementClient(nameof(ListContentItemVariants_ByExternalId_ListsVariants));

            // Arrange
            var externalId = "0220e6ec5b77401ea113b5273c8cdd5e";
            var preparedItem = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);
            await TestUtils.PrepareTestVariant(client, EXISTING_LANGUAGE_CODENAME, _elements, preparedItem);

            // Test
            var identifier = ContentItemIdentifier.ByExternalId(externalId);
            var responseVariants = await client.ListContentItemVariantsAsync(identifier);

            Assert.Single(responseVariants);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByExternalId(externalId);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void GetContentItemVariant_ById_LanguageId_GetsVariant()
        {
            var client = CreateManagementClient(nameof(GetContentItemVariant_ById_LanguageId_GetsVariant));

            var itemIdentifier = ContentItemIdentifier.ById(EXISTING_ITEM_ID);
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var response = await client.GetContentItemVariantAsync(identifier);

            Assert.NotNull(response);
            Assert.Equal(EXISTING_ITEM_ID, response.Item.Id);
            Assert.Equal(EXISTING_LANGUAGE_ID, response.Language.Id);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void GetContentItemVariant_ById_LanguageCodeName_GetsVariant()
        {
            var client = CreateManagementClient(nameof(GetContentItemVariant_ById_LanguageCodeName_GetsVariant));

            var itemIdentifier = ContentItemIdentifier.ById(EXISTING_ITEM_ID);
            var languageIdentifier = LanguageIdentifier.ByCodename(EXISTING_LANGUAGE_CODENAME);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var response = await client.GetContentItemVariantAsync(identifier);

            Assert.NotNull(response);
            Assert.Equal(EXISTING_ITEM_ID, response.Item.Id);
            Assert.Equal(EXISTING_LANGUAGE_ID, response.Language.Id);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void GetContentItemVariant_ByCodename_LanguageId_GetsVariant()
        {
            var client = CreateManagementClient(nameof(GetContentItemVariant_ByCodename_LanguageId_GetsVariant));

            var itemIdentifier = ContentItemIdentifier.ByCodename(EXISTING_ITEM_CODENAME);
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var response = await client.GetContentItemVariantAsync(identifier);

            Assert.NotNull(response);
            Assert.Equal(EXISTING_ITEM_ID, response.Item.Id);
            Assert.Equal(EXISTING_LANGUAGE_ID, response.Language.Id);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void GetContentItemVariant_ByCodename_LanguageCodeName_GetsVariant()
        {
            var client = CreateManagementClient(nameof(GetContentItemVariant_ByCodename_LanguageCodeName_GetsVariant));

            var itemIdentifier = ContentItemIdentifier.ByCodename(EXISTING_ITEM_CODENAME);
            var languageIdentifier = LanguageIdentifier.ByCodename(EXISTING_LANGUAGE_CODENAME);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var response = await client.GetContentItemVariantAsync(identifier);

            Assert.NotNull(response);
            Assert.Equal(EXISTING_ITEM_ID, response.Item.Id);
            Assert.Equal(EXISTING_LANGUAGE_ID, response.Language.Id);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void GetContentItemVariant_ByExternalId_LanguageCodename_GetsVariant()
        {
            var client = CreateManagementClient(nameof(GetContentItemVariant_ByExternalId_LanguageCodename_GetsVariant));

            // Arrange
            var externalId = "f9cfaa3e00f64e22a144fdacf4cba3e5";
            var preparedItem = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);
            await TestUtils.PrepareTestVariant(client, EXISTING_LANGUAGE_CODENAME, _elements, preparedItem);

            // Test
            var itemIdentifier = ContentItemIdentifier.ByExternalId(externalId);
            var languageIdentifier = LanguageIdentifier.ByCodename(EXISTING_LANGUAGE_CODENAME);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var response = await client.GetContentItemVariantAsync(identifier);

            Assert.NotNull(response);
            Assert.Equal(preparedItem.Id, response.Item.Id);
            Assert.Equal(EXISTING_LANGUAGE_ID, response.Language.Id);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByExternalId(externalId);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void GetContentItemVariant_ByExternalId_ReturnsVariant()
        {
            var client = CreateManagementClient(nameof(GetContentItemVariant_ByExternalId_ReturnsVariant));

            var externalId = "ad66f70ed9bb4b8694116c9119c4a930";
            var preparedItem = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);
            await TestUtils.PrepareTestVariant(client, EXISTING_LANGUAGE_CODENAME, _elements, preparedItem);

            var itemIdentifier = ContentItemIdentifier.ByExternalId(externalId);
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var response = await client.GetContentItemVariantAsync(identifier);

            Assert.NotNull(response);
            Assert.Equal(preparedItem.Id, response.Item.Id);
            Assert.Equal(EXISTING_LANGUAGE_ID, response.Language.Id);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByExternalId(externalId);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void DeleteContentItemVariant_ById_LanguageCodename_DeletesVariant()
        {
            var client = CreateManagementClient(nameof(DeleteContentItemVariant_ById_LanguageCodename_DeletesVariant));

            var itemResponse = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME);
            await TestUtils.PrepareTestVariant(client, EXISTING_LANGUAGE_CODENAME, _elements, itemResponse);

            var itemIdentifier = ContentItemIdentifier.ById(itemResponse.Id);
            var languageIdentifier = LanguageIdentifier.ByCodename(EXISTING_LANGUAGE_CODENAME);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            await client.DeleteContentItemVariantAsync(identifier);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void DeleteContentItemVariant_ById_LanguageId_DeletesVariant()
        {
            var client = CreateManagementClient(nameof(DeleteContentItemVariant_ById_LanguageId_DeletesVariant));

            var itemResponse = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME);
            await TestUtils.PrepareTestVariant(client, EXISTING_LANGUAGE_CODENAME, _elements, itemResponse);

            var itemIdentifier = ContentItemIdentifier.ById(itemResponse.Id);
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            await client.DeleteContentItemVariantAsync(identifier);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void DeleteContentItemVariant_ByCodename_LanguageId_DeletesVariant()
        {
            var client = CreateManagementClient(nameof(DeleteContentItemVariant_ByCodename_LanguageId_DeletesVariant));

            // Prepare item
            var itemResponse = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME);
            await TestUtils.PrepareTestVariant(client, EXISTING_LANGUAGE_CODENAME, _elements, itemResponse);

            var itemIdentifier = ContentItemIdentifier.ByCodename(itemResponse.CodeName);
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            await client.DeleteContentItemVariantAsync(identifier);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void DeleteContentItemVariant_ByCodename_LanguageCodename_DeletesVariant()
        {
            var client = CreateManagementClient(nameof(DeleteContentItemVariant_ByCodename_LanguageCodename_DeletesVariant));

            // Prepare item
            var itemResponse = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME);
            await TestUtils.PrepareTestVariant(client, EXISTING_LANGUAGE_CODENAME, _elements, itemResponse);

            var itemIdentifier = ContentItemIdentifier.ByCodename(itemResponse.CodeName);
            var languageIdentifier = LanguageIdentifier.ByCodename(EXISTING_LANGUAGE_CODENAME);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            await client.DeleteContentItemVariantAsync(identifier);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void DeleteContentItemVariant_ByExternalId_LanguageId_DeletesVariant()
        {
            var client = CreateManagementClient(nameof(DeleteContentItemVariant_ByExternalId_LanguageId_DeletesVariant));

            var externalId = "90285b1a983c43299638c8a835f16b81";
            var itemResponse = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);
            await TestUtils.PrepareTestVariant(client, EXISTING_LANGUAGE_CODENAME, _elements, itemResponse);

            var itemIdentifier = ContentItemIdentifier.ByExternalId(externalId);
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            await client.DeleteContentItemVariantAsync(identifier);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async void DeleteContentItemVariant_ByExternalId_LanguageCodename_DeletesVariant()
        {
            var client = CreateManagementClient(nameof(DeleteContentItemVariant_ByExternalId_LanguageCodename_DeletesVariant));

            var externalId = "f4fe87222b6b46739bc673f6e5165c12";
            var itemResponse = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);
            await TestUtils.PrepareTestVariant(client, EXISTING_LANGUAGE_CODENAME, _elements, itemResponse);

            var itemIdentifier = ContentItemIdentifier.ByExternalId(externalId);
            var languageIdentifier = LanguageIdentifier.ByCodename(EXISTING_LANGUAGE_CODENAME);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            await client.DeleteContentItemVariantAsync(identifier);
        }

        #endregion

        #region Item

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void CreateContentItem_CreatesContentItem()
        {
            var client = CreateManagementClient(nameof(CreateContentItem_CreatesContentItem));

            var itemName = "Hooray!";
            var itemCodeName = "hooray_codename";
            var type = ContentTypeIdentifier.ByCodename(EXISTING_CONTENT_TYPE_CODENAME);
            var item = new ContentItemCreateModel
            {
                CodeName = itemCodeName,
                Name = itemName,
                Type = type
            };

            var responseItem = await client.CreateContentItemAsync(item);

            Assert.Equal(itemName, responseItem.Name);
            Assert.Equal(itemCodeName, responseItem.CodeName);
            Assert.Equal(EXISTING_CONTENT_TYPE_ID, responseItem.Type.Id);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByCodename(itemCodeName);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void ListContentItems_ListsContentItems()
        {
            var client = CreateManagementClient(nameof(ListContentItems_ListsContentItems));

            var response = await client.ListContentItemsAsync();

            Assert.NotNull(response);
            Assert.NotNull(response.FirstOrDefault());
        }

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void ListContentItems_WithContinuation_ListsAllContentItems()
        {
            var client = CreateManagementClient(nameof(ListContentItems_WithContinuation_ListsAllContentItems));

            var response = await client.ListContentItemsAsync();
            Assert.NotNull(response);

            while (true)
            {
                foreach (var item in response)
                {
                    Assert.NotNull(item);
                }

                if (!response.HasNextPage())
                {
                    break;
                }
                response = await response.GetNextPage();
                Assert.NotNull(response);
            }
        }

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void UpdateContentItem_ByCodename_UpdatesContentItem()
        {
            var client = CreateManagementClient(nameof(UpdateContentItem_ByCodename_UpdatesContentItem));

            var itemName = "Hooray!";
            var identifier = ContentItemIdentifier.ByCodename(EXISTING_ITEM_CODENAME);

            // Set codename, name and sitemap locations
            var item = new ContentItemUpdateModel
            {
                CodeName = EXISTING_ITEM_CODENAME,
                Name = itemName
            };

            var responseItem = await client.UpdateContentItemAsync(identifier, item);

            Assert.Equal(itemName, responseItem.Name);
            Assert.Equal(EXISTING_ITEM_CODENAME, responseItem.CodeName);
        }

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void UpdateContentItem_ById_UpdatesContentItem()
        {
            var client = CreateManagementClient(nameof(UpdateContentItem_ById_UpdatesContentItem));

            var itemName = "Ciao!";
            var itemCodeName = "ciao_codename";
            var identifier = ContentItemIdentifier.ById(EXISTING_ITEM_ID2);

            var item = new ContentItemUpdateModel
            {
                CodeName = itemCodeName,
                Name = itemName
            };

            var responseItem = await client.UpdateContentItemAsync(identifier, item);

            Assert.Equal(itemName, responseItem.Name);
            Assert.Equal(itemCodeName, responseItem.CodeName);
        }

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void UpdateContentItemName_CodeNameNotSet_RegeneratesCodeNameByName()
        {
            var client = CreateManagementClient(nameof(UpdateContentItemName_CodeNameNotSet_RegeneratesCodeNameByName));

            var itemName = "regenerated_codename";
            var identifier = ContentItemIdentifier.ById(EXISTING_ITEM_ID2);

            var item = new ContentItemUpdateModel
            {
                Name = itemName,
            };

            var responseItem = await client.UpdateContentItemAsync(identifier, item);

            Assert.Equal(itemName, responseItem.Name);
            Assert.Equal(itemName, responseItem.CodeName);
        }

        [Fact(Skip = "Kentico.Kontent.Management.Exceptions.ManagementException : The request was not processed because the specified object has been modified by another request.")]
        [Trait("Category", "ContentItem")]
        public async void UpdateContentItem_UsingResponseModel_UpdatesContentItem()
        {
            var client = CreateManagementClient(nameof(UpdateContentItem_UsingResponseModel_UpdatesContentItem));

            // Arrange
            var externalId = "093afb41b0614a908c8734d2bb840210";
            var preparedItem = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);

            // Test
            preparedItem.Name = "EditedItem";
            var identifier = ContentItemIdentifier.ByExternalId(externalId);
            var item = client.UpdateContentItemAsync(identifier, preparedItem);

            var contentItemResponse = await client.UpdateContentItemAsync(identifier, preparedItem);
            Assert.Equal("EditedItem", contentItemResponse.Name);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByExternalId(externalId);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void UpsertContentItemByExternalId_UpdatesContentItem()
        {
            var client = CreateManagementClient(nameof(UpsertContentItemByExternalId_UpdatesContentItem));

            // Arrange
            var externalId = "753f6e965f4d49e5a120ca9a23551b10";
            var itemName = "Aloha!";
            var itemCodeName = "aloha_codename";
            await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);

            // Test
            var type = ContentTypeIdentifier.ByCodename(EXISTING_CONTENT_TYPE_CODENAME);
            var item = new ContentItemUpsertModel()
            {
                CodeName = itemCodeName,
                Name = itemName,
                Type = type
            };

            var contentItemResponse = await client.UpsertContentItemByExternalIdAsync(externalId, item);
            Assert.Equal(itemName, contentItemResponse.Name);
            Assert.Equal(itemCodeName, contentItemResponse.CodeName);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByExternalId(externalId);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void UpsertContentItemByExternalId_CreatesContentItem()
        {
            var client = CreateManagementClient(nameof(UpsertContentItemByExternalId_CreatesContentItem));

            // Test
            var externalId = "9d98959eeac446288992b44b5d366e16";
            var itemName = "Hi!";
            var itemCodeName = "hi_codename";
            var type = ContentTypeIdentifier.ByCodename(EXISTING_CONTENT_TYPE_CODENAME);
            var item = new ContentItemUpsertModel()
            {
                CodeName = itemCodeName,
                Name = itemName,
                Type = type
            };

            var contentItemResponse = await client.UpsertContentItemByExternalIdAsync(externalId, item);
            Assert.Equal(itemName, contentItemResponse.Name);
            Assert.Equal(externalId, contentItemResponse.ExternalId);
            Assert.Equal(itemCodeName, contentItemResponse.CodeName);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByExternalId(externalId);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void GetContentItem_ById_GetsContentItem()
        {
            var client = CreateManagementClient(nameof(GetContentItem_ById_GetsContentItem));

            var identifier = ContentItemIdentifier.ById(EXISTING_ITEM_ID);

            var contentItemReponse = await client.GetContentItemAsync(identifier);
            Assert.Equal(EXISTING_ITEM_ID, contentItemReponse.Id);
        }

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void GetContentItem_ByCodename_GetsContentItem()
        {
            var client = CreateManagementClient(nameof(GetContentItem_ByCodename_GetsContentItem));

            var identifier = ContentItemIdentifier.ByCodename(EXISTING_ITEM_CODENAME);

            var contentItemReponse = await client.GetContentItemAsync(identifier);
            Assert.Equal(EXISTING_ITEM_ID, contentItemReponse.Id);
        }

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void GetContentItem_ByExternalId_GetsContentItem()
        {
            var client = CreateManagementClient(nameof(GetContentItem_ByExternalId_GetsContentItem));

            // Arrange
            var externalId = "e5a8de5b584f4182b879c78b696dff09";
            await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);

            // Test
            var identifier = ContentItemIdentifier.ByExternalId(externalId);

            var contentItemReponse = await client.GetContentItemAsync(identifier);
            Assert.Equal(externalId, contentItemReponse.ExternalId);

            // Cleanup
            var itemToClean = ContentItemIdentifier.ByExternalId(externalId);
            await client.DeleteContentItemAsync(itemToClean);
        }

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void DeleteContentItem_ById_DeletesContentItem()
        {
            var client = CreateManagementClient(nameof(DeleteContentItem_ById_DeletesContentItem));

            var itemToDelete = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME);

            var identifier = ContentItemIdentifier.ById(itemToDelete.Id);

            await client.DeleteContentItemAsync(identifier);

            // Check if not available after deletion
            if (_runType != TestUtils.TestRunType.MockFromFileSystem)
            {
                await Assert.ThrowsAsync<ManagementException>(() => client.GetContentItemAsync(identifier));
            }
        }

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void DeleteContentItem_ByCodename_DeletesContentItem()
        {
            var client = CreateManagementClient(nameof(DeleteContentItem_ByCodename_DeletesContentItem));

            var itemToDelete = await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME);

            var identifier = ContentItemIdentifier.ByCodename(itemToDelete.CodeName);

            await client.DeleteContentItemAsync(identifier);

            // Check if not available after deletion
            if (_runType != TestUtils.TestRunType.MockFromFileSystem)
            {
                await Assert.ThrowsAsync<ManagementException>(() => client.GetContentItemAsync(identifier));
            }
        }

        [Fact]
        [Trait("Category", "ContentItem")]
        public async void DeleteContentItem_ByExternalId_DeletesContentItem()
        {
            var client = CreateManagementClient(nameof(DeleteContentItem_ByExternalId_DeletesContentItem));

            var externalId = "341bcf72988d49729ec34c8682710536";
            await TestUtils.PrepareTestItem(client, EXISTING_CONTENT_TYPE_CODENAME, externalId);

            var identifier = ContentItemIdentifier.ByExternalId(externalId);

            await client.DeleteContentItemAsync(identifier);

            // Check if not available after deletion
            if (_runType != TestUtils.TestRunType.MockFromFileSystem)
            {
                await Assert.ThrowsAsync<ManagementException>(() => client.GetContentItemAsync(identifier));
            }
        }

        #endregion

        #region Assets

        [Fact]
        [Trait("Category", "Asset")]
        public async void ListAssets_ListsAssets()
        {
            var client = CreateManagementClient(nameof(ListAssets_ListsAssets));

            var response = await client.ListAssetsAsync();
            Assert.NotNull(response);
            Assert.NotNull(response.FirstOrDefault());
        }

        [Fact]
        [Trait("Category", "Asset")]
        public async void ListFolders_ListFolders()
        {
            var client = CreateManagementClient(nameof(ListFolders_ListFolders));

            var response = await client.GetAssetFoldersAsync();
            Assert.NotNull(response);
            Assert.True(response.Folders.Count() > 0);
        }

        [Fact]
        [Trait("Category", "Asset")]
        public async void ListFolders_GetFolderLinkedTree()
        {
            var client = CreateManagementClient(nameof(ListFolders_GetFolderLinkedTree));

            var response = await client.GetAssetFoldersAsync();
            var linkedHierarchy = response.Folders.GetParentLinkedFolderHierarchy();

            Assert.NotNull(response);
            Assert.True(response.Folders.Count() > 0);
        }


        [Fact]
        [Trait("Category", "Asset")]
        public async void ListFolders_GetFolderLinkedTreeSearchByFolderId()
        {
            var client = CreateManagementClient(nameof(ListFolders_GetFolderLinkedTreeSearchByFolderId));

            var response = await client.GetAssetFoldersAsync();
            var linkedHierarchy = response.Folders.GetParentLinkedFolderHierarchy();
            var result = linkedHierarchy.GetParentLinkedFolderHierarchyById(ASSET_FOLDER_ID_1ST_LEVEL);
            var result2 = linkedHierarchy.GetParentLinkedFolderHierarchyById(ASSET_FOLDER_ID_2ND_LEVEL);
            var result3 = linkedHierarchy.GetParentLinkedFolderHierarchyById(ASSET_FOLDER_ID_3RD_LEVEL);
            var result4 = linkedHierarchy.GetParentLinkedFolderHierarchyById(ASSET_FOLDER_ID_4TH_LEVEL);

            Assert.NotNull(response);
            Assert.NotNull(result);
            Assert.NotNull(result2);
            Assert.NotNull(result3);
            Assert.NotNull(result4);
            Assert.Equal("TopFolder", result.Name);
            Assert.Equal("2ndFolder", result2.Name);
            Assert.Equal("3rdFolder", result3.Name);
            Assert.Equal("4thFolder", result4.Name);
        }

        [Fact]
        [Trait("Category", "Asset")]
        public async void ListFolders_GetFolderHierarchy_NonExistingFolder()
        {
            var client = CreateManagementClient(nameof(ListFolders_GetFolderHierarchy_NonExistingFolder));

            var response = await client.GetAssetFoldersAsync();
            var nonExistingFolderId = "2ddaf2dc-8635-4b3f-b04d-5be69a0949e6";
            var result = response.Folders.GetFolderHierarchyById(nonExistingFolderId);

            Assert.Null(result);
        }

        [Fact()]
        [Trait("Category", "Asset")]
        public async void ListFolders_GetFolderHierarchy_ExistingFolder()
        {
            var client = CreateManagementClient(nameof(ListFolders_GetFolderHierarchy_ExistingFolder));

            var response = await client.GetAssetFoldersAsync();
            var result = response.Folders.GetFolderHierarchyById(ASSET_FOLDER_ID_4TH_LEVEL);

            Assert.NotNull(result);
            Assert.True(result.Name == "4thFolder");
        }

        [Fact]
        [Trait("Category", "Asset")]
        public async void ListFolders_GetFolderPathString()
        {
            var client = CreateManagementClient(nameof(ListFolders_GetFolderPathString));

            var response = await client.GetAssetFoldersAsync();
            var linkedHierarchy = response.Folders.GetParentLinkedFolderHierarchy();
            var result = linkedHierarchy.GetParentLinkedFolderHierarchyById("e2fe0a21-eb4c-5fba-8a28-697aeab81f83"); //Go three levels deep
            var pathString = result.GetFullFolderPath(); //Should be a folder path string TopFolder\2ndFolder\3rdFolder (3 levels deep)

            Assert.NotNull(response);
            Assert.NotNull(result);
            Assert.True(pathString == "TopFolder\\2ndFolder\\3rdFolder");
        }

        [Fact]
        [Trait("Category", "Asset")]
        public async void ListAssets_WithContinuation_ListsAllAssets()
        {
            var client = CreateManagementClient(nameof(ListAssets_WithContinuation_ListsAllAssets));

            var response = await client.ListAssetsAsync();
            Assert.NotNull(response);

            while (true)
            {
                foreach (var asset in response)
                {
                    Assert.NotNull(asset);
                }

                if (!response.HasNextPage())
                {
                    break;
                }
                response = await response.GetNextPage();
                Assert.NotNull(response);
            }
        }

        [Fact]
        [Trait("Category", "Asset")]
        public async void CreateAsset_WithStream_Uploads_CreatesAsset()
        {
            var client = CreateManagementClient(nameof(CreateAsset_WithStream_Uploads_CreatesAsset));

            var content = $"Hello world from CM API .NET SDK test {nameof(CreateAsset_WithStream_Uploads_CreatesAsset)}! {"X".PadLeft((int)new Random().NextDouble() * 100, 'X')}";

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                var fileName = "Hello.txt";
                var contentType = "text/plain";

                var fileResult = await client.UploadFileAsync(new FileContentSource(stream, fileName, contentType));

                Assert.NotNull(fileResult);
                Assert.Equal(FileReferenceTypeEnum.Internal, fileResult.Type);

                Assert.True(Guid.TryParse(fileResult.Id, out Guid fileId));

                Assert.NotEqual(Guid.Empty, fileId);

                var asset = new AssetUpsertModel
                {
                    FileReference = fileResult,
                };

                var assetResult = await client.CreateAssetAsync(asset);

                Assert.NotNull(assetResult);
                Assert.Null(assetResult.ExternalId);
                Assert.Equal(contentType, assetResult.Type);
                Assert.Equal(content.Length, assetResult.Size);
                Assert.NotNull(assetResult.LastModified);
                Assert.Equal(fileName, assetResult.FileName);
                Assert.NotNull(assetResult.Descriptions);
                Assert.NotNull(assetResult.Url);

                // Cleanup
                await client.DeleteAssetAsync(AssetIdentifier.ById(assetResult.Id));
            }
        }

        [Fact]
        [Trait("Category", "Asset")]
        public async void UpsertAssetByExternalId_WithByteArray_Uploads_CreatesAsset()
        {
            var client = CreateManagementClient(nameof(UpsertAssetByExternalId_WithByteArray_Uploads_CreatesAsset));

            var content = $"Hello world from CM API .NET SDK test {nameof(UpsertAssetByExternalId_WithByteArray_Uploads_CreatesAsset)}! {"X".PadLeft((int)new Random().NextDouble() * 100, 'X')}";

            var fileName = "HelloExternal.txt";
            var contentType = "text/plain";

            var fileResult = await client.UploadFileAsync(new FileContentSource(Encoding.UTF8.GetBytes(content), fileName, contentType));

            Assert.NotNull(fileResult);
            Assert.Equal(FileReferenceTypeEnum.Internal, fileResult.Type);

            Assert.True(Guid.TryParse(fileResult.Id, out Guid fileId));

            Assert.NotEqual(Guid.Empty, fileId);

            var spanishDescription = "Spanish descriptión";
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var assetDescription = new AssetDescription { Description = spanishDescription, Language = languageIdentifier };
            var descriptions = new[] { assetDescription };

            var asset = new AssetUpsertModel
            {
                FileReference = fileResult,
                Descriptions = descriptions
            };
            var externalId = "99877608d1f6448ebb35778f027c92f6";

            var assetResult = await client.UpsertAssetByExternalIdAsync(externalId, asset);

            Assert.NotNull(assetResult);
            Assert.Equal(externalId, assetResult.ExternalId);
            Assert.Equal(contentType, assetResult.Type);
            Assert.Equal(content.Length, assetResult.Size);
            Assert.NotNull(assetResult.LastModified);
            Assert.Equal(fileName, assetResult.FileName);
            Assert.Equal(spanishDescription, assetResult.Descriptions.FirstOrDefault(d => d.Language.Id == EXISTING_LANGUAGE_ID).Description);
            Assert.NotNull(assetResult.Url);

            // Cleanup
            await client.DeleteAssetAsync(AssetIdentifier.ByExternalId(externalId));
        }

        [Fact]
        [Trait("Category", "Asset")]
        public async void CreateAsset_WithFile_Uploads_CreatesAsset()
        {
            var client = CreateManagementClient(nameof(CreateAsset_WithFile_Uploads_CreatesAsset));

            var content = $"Hello world from CM API .NET SDK test {nameof(CreateAsset_WithFile_Uploads_CreatesAsset)}! {"X".PadLeft((int)new Random().NextDouble() * 100, 'X')}";

            var fileName = "Hello.txt";
            var contentType = "text/plain";

            var spanishDescription = "Spanish descriptión";
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var assetDescription = new AssetDescription { Description = spanishDescription, Language = languageIdentifier };
            var descriptions = new[] { assetDescription };
            var title = "New title";

            var assetResult = await client.CreateAssetAsync(new FileContentSource(Encoding.UTF8.GetBytes(content), fileName, contentType), new AssetUpdateModel { Descriptions = descriptions, Title = title });

            Assert.NotNull(assetResult);
            Assert.Null(assetResult.ExternalId);
            Assert.Equal(contentType, assetResult.Type);
            Assert.Equal(content.Length, assetResult.Size);
            Assert.NotNull(assetResult.LastModified);
            Assert.Equal(fileName, assetResult.FileName);
            Assert.Equal(title, assetResult.Title);
            Assert.Equal(spanishDescription, assetResult.Descriptions.FirstOrDefault(d => d.Language.Id == EXISTING_LANGUAGE_ID).Description);
            Assert.NotNull(assetResult.Url);

            // Cleanup
            await client.DeleteAssetAsync(AssetIdentifier.ById(assetResult.Id));
        }

        [Fact]
        [Trait("Category", "Asset")]
        public async void CreateAsset_FromFileSystem_Uploads_CreatesAsset()
        {
            var client = CreateManagementClient(nameof(CreateAsset_FromFileSystem_Uploads_CreatesAsset));

            var descriptions = new List<AssetDescription>();
            var title = "My new asset";

            var filePath = Path.Combine(Environment.CurrentDirectory, "Data\\kentico_rgb_bigger.png");
            var contentType = "image/png";

            var assetResult = await client.CreateAssetAsync(new FileContentSource(filePath, contentType), new AssetUpdateModel { Descriptions = descriptions, Title = title });

            Assert.NotNull(assetResult);
            Assert.Null(assetResult.ExternalId);
            Assert.Equal(contentType, assetResult.Type);
            Assert.Equal(new FileInfo(filePath).Length, assetResult.Size);
            Assert.NotNull(assetResult.LastModified);
            Assert.Equal("kentico_rgb_bigger.png", assetResult.FileName);
            Assert.NotNull(assetResult.Descriptions);
            Assert.Equal(title, assetResult.Title);
            Assert.NotNull(assetResult.Url);

            // Cleanup
            await client.DeleteAssetAsync(AssetIdentifier.ById(assetResult.Id));
        }

        [Fact]
        [Trait("Category", "Asset")]
        public async void UpsertAssetByExternalId_FromByteArray_Uploads_CreatesAsset()
        {
            var client = CreateManagementClient(nameof(UpsertAssetByExternalId_FromByteArray_Uploads_CreatesAsset));

            var content = $"Hello world from CM API .NET SDK test {nameof(UpsertAssetByExternalId_FromByteArray_Uploads_CreatesAsset)}! {"X".PadLeft((int)new Random().NextDouble() * 100, 'X')}";

            var fileName = "HelloExternal.txt";
            var contentType = "text/plain";

            var externalId = "5bec7f21ad2e44bb8a3a1f4a6a5bf8ca";

            var spanishDescription = "Spanish descriptión";
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var assetDescription = new AssetDescription { Description = spanishDescription, Language = languageIdentifier };
            var descriptions = new[] { assetDescription };
            var title = "New title";

            var assetResult = await client.UpsertAssetByExternalIdAsync(externalId, new FileContentSource(Encoding.UTF8.GetBytes(content), fileName, contentType), new AssetUpdateModel { Descriptions = descriptions, Title = title });

            Assert.NotNull(assetResult);
            Assert.Equal(externalId, assetResult.ExternalId);
            Assert.Equal(contentType, assetResult.Type);
            Assert.Equal(content.Length, assetResult.Size);
            Assert.NotNull(assetResult.LastModified);
            Assert.Equal(fileName, assetResult.FileName);
            Assert.Equal(title, assetResult.Title);
            Assert.Equal(spanishDescription, assetResult.Descriptions.FirstOrDefault(d => d.Language.Id == EXISTING_LANGUAGE_ID).Description);
            Assert.NotNull(assetResult.Url);

            // Cleanup
            await client.DeleteAssetAsync(AssetIdentifier.ByExternalId(externalId));
        }

        [Fact]
        [Trait("Category", "Asset")]
        public async void UpdateAssetById_ReturnsUpdatedAsset()
        {
            var client = CreateManagementClient(nameof(UpdateAssetById_ReturnsUpdatedAsset));

            var identifier = AssetIdentifier.ById(Guid.Parse("01647205-c8c4-4b41-b524-1a98a7b12750"));
            var title = "My super asset";
            var updatedDescription = new AssetDescription()
            {
                Language = LanguageIdentifier.DEFAULT_LANGUAGE,
                Description = "Dancing Goat Café - Los Angeles - UPDATED",
            };
            var update = new AssetUpdateModel() { Descriptions = new[] { updatedDescription }, Title = title };

            var assetResult = await client.UpdateAssetAsync(identifier, update);

            Assert.Equal(identifier.Id.ToString(), assetResult.Id.ToString());
            Assert.Equal(updatedDescription.Description, assetResult.Descriptions.FirstOrDefault(d => d.Language.Id == Guid.Empty).Description);
            Assert.Equal(title, assetResult.Title);
        }

        [Fact]
        [Trait("Category", "Asset")]
        public async void GetAsset_WhenGivenAssetId_ReturnsGivenAsset()
        {
            var client = CreateManagementClient(nameof(GetAsset_WhenGivenAssetId_ReturnsGivenAsset));

            var identifier = AssetIdentifier.ById(Guid.Parse("01647205-c8c4-4b41-b524-1a98a7b12750"));

            var response = await client.GetAssetAsync(identifier);

            Assert.Equal(identifier.Id, response.Id);
        }

        #endregion

        #region Strongly Typed Item Variant

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async Task ListStronglyTypedContentItemVariants_ById_ListsVariants()
        {
            var client = CreateManagementClient(nameof(ListStronglyTypedContentItemVariants_ById_ListsVariants));

            var identifier = ContentItemIdentifier.ById(EXISTING_ITEM_ID);

            var responseVariants = await client.ListContentItemVariantsAsync<ComplexTestModel>(identifier);

            Assert.All(responseVariants, x =>
            {
                Assert.NotNull(x.Elements);
            });
            Assert.Equal(EXISTING_ITEM_ID, responseVariants.First().Item.Id);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async Task GetStronglyTypedContentItemVariantAsync_ById_LanguageId_GetVariant()
        {
            var client = CreateManagementClient(nameof(GetStronglyTypedContentItemVariantAsync_ById_LanguageId_GetVariant));

            var itemIdentifier = ContentItemIdentifier.ById(EXISTING_ITEM_ID);
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var response = await client.GetContentItemVariantAsync<ComplexTestModel>(identifier);

            Assert.NotNull(response);
            Assert.Equal(EXISTING_ITEM_ID, response.Item.Id);
            Assert.Equal(EXISTING_LANGUAGE_ID, response.Language.Id);
            Assert.NotNull(response.Elements);
        }

        [Fact]
        [Trait("Category", "ContentItemVariant")]
        public async Task UpsertStronglyTypedContentItemVariantAsync_ById_LanguageId_UpdatesVariant()
        {
            var client = CreateManagementClient(nameof(UpsertStronglyTypedContentItemVariantAsync_ById_LanguageId_UpdatesVariant));

            var itemIdentifier = ContentItemIdentifier.ById(EXISTING_ITEM_ID);
            var languageIdentifier = LanguageIdentifier.ById(EXISTING_LANGUAGE_ID);
            var identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

            var responseVariant = await client.UpsertContentItemVariantAsync(identifier, StronglyTypedElements);

            Assert.Equal(EXISTING_ITEM_ID, responseVariant.Item.Id);
            Assert.Equal(EXISTING_LANGUAGE_ID, responseVariant.Language.Id);
            Assert.NotNull(responseVariant.Elements);
            AssertStronglyTypedResponseElements(responseVariant.Elements);
        }

        #endregion

        #region Validation

        [Fact]
        [Trait("Category", "Validation")]
        public async void ValidateProject_ReturnsProjectReportModel()
        {
            var responseElementIssueMessage = "Element 'Related articles' is required but has no value";

            var project = new Project
            {
                Id = _options.ProjectId,
                Name = ".NET MAPI V2 SDK Tests"
            };

            var itemMetadata = new Metadata
            {
                Id = new Guid("deee0b3c-7b3c-4841-a603-5ada23f550fd"),
                Name = "Coffee Beverages Explained",
                Codename = "coffee_beverages_explained"
            };

            var languageMetadata = new Metadata
            {
                Id = EXISTING_LANGUAGE_ID,
                Name = "Spanish (Spain)",
                Codename = "es-ES"
            };

            var elementMetadata = new Metadata
            {
                Id = new Guid("77108990-3c30-5ffb-8dcd-8eb85fc52cb1"),
                Name = "Related articles",
                Codename = "related_articles"
            };

            var client = CreateManagementClient(nameof(ValidateProject_ReturnsProjectReportModel));
            var response = await client.ValidateProjectAsync();

            Assert.Equal(project.Id, response.Project.Id);
            Assert.Equal(project.Name, response.Project.Name);
            Assert.NotEmpty(response.VariantIssues);

            // select issue we are interested in
            var relatedArticlesVariantIssue = response.VariantIssues.Where(i => i.Item.Id == itemMetadata.Id && i.Language.Id == EXISTING_LANGUAGE_ID).FirstOrDefault();
            AssertMetadataEqual(itemMetadata, relatedArticlesVariantIssue.Item);
            AssertMetadataEqual(languageMetadata, relatedArticlesVariantIssue.Language);
            Assert.NotEmpty(relatedArticlesVariantIssue.Issues);

            var firstResponseElementIssue = relatedArticlesVariantIssue.Issues.First();
            AssertMetadataEqual(elementMetadata, firstResponseElementIssue.Element);
            Assert.Contains(responseElementIssueMessage, firstResponseElementIssue.Messages.First());
        }

        private static void AssertMetadataEqual(Metadata expected, Metadata actual)
        {
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Codename, actual.Codename);
        }

        #endregion
    }
}