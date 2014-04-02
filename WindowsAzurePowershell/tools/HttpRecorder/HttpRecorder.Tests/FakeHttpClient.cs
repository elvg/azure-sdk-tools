// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Common;

namespace HttpRecorder.Tests
{
    public class FakeHttpClient : ServiceClient<FakeHttpClient>
    {
        public FakeHttpClient()
        {
        }

        public async Task<HttpResponseMessage> DoStuffA()
        {
            // Construct URL
            string url = "http://www.microsoft.com/path/to/resourceA";

            // Create HTTP transport objects
            HttpRequestMessage httpRequest = null;

            httpRequest = new HttpRequestMessage();
            httpRequest.Method = HttpMethod.Get;
            httpRequest.RequestUri = new Uri(url);

            // Set Headers
            httpRequest.Headers.Add("x-ms-version", "2013-11-01");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "abcdefg");

            // Set Credentials
            var cancellationToken = new CancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            return await HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        }

        public async Task<HttpResponseMessage> DoStuffB()
        {
            // Construct URL
            string url = "http://www.microsoft.com/path/to/resourceB";

            // Create HTTP transport objects
            HttpRequestMessage httpRequest = null;

            httpRequest = new HttpRequestMessage();
            httpRequest.Method = HttpMethod.Get;
            httpRequest.RequestUri = new Uri(url);

            // Set Headers
            httpRequest.Headers.Add("x-ms-version", "2013-11-01");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "xyz123");

            // Set Credentials
            var cancellationToken = new CancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            return await HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        }

        public async Task<HttpResponseMessage> DoStuffX(string assetName)
        {
            // Construct URL
            string url = "http://www.microsoft.com/path/to/resource/" + assetName;

            // Create HTTP transport objects
            HttpRequestMessage httpRequest = null;

            httpRequest = new HttpRequestMessage();
            httpRequest.Method = HttpMethod.Get;
            httpRequest.RequestUri = new Uri(url);

            // Set Headers
            httpRequest.Headers.Add("x-ms-version", "2013-11-01");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "xyz123");

            // Set Credentials
            var cancellationToken = new CancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            return await HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        }

        public override FakeHttpClient WithHandler(DelegatingHandler handler)
        {
            return WithHandler(new FakeHttpClient(), handler);
        }
    }
}
