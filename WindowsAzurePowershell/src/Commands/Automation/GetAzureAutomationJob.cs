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
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Security.Permissions;

    using Microsoft.WindowsAzure.Commands.Utilities.Automation;

    using Job = Microsoft.WindowsAzure.Commands.Utilities.Automation.Models.Job;

    /// <summary>
    /// Gets azure automation jobs for a given account, filtered by one of multiple criteria.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AzureAutomationJob", DefaultParameterSetName = ByAll)]
    [OutputType(typeof(Job))]
    public class GetAzureAutomationJob : AzureAutomationBaseCmdlet
    {
        /// <summary>
        /// The get job by job id parameter set.
        /// </summary>
        private const string ByJobId = "ByJobId";

        /// <summary>
        /// The get job by runbook id parameter set.
        /// </summary>
        private const string ByRunbookId = "ByRunbookId";

        /// <summary>
        /// The get job by runbook name parameter set.
        /// </summary>
        private const string ByRunbookName = "ByRunbookName";

        /// <summary>
        /// The get all parameter set.
        /// </summary>
        private const string ByAll = "ByAll";

        /// <summary>
        /// The start time.
        /// </summary>
        private DateTime startTime = Constants.DefaultJobFilterStartTime;

        /// <summary>
        /// The end time.
        /// </summary>
        private DateTime endTime = Constants.DefaultJobFilterEndTime;

        /// <summary>
        /// Gets or sets the job id.
        /// </summary>
        [Parameter(ParameterSetName = ByJobId, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The job id.")]
        [Alias("JobId")]
        public Guid? Id { get; set; }

        /// <summary>
        /// Gets or sets the runbook id of the job.
        /// </summary>
        [Parameter(ParameterSetName = ByRunbookId, Mandatory = true, HelpMessage = "The runbook id of the job.")]
        public Guid? RunbookId { get; set; }

        /// <summary>
        /// Gets or sets the runbook name of the job.
        /// </summary>
        [Parameter(ParameterSetName = ByRunbookName, Mandatory = true, HelpMessage = "The runbook name of the job.")]
        public string RunbookName { get; set; }

        /// <summary>
        /// Gets or sets the start time filter.
        /// </summary>
        [Parameter(ParameterSetName = ByRunbookId, Mandatory = false, HelpMessage = "Filter jobs so that job start time >= StartTime.")]
        [Parameter(ParameterSetName = ByRunbookName, Mandatory = false, HelpMessage = "Filter jobs so that job start time >= StartTime.")]
        [Parameter(ParameterSetName = ByAll, Mandatory = false, HelpMessage = "Filter jobs so that job start time >= StartTime.")]
        public DateTime StartTime
        {
            get
            {
                return this.startTime;
            }

            set
            {
                this.startTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the end time filter.
        /// </summary>
        [Parameter(ParameterSetName = ByRunbookId, Mandatory = false, HelpMessage = "Filter jobs so that job end time <= EndTime.")]
        [Parameter(ParameterSetName = ByRunbookName, Mandatory = false, HelpMessage = "Filter jobs so that job end time <= EndTime.")]
        [Parameter(ParameterSetName = ByAll, Mandatory = false, HelpMessage = "Filter jobs so that job end time <= EndTime.")]
        public DateTime EndTime
        {
            get
            {
                return this.endTime;
            }

            set
            {
                this.endTime = value;
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
            this.EndTime = this.EndTime.Kind == DateTimeKind.Unspecified
                                 ? DateTime.SpecifyKind(this.EndTime, DateTimeKind.Local)
                                 : this.EndTime;

            IEnumerable<Job> jobs;

            if (this.Id.HasValue)
            {
                // ByJobId
                jobs = new List<Job> { this.AutomationClient.GetJob(this.AutomationAccountName, this.Id.Value) };
            }
            else if (this.RunbookId.HasValue)
            {
                // ByRunbookId
                jobs = this.AutomationClient.ListJobsByRunbookId(this.AutomationAccountName, this.RunbookId.Value, this.StartTime, this.EndTime);
            }
            else if (this.RunbookName != null)
            {
                // ByRunbookName
                jobs = this.AutomationClient.ListJobsByRunbookName(this.AutomationAccountName, this.RunbookName, this.StartTime, this.EndTime);
            }
            else
            {
                // ByAll
                jobs = this.AutomationClient.ListJobs(this.AutomationAccountName, this.StartTime, this.EndTime);
            }

            this.WriteObject(jobs, true);
        }
    }
}
