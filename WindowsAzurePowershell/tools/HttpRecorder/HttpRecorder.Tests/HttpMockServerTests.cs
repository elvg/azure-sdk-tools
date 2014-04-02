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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.WindowsAzure.Utilities.HttpRecorder;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HttpRecorder.Tests
{
    public class HttpMockServerTests : IDisposable
    {
        private string currentDir;
        private RecordedDelegatingHandler recordingHandler;
        private RecordedDelegatingHandler recordingHandlerWithBadResponse;
        public HttpMockServerTests()
        {
            currentDir = Environment.CurrentDirectory;
            recordingHandler = new RecordedDelegatingHandler(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{'error':'message'}")
                });
            recordingHandler.StatusCodeToReturn = HttpStatusCode.OK;
            recordingHandlerWithBadResponse = new RecordedDelegatingHandler(new HttpResponseMessage(HttpStatusCode.Conflict));
            recordingHandlerWithBadResponse.StatusCodeToReturn = HttpStatusCode.Conflict;
        }

        private FakeHttpClient CreateClient()
        {
            return new FakeHttpClient().WithHandlers(new DelegatingHandler[] { recordingHandler, HttpMockServer.CreateInstance() });
        }

        private FakeHttpClient CreateClientWithBadResult()
        {
            return new FakeHttpClient().WithHandlers(new DelegatingHandler[] { recordingHandlerWithBadResponse, HttpMockServer.CreateInstance() });
        }

        [Fact]
        public void TestRecordingWithOneClientWritesFile()
        {
            HttpMockServer.Initialize(this.GetType(), Utilities.GetCurrentMethodName(), HttpRecorderMode.Record);
            FakeHttpClient client = CreateClient();
            var result = client.DoStuffA().Result;

            HttpMockServer.Flush(currentDir);

            Assert.True(File.Exists(Path.Combine(HttpMockServer.CallerIdentity, Utilities.GetCurrentMethodName() + ".json")));
        }

        [Fact]
        public void TestRecordingWithTwoClientsWritesFile()
        {
            HttpMockServer.Initialize(this.GetType(), Utilities.GetCurrentMethodName(), HttpRecorderMode.Record);
            FakeHttpClient client1 = CreateClient();
            FakeHttpClient client2 = CreateClient();
            var result1 = client1.DoStuffA().Result;
            var result2 = client2.DoStuffA().Result;

            HttpMockServer.Flush(currentDir);

            Assert.True(File.Exists(Path.Combine(HttpMockServer.CallerIdentity, Utilities.GetCurrentMethodName() + ".json")));
        }

        [Fact]
        public void TestPlaybackWithOneClient()
        {
            HttpMockServer.RecordsDirectory = currentDir;
            HttpMockServer.Initialize(this.GetType(), Utilities.GetCurrentMethodName(), HttpRecorderMode.Record);
            FakeHttpClient client1 = CreateClient();
            var result1A = client1.DoStuffA().Result;
            var result1B = client1.DoStuffB().Result;
            string assetName1 = HttpMockServer.GetAssetName(Utilities.GetCurrentMethodName(), "tst");
            string assetName2 = HttpMockServer.GetAssetName(Utilities.GetCurrentMethodName(), "tst");
            HttpMockServer.Flush(currentDir);

            HttpMockServer.Initialize(this.GetType(), Utilities.GetCurrentMethodName(), HttpRecorderMode.None);
            FakeHttpClient client2 = CreateClientWithBadResult();
            var result2 = client2.DoStuffA().Result;
            HttpMockServer.Flush(currentDir);

            HttpMockServer.Initialize(this.GetType(), Utilities.GetCurrentMethodName(), HttpRecorderMode.Playback);
            FakeHttpClient client3 = CreateClientWithBadResult();
            var result3B = client3.DoStuffB().Result;
            var result3A = client3.DoStuffA().Result;
            string assetName1Playback = HttpMockServer.GetAssetName(Utilities.GetCurrentMethodName(), "tst");
            string assetName2Playback = HttpMockServer.GetAssetName(Utilities.GetCurrentMethodName(), "tst");
            HttpMockServer.Flush(currentDir);

            string result1AConent = JObject.Parse(result1A.Content.ReadAsStringAsync().Result).ToString();
            string result3AConent = JObject.Parse(result3A.Content.ReadAsStringAsync().Result).ToString();

            Assert.True(File.Exists(Path.Combine(HttpMockServer.CallerIdentity, Utilities.GetCurrentMethodName() + ".json")));
            Assert.Equal(result1A.StatusCode, result3A.StatusCode);
            Assert.Equal(result1A.RequestMessage.RequestUri.AbsoluteUri, result3A.RequestMessage.RequestUri.AbsoluteUri);
            Assert.Equal(result1AConent, result3AConent);
            Assert.Equal(HttpStatusCode.Conflict, result2.StatusCode);
            Assert.Equal(assetName1, assetName1Playback);
            Assert.Equal(assetName2, assetName2Playback);
        }

        [Fact]
        public void TestRecordingWithTwoMethodsWritesFile()
        {
            HttpMockServer.Initialize(this.GetType(), "testA", HttpRecorderMode.Record);
            FakeHttpClient client1 = CreateClient();
            FakeHttpClient client2 = CreateClient();
            var result1 = client1.DoStuffA().Result;
            var result2 = client2.DoStuffA().Result;
            HttpMockServer.Flush(currentDir);

            HttpMockServer.Initialize(this.GetType(), "testB", HttpRecorderMode.Record);
            FakeHttpClient client3 = CreateClient();
            var result3 = client3.DoStuffA().Result;
            HttpMockServer.Flush(currentDir);

            Assert.True(File.Exists(HttpMockServer.CallerIdentity + "\\testA.json"));
            Assert.True(File.Exists(HttpMockServer.CallerIdentity + "\\testB.json"));
        }

        [Fact]
        public void TestRecordingWithTwoMethodsWritesAllData()
        {
            HttpMockServer.Initialize(this.GetType(), "testA", HttpRecorderMode.Record);
            FakeHttpClient client1 = CreateClient();
            FakeHttpClient client2 = CreateClient();
            var result1 = client1.DoStuffA().Result;
            var result2 = client2.DoStuffA().Result;
            var name = HttpMockServer.GetAssetName("testA", "tst");
            HttpMockServer.Flush(currentDir);
            RecordEntryPack pack = RecordEntryPack.Deserialize(HttpMockServer.CallerIdentity + "\\testA.json");

            Assert.NotNull(name);
            Assert.True(File.Exists(HttpMockServer.CallerIdentity + "\\testA.json"));
            Assert.Equal(2, pack.Entries.Count);
            Assert.Equal(1, pack.Names["testA"].Count);
        }

        [Fact]
        public void NoneModeCreatesNoFiles()
        {
            HttpMockServer.Initialize(this.GetType(), Utilities.GetCurrentMethodName(), HttpRecorderMode.None);
            FakeHttpClient client = CreateClient();
            var result = client.DoStuffA().Result;

            HttpMockServer.Flush(currentDir);

            Assert.False(File.Exists(Utilities.GetCurrentMethodName() + ".json"));
        }

        [Fact]
        public void TestRecordingWithExplicitDir()
        {
            HttpMockServer.Initialize(this.GetType(), Utilities.GetCurrentMethodName(), HttpRecorderMode.Record);
            HttpMockServer.RecordsDirectory = Path.GetTempPath();

            FakeHttpClient client = CreateClient();
            var result = client.DoStuffA().Result;

            HttpMockServer.Flush();

            Assert.True(File.Exists(Path.Combine(Path.GetTempPath(), this.GetType().Name, Utilities.GetCurrentMethodName() + ".json")));
        }

        [Fact]
        public void TestPlaybackWithAssetInUrlClient()
        {
            HttpMockServer.RecordsDirectory = currentDir;
            HttpMockServer.Initialize(this.GetType(), Utilities.GetCurrentMethodName(), HttpRecorderMode.Record);
            FakeHttpClient client1 = CreateClient();
            string assetName1 = HttpMockServer.GetAssetName(Utilities.GetCurrentMethodName(), "tst");
            string assetName2 = HttpMockServer.GetAssetName(Utilities.GetCurrentMethodName(), "tst");
            var result1A = client1.DoStuffX(assetName1).Result;
            var result1B = client1.DoStuffX(assetName2).Result;
            HttpMockServer.Flush(currentDir);

            HttpMockServer.Initialize(this.GetType(), Utilities.GetCurrentMethodName(), HttpRecorderMode.Playback);
            FakeHttpClient client3 = CreateClientWithBadResult();
            string assetName1Playback = HttpMockServer.GetAssetName(Utilities.GetCurrentMethodName(), "tst");
            string assetName2Playback = HttpMockServer.GetAssetName(Utilities.GetCurrentMethodName(), "tst");
            var result3A = client3.DoStuffX(assetName1Playback).Result;
            var result3B = client3.DoStuffX(assetName2Playback).Result;
            HttpMockServer.Flush(currentDir);

            string result1AConent = JObject.Parse(result1A.Content.ReadAsStringAsync().Result).ToString();
            string result3AConent = JObject.Parse(result3A.Content.ReadAsStringAsync().Result).ToString();

            Assert.True(File.Exists(Path.Combine(HttpMockServer.CallerIdentity, Utilities.GetCurrentMethodName() + ".json")));
            Assert.Equal(result1A.StatusCode, result3A.StatusCode);
            Assert.Equal(result1A.RequestMessage.RequestUri.AbsoluteUri, result3A.RequestMessage.RequestUri.AbsoluteUri);
            Assert.Equal(result1AConent, result3AConent);
            Assert.Equal(assetName1, assetName1Playback);
            Assert.Equal(assetName2, assetName2Playback);
        }

        public void Dispose()
        {
            foreach (var file in Directory.GetFiles(currentDir, "*.json", SearchOption.AllDirectories))
            {
                File.Delete(file);
            }
        }
    }
}
