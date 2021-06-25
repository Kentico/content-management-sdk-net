using Kentico.Kontent.Management.Models.Items;
using Kentico.Kontent.Management.Modules.ActionInvoker;
using Kentico.Kontent.Management.Modules.HttpClient;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Kentico.Kontent.Management.Models.Assets;
using Xunit;
using System.Collections.Generic;

namespace Kentico.Kontent.Management.Tests
{
    internal class FakeManagementHttpClient : IManagementHttpClient
    {
        internal string requestBody;

        public async Task<HttpResponseMessage> SendAsync(IMessageCreator messageCreator, string endpointUrl, HttpMethod method, HttpContent content = null, Dictionary<string, string> headers = null)
        {
            var message = messageCreator.CreateMessage(method, endpointUrl, content);
            requestBody = await message.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent(requestBody)
            };
        }
    }

    public class ActionInvokerTests
    {
        [Theory]
        [Trait("Issue", "29")]
        [InlineData(0.0, "0")]
        [InlineData(29.0, "29.0")]
        public void ActionInvokerSerializeElementContainingZero_SerializedJsonContainsZero(decimal d, string s)
        {
            var httpClient = new FakeManagementHttpClient();
            var actionInvoker = new ActionInvoker(httpClient, new MessageCreator("{api_key}"));

            var contentItemVariantUpsertModel = new ContentItemVariantUpsertModel()
            {
                Elements = new List<dynamic>
                {
                    new
                    {
                        zero = d,
                        optZero = new decimal?(d),
                    }
                },
            };

            var result = actionInvoker.InvokeMethodAsync<ContentItemVariantUpsertModel, dynamic>("{endpoint_url}", HttpMethod.Get, contentItemVariantUpsertModel);
            Assert.Equal($"{{\"elements\":[{{\"zero\":{s},\"optZero\":{s}}}]}}", httpClient.requestBody);
        }
        
        [Fact]
        public async Task ActionInvokerSerializeEnum_EnumIsSerializedAsString()
        {
            var httpClient = new FakeManagementHttpClient();
            var actionInvoker = new ActionInvoker(httpClient, new MessageCreator("{api_key}"));
            
            var assetUpsertModel = new AssetUpsertModel()
            {
                Title = "Asset",
                Descriptions = new []
                {
                    new AssetDescription()
                    {
                        Description = "Description",
                        Language = LanguageIdentifier.DEFAULT_LANGUAGE
                    }, 
                },
                FileReference = new FileReference()
                {
                    Id = "ab7bdf75-781b-4bf9-aed8-501048860402",
                    Type = FileReferenceTypeEnum.Internal
                }
            };

            await actionInvoker.InvokeMethodAsync<AssetUpsertModel, dynamic>("{endpoint_url}", HttpMethod.Put, assetUpsertModel);

            var expectedRequestBody = "{\"file_reference\":{\"id\":\"ab7bdf75-781b-4bf9-aed8-501048860402\",\"type\":\"internal\"},\"descriptions\":[{\"language\":{\"id\":\"00000000-0000-0000-0000-000000000000\"},\"description\":\"Description\"}],\"title\":\"Asset\"}";
            Assert.Equal(expectedRequestBody, httpClient.requestBody);
        }
    }
}
