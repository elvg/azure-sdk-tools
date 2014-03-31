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
using Microsoft.WindowsAzure.Commands.Utilities.Common;

namespace Microsoft.WindowsAzure.Commands.Common.Test.Common
{
    public class MockProfileStore : IProfileStore
    {
        public ProfileData ProfileData { get; set; }

        public MockProfileStore()
        {
            ResetData();
        }

        public void Save(ProfileData profile)
        {
            ProfileData.DefaultEnvironmentName = profile.DefaultEnvironmentName;
            ProfileData.Environments = new List<AzureEnvironmentData>();
            ProfileData.Subscriptions = new List<AzureSubscriptionData>();
            foreach (var env in profile.Environments)
            {
                ((List<AzureEnvironmentData>)ProfileData.Environments).Add(env);
            }
            foreach (var sub in profile.Subscriptions)
            {
                ((List<AzureSubscriptionData>)ProfileData.Subscriptions).Add(sub);
            }
        }

        public ProfileData Load()
        {
            return ProfileData;
        }

        public void DestroyData()
        {
            ResetData();
        }

        private void ResetData()
        {
            ProfileData = new ProfileData();
            ProfileData.DefaultEnvironmentName = EnvironmentName.AzureCloud;
            ProfileData.Environments = new List<AzureEnvironmentData>();
            ProfileData.Subscriptions = new List<AzureSubscriptionData>();
        }
    }
}
