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

    using Microsoft.WindowsAzure.Commands.Utilities.Automation;
    using Microsoft.WindowsAzure.Commands.Utilities.Automation.Models;

    /// <summary>
    /// Creates an azure automation Schedule.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "AzureAutomationSchedule", DefaultParameterSetName = ByDaily)]
    [OutputType(typeof(Schedule))]
    public class NewAzureAutomationSchedule : AzureAutomationBaseCmdlet
    {
        /// <summary>
        /// The one time schedule parameter set.
        /// </summary>
        private const string ByOneTime = "ByOneTime";

        /// <summary>
        /// The daily schedule parameter set.
        /// </summary>
        private const string ByDaily = "ByDaily";

        /// <summary>
        /// The schedule expiry time.
        /// </summary>
        private DateTime expiryTime = Constants.DefaultScheduleExpiryTime;

        /// <summary>
        /// The day interval, whose default value is 1.
        /// </summary>
        private int dayInterval = Constants.DefaultDailyScheduleDayInterval;
        
        /// <summary>
        /// Gets or sets the schedule name.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true,
            HelpMessage = "The schedule name.")]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the schedule start time.
        /// </summary>
        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true, 
            HelpMessage = "The schedule start time.")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the schedule description.
        /// </summary>
        [Parameter(Position = 3, Mandatory = false, ValueFromPipelineByPropertyName = true,
            HelpMessage = "The schedule description.")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the switch parameter to create a one time schedule.
        /// </summary>
        [Parameter(ParameterSetName = ByOneTime, Mandatory = true, HelpMessage = "To create a one time schedule.")]
        public SwitchParameter OneTime { get; set; }

        /// <summary>
        /// Gets or sets the schedule expiry time.
        /// </summary>
        [Parameter(ParameterSetName = ByDaily, Position = 4, Mandatory = false, HelpMessage = "The schedule expiry time.")]
        public DateTime ExpiryTime
        {
            get
            {
                return this.expiryTime;
            }

            set
            {
                this.expiryTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the daily schedule day interval.
        /// </summary>
        [Parameter(ParameterSetName = ByDaily, Position = 5, Mandatory = false, HelpMessage = "The daily schedule day interval.")]
        public int DayInterval
        {
            get
            {
                return this.dayInterval;
            }

            set
            {
                this.dayInterval = value;
            }
        }

        /// <summary>
        /// Execute this cmdlet.
        /// </summary>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        protected override void AutomationExecuteCmdlet()
        {
            // Assume local time if DateTimeKind.Unspecified
            this.StartTime = this.StartTime.Kind == DateTimeKind.Unspecified
                                 ? DateTime.SpecifyKind(this.StartTime, DateTimeKind.Local)
                                 : this.StartTime;
            this.ExpiryTime = this.ExpiryTime.Kind == DateTimeKind.Unspecified
                                  ? DateTime.SpecifyKind(this.ExpiryTime, DateTimeKind.Local)
                                  : this.ExpiryTime;

            if (this.OneTime.IsPresent)
            {
                // ByOneTime
                var oneTimeSchedule = new OneTimeSchedule
                {
                    Name = this.Name,
                    StartTime = this.StartTime,
                    Description = this.Description,
                    ExpiryTime = this.ExpiryTime
                };

                var schedule = this.AutomationClient.CreateSchedule(this.AutomationAccountName, oneTimeSchedule);
                this.WriteObject(schedule);
            }
            else
            {
                // ByDaily
                var dailySchedule = new DailySchedule
                {
                    Name = this.Name,
                    StartTime = this.StartTime,
                    DayInterval = this.DayInterval,
                    Description = this.Description,
                    ExpiryTime = this.ExpiryTime
                };

                var schedule = this.AutomationClient.CreateSchedule(this.AutomationAccountName, dailySchedule);
                this.WriteObject(schedule);
            }
        }
    }
}
