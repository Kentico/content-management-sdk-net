using Kentico.Kontent.Management.Modules.Extensions;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Kentico.Kontent.Management.Modules.ActionInvoker
{
    internal class MessageCreator : IMessageCreator
    {
        private readonly string _apiKey;

        public MessageCreator(string apiKey)
        {
            _apiKey = apiKey;
        }

        public HttpRequestMessage CreateMessage(HttpMethod method, string url, HttpContent content = null)
        {
            var message = new HttpRequestMessage(method, url);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            message.Content = content;
            message.Headers.AddSdkTrackingHeader();
            return message;
        }
    }
}