﻿// ----------------------------------------------------------------------------------
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
    using System.Linq;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Diagnostics.CodeAnalysis;
    using System.Collections;

    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    public class Records
    {
        private Dictionary<string, Queue<RecordEntry>> sessionRecords;

        private IRecordMatcher matcher;

        public Records(IRecordMatcher matcher)
            : this(new Dictionary<string, Queue<RecordEntry>>(), matcher) { }

        public Records(Dictionary<string, Queue<RecordEntry>> records, IRecordMatcher matcher)
        {
            this.sessionRecords = new Dictionary<string, Queue<RecordEntry>>(records);
            this.matcher = matcher;
        }

        public void Enqueue(RecordEntry record)
        {
            string recordKey = matcher.GetMatchingKey(record);
            if (!sessionRecords.ContainsKey(recordKey))
            {
                sessionRecords[recordKey] = new Queue<RecordEntry>();
            }
            sessionRecords[recordKey].Enqueue(record);
        }

        public Queue<RecordEntry> this[string key]
        {
            get { return sessionRecords[key]; }
            set { sessionRecords[key] = value; }
        }

        public IEnumerable<RecordEntry> GetAllEntities()
        {
            foreach (var queues in sessionRecords.Values)
            {
                while (queues.Count > 0)
                {
                    yield return queues.Dequeue();
                }
            }
        }

        public void EnqueueRange(List<RecordEntry> records)
        {
            foreach (RecordEntry recordEntry in records)
            {
                Enqueue(recordEntry);
            }
        }
    }
}
