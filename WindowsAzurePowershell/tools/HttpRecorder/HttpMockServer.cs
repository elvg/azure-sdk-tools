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
        private static string namesPath = "assetNames.json";

        private static string recordDir = "SessionRecords";

        private static string modeEnvironmentVariableName = "AZURE_TEST_MODE";

        private static AssetNames names;

        private static List<RecordEntry> sessionRecords;

        private static int instanceCount;

        public static HttpRecorderMode Mode { get; set; }

        public static bool CleanRecordsDirectory { get; set; }

        public static string OutputDirectory { get; set; }

        private IRecordMatcher matcher;

        public Records Records { get; private set; }

        public string RecordsDirectory
        {
            get
            {
                string dirName = Path.Combine(recordDir, Identity);
                if (Mode == HttpRecorderMode.Record)
                {
                    dirName = Path.Combine(OutputDirectory, dirName);
                }
                return dirName;
            }
        }

        public string Identity { get; private set; }

        private static HttpRecorderMode GetCurrentMode()
        {

            string input =  Environment.GetEnvironmentVariable(modeEnvironmentVariableName);
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

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (Mode == HttpRecorderMode.Playback)
            {
                // Will throw KeyNotFoundException if the request is not recorded
                return TaskEx.FromResult(Records[matcher.GetMatchingKey(request)].Dequeue().GetResponse());
            }
            else
            {
                return base.SendAsync(request, cancellationToken).ContinueWith<HttpResponseMessage>(response =>
                {
                    HttpResponseMessage result = response.Result;
                    if (Mode == HttpRecorderMode.Record)
                    {
                        RecordEntry recordEntry = new RecordEntry(result);
                        Records.Enqueue(new RecordEntry(result));
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            instanceCount--;

            if (Mode == HttpRecorderMode.Record && instanceCount == 0)
            {
                Utilities.EnsureDirectoryExists(RecordsDirectory);

                if (CleanRecordsDirectory)
                {
                    Utilities.CleanDirectory(RecordsDirectory);
                }
                
                int count = 0;
                const int packSize = 20;
                List<RecordEntry> pack = new List<RecordEntry>();

                foreach (RecordEntry recordEntry in sessionRecords)
                {
                    pack.Add(recordEntry);

                    if (pack.Count == packSize)
                    {
                        Utilities.SerializeJson<List<RecordEntry>>(
                            pack,
                            Path.Combine(RecordsDirectory, string.Format("record{0}.json", count++)));
                        pack.Clear();
                    }
                }

                if (pack.Count != 0)
                {
                    Utilities.SerializeJson<List<RecordEntry>>(
                        pack,
                        Path.Combine(RecordsDirectory, string.Format("record{0}.json", count++)));
                }

                Utilities.SerializeJson(names.Names, namesPath);
            }
        }

        public void InjectRecordEntry(RecordEntry record)
        {
            if (Mode == HttpRecorderMode.Playback)
            {
                Records.Enqueue(record);
            }
        }

        static HttpMockServer()
        {
            names = new AssetNames();
            sessionRecords = new List<RecordEntry>();
            instanceCount = 0;
            CleanRecordsDirectory = false;
            Mode = GetCurrentMode();
        }

        public HttpMockServer(IRecordMatcher matcher, Type callerIdentity)
        {
            instanceCount++;
            this.matcher = matcher;
            this.Identity = callerIdentity.Name;
            this.Records = new Records(matcher);

            if (Mode == HttpRecorderMode.Playback)
            {
                if (Directory.Exists(RecordsDirectory))
                {
                    foreach (string recordsFile in Directory.GetFiles(RecordsDirectory))
                    {
                        sessionRecords.AddRange(Utilities.DeserializeJson<List<RecordEntry>>(recordsFile));
                    }
                }

                Dictionary<string, string[]> savedNames = Utilities.DeserializeJson<Dictionary<string, string[]>>(namesPath)
                    ?? new Dictionary<string, string[]>();
                savedNames.ForEach(r => names.Enqueue(r.Key, r.Value));
                Records.EnqueueRange(sessionRecords);
            }
        }
    }
}