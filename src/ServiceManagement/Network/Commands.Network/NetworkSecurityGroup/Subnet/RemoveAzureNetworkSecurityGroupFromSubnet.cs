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
using Microsoft.Azure.Commands.Network.Properties;

namespace Microsoft.Azure.Commands.Network.NetworkSecurityGroup.Subnet
{
    [Cmdlet(VerbsCommon.Remove, "AzureNetworkSecurityGroup"), OutputType(typeof(bool))]
    public class RemoveAzureNetworkSecurityGroupFromSubnet : NetworkCmdletBase
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = false)]
        [ValidateNotNullOrEmpty]
        public string VirtualNetworkName { get; set; }

        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = false)]
        [ValidateNotNullOrEmpty]
        public string SubnetName { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        public override void ExecuteCmdlet()
        {
            ConfirmAction(
                Force.IsPresent,
                string.Format(Resources.RemoveNetworkSecurityGroupFromSubnetWarning, Name, SubnetName, VirtualNetworkName),
                Resources.RemoveNetworkSecurityGroupFromSubnetWarning,
                Name,
                () =>
                {
                    Client.RemoveNetworkSecurityGroupFromSubnet(Name, SubnetName, VirtualNetworkName);

                    WriteVerboseWithTimestamp(Resources.RemoveNetworkSecurityGroupFromSubnetSucceeded, Name, VirtualNetworkName, SubnetName);
                    if (PassThru.IsPresent)
                    {
                        WriteObject(true);
                    }
                });
        }
    }
}
