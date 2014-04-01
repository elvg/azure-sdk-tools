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

using Microsoft.WindowsAzure.Utilities.HttpRecorder;

namespace Microsoft.WindowsAzure.Utilities.HttpRecorder
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;

    /// <summary>
    /// This class does a simple mapping between given request and responses.
    /// The hashing algorithm works by combining the HTTP method of the request
    /// plus the request uri together. Optionally a key-value pair of headers
    /// can be added to the key.
    /// </summary>
    public class SimpleRecordMatcher : IRecordMatcher
    {
        public Dictionary<string, List<string>> MatchingHeaders { get; private set; }

        public SimpleRecordMatcher()
        {
            MatchingHeaders = new Dictionary<string, List<string>>();
        }

        public SimpleRecordMatcher(IDictionary<string, List<string>> matchingHeaders)
        {
            MatchingHeaders = new Dictionary<string, List<string>>(matchingHeaders);
        }

        private string GetMatchingKey(string httpMethod, string requestUri)
        {
            try
            {
                requestUri = new Uri(requestUri).PathAndQuery;
            }
            catch
            {
                // Ignore
            }
            StringBuilder key = new StringBuilder(string.Format("{0} {1}", httpMethod, requestUri));

            foreach (KeyValuePair<string, List<string>> item in MatchingHeaders)
            {
                StringBuilder matchedValues = new StringBuilder();
                for (int i = 0; i < item.Value.Count; i++)
                {
                    matchedValues.AppendFormat("{0}{1}", item.Value[i], i < item.Value.Count - 1 ? "," : string.Empty);
                }

                key.AppendFormat(" {{0}={1}}", item.Key, matchedValues);
            }

            return key.ToString();
        }

        public string GetMatchingKey(RecordEntry recordEntry)
        {
            return GetMatchingKey(recordEntry.RequestMethod, recordEntry.RequestUri.ToString());
        }

        public string GetMatchingKey(HttpRequestMessage request)
        {
            return GetMatchingKey(request.Method.Method, request.RequestUri.ToString());
        }   
    }
}
