using System;
using System.Collections.Generic;
using System.Linq;
using Kentico.Kontent.Management.Models.Assets;
using Kentico.Kontent.Management.Models.Items;
using Kentico.Kontent.Management.Models.StronglyTyped;
using Kentico.Kontent.Management.Modules.ActionInvoker;
using Kentico.Kontent.Management.Modules.ModelBuilders;
using Kentico.Kontent.Management.Tests.Data;
using Newtonsoft.Json;
using Xunit;

namespace Kentico.Kontent.Management.Tests.ModelBuildersTests
{
    public class ModelProviderTests
    {
        private readonly CustomElementProvider _elementProvider;
        private readonly IModelProvider _modelProvider;

        public ModelProviderTests()
        {
            _elementProvider = new CustomElementProvider();
            _modelProvider = new ModelProvider(_elementProvider);
        }

        [Fact]
        public void GetContentItemVariantModel_ReturnsExpected()
        {
            var expected = GetTestModel();
            var model = new ContentItemVariantModel
            {
                Elements = ToDynamic(expected)
            };
            var actual = _modelProvider.GetContentItemVariantModel<ComplexTestModel>(model).Elements;

            AssertElements(expected, actual);
        }

        [Fact]
        public void GetContentItemVariantUpsertModel_ReturnsExpected()
        {
            var model = GetTestModel();
            var expected = model;
            var actual = _modelProvider.GetContentItemVariantUpsertModel(model).Elements;

            AssertElements(expected, actual);
        }

        private void AssertElements(ComplexTestModel expected, IEnumerable<dynamic> actual)
        {
            var type = typeof(ComplexTestModel);

            Assert.Equal(expected.Title, actual.Single(x => x.element.id == _elementProvider.GetElementIdByCodename(type, "title")).value);
            Assert.Equal(expected.PostDate, actual.Single(x => x.element.id == _elementProvider.GetElementIdByCodename(type, "post_date")).value);
            Assert.Equal(expected.UrlPattern.Mode, actual.Single(x => x.element.id == _elementProvider.GetElementIdByCodename(type, "url_pattern")).mode);
            Assert.Equal(expected.UrlPattern.Value, actual.Single(x => x.element.id == _elementProvider.GetElementIdByCodename(type, "url_pattern")).value);
            Assert.Equal(expected.BodyCopy, actual.Single(x => x.element.id == _elementProvider.GetElementIdByCodename(type, "body_copy")).value);
            Assert.Equal(expected.TeaserImage, actual.Single(x => x.element.id == _elementProvider.GetElementIdByCodename(type, "teaser_image")).value);
            Assert.Equal(expected.RelatedArticles, actual.Single(x => x.element.id == _elementProvider.GetElementIdByCodename(type, "related_articles")).value);
            Assert.Equal(expected.Personas, actual.Single(x => x.element.id == _elementProvider.GetElementIdByCodename(type, "personas")).value);
        }

        private static void AssertElements(ComplexTestModel expected, ComplexTestModel actual)
        {
            Assert.Equal(expected.Title, actual.Title);
            Assert.Equal(expected.PostDate, actual.PostDate);
            Assert.Equal(expected.UrlPattern.Mode, actual.UrlPattern.Mode);
            Assert.Equal(expected.UrlPattern.Value, actual.UrlPattern.Value);
            Assert.Equal(expected.BodyCopy, actual.BodyCopy);
            AssertIdentifiers(expected.Personas?.Select(x => x.Id.Value), actual.Personas?.Select(x => x.Id.Value));
            AssertIdentifiers(expected.RelatedArticles?.Select(x => x.Id.Value), actual.RelatedArticles?.Select(x => x.Id.Value));
            AssertIdentifiers(expected.Personas?.Select(x => x.Id.Value), actual.Personas?.Select(x => x.Id.Value));

        }

        private static ComplexTestModel GetTestModel()
        {
            return new ComplexTestModel
            {
                Title = "text",
                PostDate = DateTime.Now,
                UrlPattern = new UrlSlug{ Value = "urlslug", Mode = "custom"},
                BodyCopy = "RichText",
                TeaserImage = new[] { Guid.NewGuid(), Guid.NewGuid() }.Select(AssetIdentifier.ById).ToArray(),
                RelatedArticles = new[] { Guid.NewGuid(), Guid.NewGuid() }.Select(ContentItemIdentifier.ById).ToArray(),
                Personas = new[] { Guid.NewGuid(), Guid.NewGuid() }.Select(TaxonomyTermIdentifier.ById).ToList(),
            };
        }

        private IEnumerable<dynamic> ToDynamic(ComplexTestModel model)
        {
            var type = typeof(ComplexTestModel);

            var elements = new List<dynamic> {
                new
                {
                    element = new { id = _elementProvider.GetElementIdByCodename(type, "title") },
                    value = model.Title
                },
                new
                {
                    element = new { id = _elementProvider.GetElementIdByCodename(type, "post_date") },
                    value = model.PostDate
                },
                new
                {
                    element = new { id = _elementProvider.GetElementIdByCodename(type, "url_pattern") },
                    value = model.UrlPattern.Value,
                    mode = model.UrlPattern.Mode
                },
                new
                {
                    element = new { id = _elementProvider.GetElementIdByCodename(type, "body_copy") },
                    value = model.BodyCopy
                },
                new
                {
                    element = new { id = _elementProvider.GetElementIdByCodename(type, "teaser_image") },
                    value = model.TeaserImage
                },
                new
                {
                    element = new { id = _elementProvider.GetElementIdByCodename(type, "related_articles") },
                    value = model.RelatedArticles
                },
                new
                {
                    element = new { id = _elementProvider.GetElementIdByCodename(type, "personas") },
                    value = model.Personas
                }
            };

            var serialized = JsonConvert.SerializeObject(elements, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return JsonConvert.DeserializeObject<IEnumerable<dynamic>>(serialized, new JsonSerializerSettings { Converters = new JsonConverter[] { new DynamicObjectJsonConverter() } });
        }

        private static void AssertIdentifiers(IEnumerable<Guid> expected, IEnumerable<Guid> actual)
        {
            if (expected == null && actual == null) return;
            if (expected == null || actual == null)
            {
                Assert.True(false, "Null check");
            }
            Assert.Equal(expected, actual);
        }
    }
}