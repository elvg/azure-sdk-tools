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

using Microsoft.WindowsAzure.Commands.Common.Factories;
using Microsoft.WindowsAzure.Commands.Common.Models;
using System.Diagnostics;

namespace Microsoft.WindowsAzure.Commands.Common
{
    public static class AzurePowerShell
    {
        private static IClientFactory clientFactory = null;
        
        private static AzureProfile profile = null;

        private static IAuthenticationFactory authenticationFactory = null;

        public const string AssemblyCompany = "Microsoft";

        public const string AssemblyProduct = "Microsoft Azure Powershell";

        public const string AssemblyCopyright = "Copyright © Microsoft";

        public const string AssemblyVersion = "0.8.7.1";

        public const string AssemblyFileVersion = "0.8.7.1";

        public static AzureProfile Profile
        {
            get
            {
                if (profile == null)
                {
                    profile = new AzureProfile();
                }

                return profile;
            }

            set
            {
                Debug.Assert(value != null, "The profile must have a value.");
                profile = value;
            }
        }

        public static IClientFactory ClientFactory
        {
            get
            {
                if (clientFactory == null)
                {
                    clientFactory = new ClientFactory();
                }

                return clientFactory;
            }

            set
            {
                Debug.Assert(value != null, "The client factory must have a value.");
                clientFactory = value;
            }
        }

        public static IAuthenticationFactory AuthenticationFactory
        {
            get
            {
                if (authenticationFactory == null)
                {
                    authenticationFactory = new AuthenticationFactory();
                }

                return authenticationFactory;
            }

            set
            {
                Debug.Assert(value != null, "The authentication factory must have a value.");
                authenticationFactory = value;
            }
        }
    }
}
