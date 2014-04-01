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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Utilities.HttpRecorder
{
    public class HttpMockServer : DelegatingHandler
    {
        private const string recordDir = "SessionRecords";
        private const string modeEnvironmentVariableName = "AZURE_TEST_MODE";
        private static AssetNames names;
        private static List<RecordEntry> sessionRecords;
        private static List<HttpMockServer> servers;
        private static Records records;
        private static bool initialized;

        public static HttpRecorderMode Mode { get; set; }
        public static IRecordMatcher Matcher { get; set; }
        public static string InputDirectory { get; set; }
        public static string OutputDirectory { get; set; }
        public static string CallerIdentity { get; set; }
        public static string TestIdentity { get; set; }

        static HttpMockServer()
        {
            Matcher = new SimpleRecordMatcher();
            records = new Records(Matcher);
        }

        private HttpMockServer() { }

        public static void Initialize(Type callerIdentity, string testIdentity, HttpRecorderMode mode)
        {
            CallerIdentity = callerIdentity.Name;
            TestIdentity = testIdentity;
            Mode = mode;
            names = new AssetNames();
            sessionRecords = new List<RecordEntry>();
            servers = new List<HttpMockServer>();

            if (Mode == HttpRecorderMode.Playback)
            {
                if (Directory.Exists(RecordsDirectory))
                {
                    foreach (string recordsFile in Directory.GetFiles(RecordsDirectory, testIdentity + "*.json"))
                    {
                        RecordEntryPack pack = RecordEntryPack.Deserialize(recordsFile);
                        sessionRecords.AddRange(pack.Entries);
                        foreach (var func in pack.Names.Keys)
                        {
                            pack.Names[func].ForEach(n => names.Enqueue(func, n));
                        }
                    }
                }
                records.EnqueueRange(sessionRecords);
            }

            initialized = true;
        }

        public static void Initialize(Type callerIdentity, string testIdentity)
        {
            Initialize(callerIdentity, testIdentity, GetCurrentMode());
        }

        public static HttpMockServer CreateInstance()
        {
            if (!initialized)
            {
                throw new ApplicationException("HttpMockServer has not been initialized yet. Use HttpMockServer.Initialize() method to initialize the HttpMockServer.");
            }
            HttpMockServer server = new HttpMockServer();
            servers.Add(server);
            return server;
        }

        public static string RecordsDirectory
        {
            get
            {
                string dirName = InputDirectory ?? Path.Combine(recordDir, CallerIdentity);
                return dirName;
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Mode == HttpRecorderMode.Playback)
            {
                // Will throw KeyNotFoundException if the request is not recorded
                var result = records[Matcher.GetMatchingKey(request)].Dequeue().GetResponse();
                result.RequestMessage = request;
                return TaskEx.FromResult(result);
            }
            else
            {
                return base.SendAsync(request, cancellationToken).ContinueWith<HttpResponseMessage>(response =>
                {
                    HttpResponseMessage result = response.Result;
                    if (Mode == HttpRecorderMode.Record)
                    {
                        RecordEntry recordEntry = new RecordEntry(result);
                        records.Enqueue(new RecordEntry(result));
                        sessionRecords.Add(recordEntry);
                    }

                    return result;
                });
            }
        }

        public static string GetAssetName(string testName, string prefix)
        {
            if (Mode == HttpRecorderMode.Playback)
            {
                return names[testName].Dequeue();
            }
            else
            {
                string generated = prefix + new Random().Next(9999);

                if (names.ContainsKey(testName))
                {
                    while (names[testName].Any(n => n.Equals(generated)))
                    {
                        generated = prefix + new Random().Next(9999);
                    }
                }
                names.Enqueue(testName, generated);

                return generated;
            }
        }

        public void InjectRecordEntry(RecordEntry record)
        {
            if (Mode == HttpRecorderMode.Playback)
            {
                records.Enqueue(record);
            }
        }

        public static void Flush(string outputPath = null)
        {
            if (Mode == HttpRecorderMode.Record)
            {
                RecordEntryPack pack = new RecordEntryPack();

                foreach (RecordEntry recordEntry in sessionRecords)
                {
                    pack.Entries.Add(recordEntry);
                }

                string fileDirectory = outputPath ?? RecordsDirectory;
                string fileName = (TestIdentity ?? "record") + ".json";

                Utilities.EnsureDirectoryExists(fileDirectory);
                
                pack.Names = names.Names;
                pack.Serialize(Utilities.GetUniqueFileName(Path.Combine(fileDirectory, fileName)));
            }

            servers.ForEach(s => s.Dispose());
        }

        private static HttpRecorderMode GetCurrentMode()
        {
            string input = Environment.GetEnvironmentVariable(modeEnvironmentVariableName);
            HttpRecorderMode mode;

            if (string.IsNullOrEmpty(input))
            {
                mode = HttpRecorderMode.None;
            }
            else
            {
                mode = (HttpRecorderMode)Enum.Parse(typeof(HttpRecorderMode), input, true);
            }

            return mode;
        }

    }
}