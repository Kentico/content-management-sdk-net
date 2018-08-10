﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

using KenticoCloud.ContentManagement.Modules.HttpClient;

using Newtonsoft.Json;
using Xunit;

namespace KenticoCloud.ContentManagement.Tests.Mocks
{
    public class FileSystemHttpClientMock : IContentManagementHttpClient
    {
        private const string PROJECT_ID_REPLACEMENT = "{PROJECT_ID}";
        private const string API_KEY_REPLACEMENT = "{API_KEY}";

        private ContentManagementOptions _options;
        private bool _saveToFileSystem;
        private string _directoryName;
        private bool _firstRequest = true;

        private IContentManagementHttpClient _nativeClient = new ContentManagementHttpClient();

        public FileSystemHttpClientMock(ContentManagementOptions options, bool saveToFileSystem, string testName)
        {
            _saveToFileSystem = saveToFileSystem;
            _options = options;
            _directoryName = GetTestNameIdentifier(testName);
        }

        public string MakeProjectAgnostic(string data)
        {
            return data.Replace(_options.ProjectId, PROJECT_ID_REPLACEMENT).Replace(_options.ApiKey, API_KEY_REPLACEMENT);
        }

        public string ApplyProjectData(string data)
        {
            return data.Replace(PROJECT_ID_REPLACEMENT, _options.ProjectId).Replace(API_KEY_REPLACEMENT, _options.ApiKey);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message)
        {
            var isFirst = _firstRequest;
            _firstRequest = false;

            var serializationSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };

            var serializedRequest = MakeProjectAgnostic(JsonConvert.SerializeObject(message, serializationSettings));
            var serializedRequestContent = await SerializeContent(message.Content);

            var hashContent = $"{message.Method} {serializedRequest} {UnifySerializedRequestContent(serializedRequestContent)}";
            var folderPath = GetMockFileFolder(message, hashContent);

            if (_saveToFileSystem)
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                else if (isFirst)
                {
                    // Cleanup previously recorded data at first request to avoid data overlap upon change
                    Directory.Delete(folderPath, true);
                }
                
                var response = await _nativeClient.SendAsync(message);

                File.WriteAllText(Path.Combine(folderPath, "request.json"), serializedRequest);
                File.WriteAllText(Path.Combine(folderPath, "request_content.json"), serializedRequestContent);

                var serializedResponse = MakeProjectAgnostic(JsonConvert.SerializeObject(response, serializationSettings));
                var serializedResponseContent = await SerializeContent(response.Content);

                File.WriteAllText(Path.Combine(folderPath, "response.json"), serializedResponse);
                File.WriteAllText(Path.Combine(folderPath, "response_content.json"), serializedResponseContent);

                return response;
            }
            else
            {
                // Expected request is validated through the presence of the recorded files
                Assert.True(
                    Directory.Exists(folderPath),
                    $"Cannot find expected data folder {folderPath} for {message.Method} request to {message.RequestUri}. " + Environment.NewLine +
                    $"Either the request properties or content seem to differ from the expected recorded state." + Environment.NewLine +
                    $"Request:" + Environment.NewLine +
                    serializedRequest + Environment.NewLine +
                    $"Request content:" + Environment.NewLine +
                    serializedRequestContent
                );

                var serializedResponse = ApplyProjectData(File.ReadAllText(Path.Combine(folderPath, "response.json")));
                var serializedResponseContent = File.ReadAllText(Path.Combine(folderPath, "response_content.json"));

                var deserializationSettings = new JsonSerializerSettings
                {
                    ContractResolver = new IgnoreHttpContentContractResolver()
                };
                var response = JsonConvert.DeserializeObject<HttpResponseMessage>(serializedResponse, deserializationSettings);
                response.Content = new StringContent(serializedResponseContent);

                return response;
            }
        }

        private async Task<string> SerializeContent(HttpContent content)
        {
            if (content == null)
            {
                return null;
            }

            return await content.ReadAsStringAsync();
        }

        private string GetMockFileFolder(HttpRequestMessage message, string hashContent)
        {
            var rootPath = Path.Combine(Environment.CurrentDirectory, "Data\\");
            var testPath = Path.Combine(rootPath, _directoryName);
            var stringMessageHash = GetHashFingerprint(hashContent);

            var uniqueRequestPath = Path.Combine(testPath, $"{message.Method}_{stringMessageHash}");

            return uniqueRequestPath;
        }

        /// <summary>
        /// There is a limit in path length in test framework and git.
        /// This method shortens test name but persists test area (substring before first underscore).
        /// </summary>
        private string GetTestNameIdentifier(string testName)
        {
            var testFeature = testName.Split('_')[0];
            var testNameHash = GetHashFingerprint(testName);

            return $"{testFeature}_{testNameHash}";
        }

        private string GetHashFingerprint(string input)
        {
            var hashingAlgorithm = SHA1.Create();
            var fingerprint = hashingAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            return Convert.ToBase64String(fingerprint).Replace('+', '-').Replace('/', '_').Substring(0, 10);
        }

        private string UnifySerializedRequestContent(string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                return content.Replace("\\r", string.Empty);
            }

            return string.Empty;
        }
    }
}
