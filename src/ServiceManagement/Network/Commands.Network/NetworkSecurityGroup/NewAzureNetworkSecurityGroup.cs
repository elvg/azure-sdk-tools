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

using System.Management.Automation;
using Microsoft.Azure.Commands.Network.NetworkSecurityGroup.Model;

namespace Microsoft.Azure.Commands.Network.NetworkSecurityGroup
{
    [Cmdlet(VerbsCommon.New, "AzureNetworkSecurityGroup"), OutputType(typeof(INetworkSecurityGroup))]
    public class NewAzureNetworkSecurityGroup : NetworkCmdletBase
    {
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Location { get; set; }

        [Parameter(Position = 2, Mandatory = false)]
        [ValidateNotNullOrEmpty]
        public string Label { get; set; }

        public override void ExecuteCmdlet()
        {
            var networkSecurityGroup = Client.CreateNetworkSecurityGroup(Name, Location, Label);
            WriteObject(networkSecurityGroup);
        }
    }
}
