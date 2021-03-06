﻿using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;

namespace Kentico.Kontent.Management.Modules.Extensions
{
    internal static class HttpRequestHeadersExtensions
    {
        private const string SdkTrackingHeaderName = "X-KC-SDKID";

        private const string PackageRepositoryHost = "nuget.org";

        private static readonly Lazy<string> SdkVersion = new Lazy<string>(GetSdkVersion);
        private static readonly Lazy<string> SdkPackageId = new Lazy<string>(GetSdkPackageId);


        internal static void AddSdkTrackingHeader(this HttpRequestHeaders header)
        {
            header.Add(SdkTrackingHeaderName, GetSdkTrackingHeader());
        }

        internal static string GetSdkTrackingHeader()
        {
            return $"{PackageRepositoryHost};{SdkPackageId.Value};{SdkVersion.Value}";
        }

        private static string GetSdkVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            var sdkVersion = fileVersionInfo.ProductVersion;

            return sdkVersion;
        }

        private static string GetSdkPackageId()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var sdkPackageId = assembly.GetName().Name;

            return sdkPackageId;

        }
    }
}
