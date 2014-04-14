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

namespace Microsoft.WindowsAzure.Commands.Automation
{
    using System;
    using System.Management.Automation;
    using System.Security.Permissions;

    using Microsoft.WindowsAzure.Commands.Utilities.Automation.Models;

    /// <summary>
    /// Sets an azure automation schedule.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "AzureAutomationSchedule", DefaultParameterSetName = ByScheduleName)]
    [OutputType(typeof(Schedule))]
    public class SetAzureAutomationSchedule : AzureAutomationBaseCmdlet
    {
        /// <summary>
        /// The get schedule by schedule id parameter set.
        /// </summary>
        private const string ByScheduleId = "ByScheduleId";

        /// <summary>
        /// The get schedule by schedule name parameter set.
        /// </summary>
        private const string ByScheduleName = "ByScheduleName";

        /// <summary>
        /// Gets or sets the schedule id.
        /// </summary>
        [Parameter(ParameterSetName = ByScheduleId, Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true,
            HelpMessage = "The schedule id.")]
        public Guid? Id { get; set; }

        /// <summary>
        /// Gets or sets the schedule name.
        /// </summary>
        [Parameter(ParameterSetName = ByScheduleName, Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true,
            HelpMessage = "The schedule name.")]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the schedule description.
        /// </summary>
        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true,
            HelpMessage = "The schedule description.")]
        public string Description { get; set; }

        /// <summary>
        /// Execute this cmdlet.
        /// </summary>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        protected override void AutomationExecuteCmdlet()
        {
            if (this.Id.HasValue)
            {
                // ByScheduleId
                var schedule = this.AutomationClient.UpdateSchedule(
                    this.AutomationAccountName, this.Id.Value, this.Description);
                this.WriteObject(schedule);
            }
            else
            {
                // ByScheduleName
                var schedule = this.AutomationClient.UpdateSchedule(
                    this.AutomationAccountName, this.Name, this.Description);
                this.WriteObject(schedule);
            }
        }
    }
}