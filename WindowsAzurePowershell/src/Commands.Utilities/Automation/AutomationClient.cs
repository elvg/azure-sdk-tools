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

namespace Microsoft.WindowsAzure.Commands.Utilities.Automation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web;

    using Microsoft.Azure.Management.Automation;
    using Microsoft.WindowsAzure.Commands.Utilities.Automation.Models;
    using Microsoft.WindowsAzure.Commands.Utilities.Common;

    using AutomationManagement = Microsoft.Azure.Management.Automation;

    public class AutomationClient : IAutomationClient
    {
        private const string AutomationCloudServicePrefix = "OaaSCS";

        private const string AutomationResourceProvider = "automation";

        private readonly IAutomationManagementClient automationManagementClient;

        // Injection point for unit tests
        public AutomationClient()
        {
        }

        public AutomationClient(WindowsAzureSubscription subscription)
            : this(
                subscription,
                subscription.CreateClient<AutomationManagementClient>())
        {
        }

        public AutomationClient(
            WindowsAzureSubscription subscription,
            IAutomationManagementClient automationManagementClient)
        {
            Requires.Argument("automationManagementClient", automationManagementClient).NotNull();

            this.Subscription = subscription;
            this.automationManagementClient = automationManagementClient;
        }

        public WindowsAzureSubscription Subscription { get; private set; }

        #region Account Operations

        public IEnumerable<AutomationAccount> ListAutomationAccounts(string automationAccountName, string location)
        {
            var automationAccounts = new List<AutomationAccount>();
            var cloudServices = new List<AutomationManagement.Models.CloudService>(this.automationManagementClient.CloudServices.List(AutomationResourceProvider).CloudServices);

            foreach (var cloudService in cloudServices)
            {
                automationAccounts.AddRange(cloudService.Resources.Select(resource => new AutomationAccount(cloudService, resource)));
            }

            // RDFE does not support server-side filtering, hence we filter in the client-side.
            if (automationAccountName != null)
            {
                automationAccounts = automationAccounts.Where(account => account.AutomationAccountName == automationAccountName).ToList();
            }

            if (location != null)
            {
                automationAccounts = automationAccounts.Where(account => account.Location == location).ToList();
            }

            return automationAccounts;
        }

        #endregion

        #region Runbook Operations

        public Runbook CreateRunbookByName(string automationAccountName, string runbookName, string description, string[] tags)
        {
            var runbookScript = string.Format(CultureInfo.InvariantCulture, @"workflow {0}{1}{{{1}}}", runbookName, Environment.NewLine);
            using (var streamReader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(runbookScript), false), Encoding.UTF8))
            {
                var runbookStream = streamReader.BaseStream;
                var runbook = this.CreateRunbook(automationAccountName, runbookStream);
                this.UpdateRunbook(automationAccountName, runbook.Id, description, tags, null, null, null);
                return this.GetRunbook(automationAccountName, runbook.Id);
            }
        }

        public Runbook CreateRunbookByPath(string automationAccountName, string runbookPath, string description, string[] tags)
        {
            var runbook = this.CreateRunbook(automationAccountName, File.OpenRead(runbookPath));
            this.UpdateRunbook(automationAccountName, runbook.Id, description, tags, null, null, null);
            return this.GetRunbook(automationAccountName, runbook.Id);
        }

        public void DeleteRunbook(string automationAccountName, Guid runbookId)
        {
            this.automationManagementClient.Runbooks.Delete(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                runbookId.ToString());
        }

        public void DeleteRunbook(string automationAccountName, string runbookName)
        {
            var runbook = this.GetRunbook(automationAccountName, runbookName);
            this.DeleteRunbook(automationAccountName, runbook.Id);
        }

        /// <summary>
        /// Gets the runbook identified by runbookId, with schedule names expanded.
        /// </summary>
        /// <param name="automationAccountName">
        /// The automation account name.
        /// </param>
        /// <param name="runbookId">
        /// The runbook id.
        /// </param>
        /// <returns>
        /// The <see cref="Runbook"/>.
        /// </returns>
        public Runbook GetRunbook(string automationAccountName, Guid runbookId)
        {
            return new Runbook(this.GetRunbookModel(automationAccountName, runbookId, true));
        }

        /// <summary>
        /// Gets the runbook identified by runbookId, with schedule names expanded.
        /// </summary>
        /// <param name="automationAccountName">
        /// The automation account name.
        /// </param>
        /// <param name="runbookName">
        /// The runbook name.
        /// </param>
        /// <returns>
        /// The <see cref="Runbook"/>.
        /// </returns>
        public Runbook GetRunbook(string automationAccountName, string runbookName)
        {
            return new Runbook(this.GetRunbookModel(automationAccountName, runbookName, true));
        }

        public IEnumerable<Runbook> ListRunbooks(string automationAccountName)
        {
            var runbooks = this.ContinuationTokenHandler(
                skipToken =>
                {
                    var listRunbookResponse =
                        this.automationManagementClient.Runbooks.ListWithSchedules(
                        automationAccountName, AutomationCloudServicePrefix, AutomationResourceProvider, skipToken);
                    return new ResponseWithNextLink<AutomationManagement.Models.Runbook>(
                        listRunbookResponse, listRunbookResponse.Runbooks);
                });

            return runbooks.Select(runbook => new Runbook(runbook));
        }

        public IEnumerable<Runbook> ListRunbookByScheduleName(string automationAccountName, string scheduleName)
        {
            var runbooks = this.ContinuationTokenHandler(
                skipToken =>
                {
                    var listRunbookResponse =
                        this.automationManagementClient.Runbooks.ListByScheduleNameWithSchedules(
                            automationAccountName,
                            AutomationCloudServicePrefix,
                            AutomationResourceProvider,
                            new AutomationManagement.Models.RunbookListByScheduleNameParameters
                            {
                                ScheduleName = scheduleName,
                                SkipToken = skipToken
                            });
                    return new ResponseWithNextLink<AutomationManagement.Models.Runbook>(
                        listRunbookResponse, listRunbookResponse.Runbooks);
                });

            return runbooks.Select(runbook => new Runbook(runbook));
        }

        public Runbook PublishRunbook(string automationAccountName, Guid runbookId)
        {
            this.automationManagementClient.Runbooks.Publish(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                runbookId.ToString());

            return this.GetRunbook(automationAccountName, runbookId);
        }

        public Runbook PublishRunbook(string automationAccountName, string runbookName)
        {
            var runbookId = this.GetRunbookIdByRunbookName(automationAccountName, runbookName);
            return this.PublishRunbook(automationAccountName, runbookId);
        }

        public Job StartRunbook(string automationAccountName, Guid runbookId, IDictionary parameters)
        {
            var nameValuePairs = this.ProcessRunbookParameters(automationAccountName, runbookId, parameters);
            var startResponse = this.automationManagementClient.Runbooks.Start(
                                    automationAccountName,
                                    AutomationCloudServicePrefix,
                                    AutomationResourceProvider,
                                    new AutomationManagement.Models.RunbookStartParameters
                                    {
                                        RunbookId = runbookId.ToString(),
                                        Parameters = nameValuePairs.ToList()
                                    });

            return this.GetJob(automationAccountName, new Guid(startResponse.JobId));
        }

        public Job StartRunbook(string automationAccountName, string runbookName, IDictionary parameters)
        {
            var runbookId = this.GetRunbookIdByRunbookName(automationAccountName, runbookName);
            return this.StartRunbook(automationAccountName, runbookId, parameters);
        }

        public Runbook RegisterScheduledRunbook(
            string automationAccountName, Guid runbookId, IDictionary parameters, string scheduleName)
        {
            var schedule = this.GetSchedule(automationAccountName, scheduleName);
            var nameValuePairs = this.ProcessRunbookParameters(automationAccountName, runbookId, parameters);
            this.automationManagementClient.Runbooks.StartOnSchedule(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                new AutomationManagement.Models.RunbookStartOnScheduleParameters
                    {
                        RunbookId = runbookId.ToString(),
                        Parameters = nameValuePairs.ToList(),
                        ScheduleId = schedule.Id.ToString()
                    });

            return this.GetRunbook(automationAccountName, runbookId);
        }

        public Runbook RegisterScheduledRunbook(
            string automationAccountName, string runbookName, IDictionary parameters, string scheduleName)
        {
            var runbookId = this.GetRunbookIdByRunbookName(automationAccountName, runbookName);
            return this.RegisterScheduledRunbook(automationAccountName, runbookId, parameters, scheduleName);
        }

        public Runbook UpdateRunbook(string automationAccountName, Guid runbookId, string description, string[] tags, bool? logDebug, bool? logProgress, bool? logVerbose)
        {
            var runbookModel = this.GetRunbookModel(automationAccountName, runbookId, false);
            return this.UpdateRunbookHelper(automationAccountName, runbookModel, description, tags, logDebug, logProgress, logVerbose);
        }

        public Runbook UpdateRunbook(string automationAccountName, string runbookName, string description, string[] tags, bool? logDebug, bool? logProgress, bool? logVerbose)
        {
            var runbookModel = this.GetRunbookModel(automationAccountName, runbookName, false);
            return this.UpdateRunbookHelper(automationAccountName, runbookModel, description, tags, logDebug, logProgress, logVerbose);
        }

        public Runbook UnregisterScheduledRunbook(string automationAccountName, Guid runbookId, string scheduleName)
        {
            var schedule = this.GetSchedule(automationAccountName, scheduleName);
            this.automationManagementClient.Runbooks.DeleteScheduleLink(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                new AutomationManagement.Models.RunbookDeleteScheduleLinkParameters
                    {
                        RunbookId = runbookId.ToString(),
                        ScheduleId =
                            schedule.Id.ToString()
                    });
            return this.GetRunbook(automationAccountName, runbookId);
        }

        public Runbook UnregisterScheduledRunbook(string automationAccountName, string runbookName, string scheduleName)
        {
            var runbookId = this.GetRunbookIdByRunbookName(automationAccountName, runbookName);
            return this.UnregisterScheduledRunbook(automationAccountName, runbookId, scheduleName);
        }

        #endregion

        #region Runbook Definition Operations

        public IEnumerable<RunbookDefinition> ListRunbookDefinitionsByRunbookName(string automationAccountName, string runbookName, bool? isDraft)
        {
            var runbookId = this.GetRunbookIdByRunbookName(automationAccountName, runbookName);
            return this.ListRunbookDefinitionsByRunbookId(automationAccountName, runbookId, isDraft);
        }

        public IEnumerable<RunbookDefinition> ListRunbookDefinitionsByRunbookId(string automationAccountName, Guid runbookId, bool? isDraft)
        {
            var runbookVersions = isDraft.HasValue
                                      ? this.automationManagementClient.RunbookVersions.ListLatestByRunbookIdSlot(
                                          automationAccountName,
                                          AutomationCloudServicePrefix,
                                          AutomationResourceProvider,
                                          new AutomationManagement.Models.
                                            RunbookVersionListLatestByRunbookIdSlotParameters
                                              {
                                                  RunbookId =
                                                      runbookId.ToString(),
                                                  IsDraft =
                                                      isDraft.Value
                                              })
                                            .RunbookVersions
                                      : this.automationManagementClient.RunbookVersions.ListLatestByRunbookId(
                                          automationAccountName,
                                          AutomationCloudServicePrefix,
                                          AutomationResourceProvider,
                                          runbookId.ToString()).RunbookVersions;

            return this.CreateRunbookDefinitionsFromRunbookVersionModels(automationAccountName, runbookVersions);
        }

        public IEnumerable<RunbookDefinition> ListRunbookDefinitionsByRunbookVersionId(string automationAccountName, Guid runbookVersionId, bool? isDraft)
        {
            var runbookVersion = this.GetRunbookVersionModel(automationAccountName, runbookVersionId);
            if (!isDraft.HasValue || isDraft.Value == runbookVersion.IsDraft)
            {
                return this.CreateRunbookDefinitionsFromRunbookVersionModels(
                    automationAccountName, new List<AutomationManagement.Models.RunbookVersion> { runbookVersion });
            }
            else
            {
                return new List<RunbookDefinition>();
            }
        }

        public RunbookDefinition UpdateRunbookDefinition(string automationAccountName, Guid runbookId, string runbookPath, bool overwrite)
        {
            return this.UpdateRunbookDefinition(automationAccountName, runbookId, File.OpenRead(runbookPath), overwrite);
        }

        public RunbookDefinition UpdateRunbookDefinition(string automationAccountName, string runbookName, string runbookPath, bool overwrite)
        {
            var runbookId = this.GetRunbookIdByRunbookName(automationAccountName, runbookName);
            return this.UpdateRunbookDefinition(automationAccountName, runbookId, runbookPath, overwrite);
        }

        #endregion

        #region Job Operations

        public Job GetJob(string automationAccountName, Guid jobId)
        {
            return new Job(this.GetJobModel(automationAccountName, jobId));
        }

        public IEnumerable<Job> ListJobs(string automationAccountName, DateTime startTime, DateTime endTime)
        {
            var jobs = this.ContinuationTokenHandler(
                skipToken =>
                    {
                        var response = this.automationManagementClient.Jobs.List(
                            automationAccountName,
                            AutomationCloudServicePrefix,
                            AutomationResourceProvider,
                            new AutomationManagement.Models.JobListParameters
                                {
                                    SkipToken = skipToken,
                                    StartTime =
                                        this.FormatDateTime(startTime),
                                    EndTime = this.FormatDateTime(endTime)
                                });
                        return new ResponseWithNextLink<AutomationManagement.Models.Job>(
                            response, response.Jobs);
                    });

            return jobs.Select(job => new Job(job));
        }

        public IEnumerable<Job> ListJobsByRunbookId(string automationAccountName, Guid runbookId, DateTime startTime, DateTime endTime)
        {
            var jobs = this.ContinuationTokenHandler(
                skipToken =>
                {
                    var response = this.automationManagementClient.Jobs.ListByRunbookId(
                        automationAccountName,
                        AutomationCloudServicePrefix,
                        AutomationResourceProvider,
                        new AutomationManagement.Models.JobListByRunbookIdParameters
                        {
                            RunbookId = runbookId.ToString(),
                            SkipToken = skipToken,
                            StartTime = this.FormatDateTime(startTime),
                            EndTime = this.FormatDateTime(endTime)
                        });
                    return new ResponseWithNextLink<AutomationManagement.Models.Job>(
                        response, response.Jobs);
                });

            return jobs.Select(job => new Job(job));
        }

        public IEnumerable<Job> ListJobsByRunbookName(string automationAccountName, string runbookName, DateTime startTime, DateTime endTime)
        {
            var runbookId = this.GetRunbookIdByRunbookName(automationAccountName, runbookName);
            return this.ListJobsByRunbookId(automationAccountName, runbookId, startTime, endTime);
        }

        public void ResumeJob(string automationAccountName, Guid jobId)
        {
            this.automationManagementClient.Jobs.Resume(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                jobId.ToString());
        }

        public void StopJob(string automationAccountName, Guid jobId)
        {
            this.automationManagementClient.Jobs.Stop(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                jobId.ToString());
        }

        public void SuspendJob(string automationAccountName, Guid jobId)
        {
            this.automationManagementClient.Jobs.Suspend(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                jobId.ToString());
        }

        #endregion

        #region Job Stream Item Operations

        public IEnumerable<JobStreamItem> ListJobStreamItems(string automationAccountName, Guid jobId, DateTime createdSince, string streamTypeName)
        {
            var jobStreamItems = this.ContinuationTokenHandler(
                skipToken =>
                {
                    var response = this.automationManagementClient.JobStreams.ListStreamItems(
                        automationAccountName,
                        AutomationCloudServicePrefix,
                        AutomationResourceProvider,
                        new AutomationManagement.Models.JobStreamListStreamItemsParameters
                        {
                            JobId = jobId.ToString(),
                            StartTime = createdSince.ToUniversalTime(),
                            StreamType = streamTypeName,
                            SkipToken = skipToken
                        });
                    return new ResponseWithNextLink<AutomationManagement.Models.JobStreamItem>(
                        response, response.JobStreamItems);
                });

            return jobStreamItems.Select(jobStreamItem => new JobStreamItem(jobStreamItem));
        }

        #endregion

        #region Schedule Operations

        public Schedule CreateSchedule(string automationAccountName, OneTimeSchedule schedule)
        {
            this.ValidateScheduleName(automationAccountName, schedule.Name);

            var modelschedule = new AutomationManagement.Models.Schedule
            {
                Name = schedule.Name,
                StartTime = schedule.StartTime.ToUniversalTime(),
                ExpiryTime = schedule.ExpiryTime.ToUniversalTime(),
                Description = schedule.Description,
                ScheduleType =
                    AutomationManagement.Models.ScheduleType
                                        .OneTimeSchedule
            };

            var scheduleCreateParameters = new AutomationManagement.Models.ScheduleCreateParameters()
            {
                Schedule = modelschedule
            };

            var scheduleCreateResponse = this.automationManagementClient.Schedules.Create(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                scheduleCreateParameters);

            return this.GetSchedule(automationAccountName, new Guid(scheduleCreateResponse.Schedule.Id));
        }

        public Schedule CreateSchedule(string automationAccountName, DailySchedule schedule)
        {
            this.ValidateScheduleName(automationAccountName, schedule.Name);

            var modelschedule = new AutomationManagement.Models.Schedule
            {
                Name = schedule.Name,
                StartTime = schedule.StartTime.ToUniversalTime(),
                ExpiryTime = schedule.ExpiryTime.ToUniversalTime(),
                Description = schedule.Description,
                DayInterval = schedule.DayInterval,
                ScheduleType =
                    AutomationManagement.Models.ScheduleType
                                        .DailySchedule
            };

            var scheduleCreateParameters = new AutomationManagement.Models.ScheduleCreateParameters()
            {
                Schedule = modelschedule
            };

            var scheduleCreateResponse = this.automationManagementClient.Schedules.Create(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                scheduleCreateParameters);

            return this.GetSchedule(automationAccountName, new Guid(scheduleCreateResponse.Schedule.Id));
        }

        public void DeleteSchedule(string automationAccountName, Guid scheduleId)
        {
            this.automationManagementClient.Schedules.Delete(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                scheduleId.ToString());
        }

        public void DeleteSchedule(string automationAccountName, string scheduleName)
        {
            var schedule = this.GetSchedule(automationAccountName, scheduleName);
            this.DeleteSchedule(automationAccountName, schedule.Id);
        }

        public Schedule GetSchedule(string automationAccountName, Guid scheduleId)
        {
            var scheduleModel = this.GetScheduleModel(automationAccountName, scheduleId);
            return this.CreateScheduleFromScheduleModel(scheduleModel);
        }

        public Schedule GetSchedule(string automationAccountName, string scheduleName)
        {
            var scheduleModel = this.GetScheduleModel(automationAccountName, scheduleName);
            return this.CreateScheduleFromScheduleModel(scheduleModel);
        }

        public IEnumerable<Schedule> ListSchedules(string automationAccountName)
        {
            var schedules = this.ContinuationTokenHandler(
                skipToken =>
                    {
                        var response = this.automationManagementClient.Schedules.List(
                            automationAccountName, AutomationCloudServicePrefix, AutomationResourceProvider, skipToken);
                        return new ResponseWithNextLink<AutomationManagement.Models.Schedule>(
                            response, response.Schedules);
                    });

            return schedules.Select(this.CreateScheduleFromScheduleModel);
        }

        public Schedule UpdateSchedule(string automationAccountName, Guid scheduleId, string description)
        {
            var scheduleModel = this.GetScheduleModel(automationAccountName, scheduleId);
            return this.UpdateScheduleHelper(automationAccountName, scheduleModel, description);
        }

        public Schedule UpdateSchedule(string automationAccountName, string scheduleName, string description)
        {
            var scheduleModel = this.GetScheduleModel(automationAccountName, scheduleName);
            return this.UpdateScheduleHelper(automationAccountName, scheduleModel, description);
        }

        #endregion

        #region Private Methods

        private Runbook CreateRunbook(string automationAccountName, Stream runbookStream)
        {
            var createRunbookVersionResponse = this.automationManagementClient.RunbookVersions.Create(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                runbookStream);

            var getRunbookVersionResponse = this.automationManagementClient.RunbookVersions.Get(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                createRunbookVersionResponse.RunbookVersion.Id);

            return this.GetRunbook(automationAccountName, new Guid(getRunbookVersionResponse.RunbookVersion.RunbookId));
        }

        private IEnumerable<RunbookDefinition> CreateRunbookDefinitionsFromRunbookVersionModels(
            string automationAccountName, IEnumerable<Azure.Management.Automation.Models.RunbookVersion> runbookVersions)
        {
            foreach (AutomationManagement.Models.RunbookVersion runbookVersion in runbookVersions)
            {
                var getRunbookDefinitionResponse =
                    this.automationManagementClient.RunbookVersions.GetRunbookDefinition(
                        automationAccountName,
                        AutomationCloudServicePrefix,
                        AutomationResourceProvider,
                        runbookVersion.Id);

                yield return new RunbookDefinition(runbookVersion, getRunbookDefinitionResponse.RunbookDefinition);
            }
        }

        private Schedule CreateScheduleFromScheduleModel(AutomationManagement.Models.Schedule schedule)
        {
            Requires.Argument("schedule", schedule).NotNull();

            if (schedule.ScheduleType == AutomationManagement.Models.ScheduleType.DailySchedule)
            {
                return new DailySchedule(schedule);
            }
            else
            {
                return new OneTimeSchedule(schedule);
            }
        }

        private List<T> ContinuationTokenHandler<T>(Func<string, ResponseWithNextLink<T>> listFunc)
        {
            var models = new List<T>();
            string skipToken = null;
            string nextLink;
            do
            {
                var result = listFunc.Invoke(skipToken);
                models.AddRange(result.AutomationManagementModels);
                nextLink = result.NextLink;
                if (!string.IsNullOrEmpty(nextLink))
                {
                    skipToken = this.GetSkipToken(nextLink);
                }
            }
            while (!string.IsNullOrEmpty(nextLink));
            return models;
        }

        private Guid EditRunbook(string automationAccountName, Guid runbookId)
        {
            return new Guid(this.automationManagementClient.Runbooks.Edit(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                runbookId.ToString())
                .DraftRunbookVersionId);
        }

        private string FormatDateTime(DateTime dateTime)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:O}", dateTime.ToUniversalTime());
        }

        private AutomationManagement.Models.Job GetJobModel(string automationAccountName, Guid jobId)
        {
            var job = this.automationManagementClient.Jobs.Get(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                jobId.ToString())
                .Job;

            if (job == null)
            {
                throw new ResourceNotFoundException(typeof(Job), string.Format(CultureInfo.CurrentCulture, Resources.JobNotFoundById, jobId));
            }

            return job;
        }

        private Guid GetRunbookIdByRunbookName(string automationAccountName, string runbookName)
        {
            return new Guid(this.GetRunbookModel(automationAccountName, runbookName, false).Id);
        }

        private AutomationManagement.Models.Runbook GetRunbookModel(string automationAccountName, Guid runbookId, bool withSchedules)
        {
            var runbook = withSchedules
                              ? this.automationManagementClient.Runbooks.GetWithSchedules(
                                  automationAccountName,
                                  AutomationCloudServicePrefix,
                                  AutomationResourceProvider,
                                  runbookId.ToString()).Runbook
                              : this.automationManagementClient.Runbooks.Get(
                                  automationAccountName,
                                  AutomationCloudServicePrefix,
                                  AutomationResourceProvider,
                                  runbookId.ToString()).Runbook;

            if (runbook == null)
            {
                throw new ResourceNotFoundException(typeof(Runbook), string.Format(CultureInfo.CurrentCulture, Resources.RunbookNotFoundById, runbookId));
            }

            return runbook;
        }

        private AutomationManagement.Models.Runbook GetRunbookModel(string automationAccountName, string runbookName, bool withSchedules)
        {
            var runbooks = withSchedules
                               ? this.automationManagementClient.Runbooks.ListByNameWithSchedules(
                                   automationAccountName,
                                   AutomationCloudServicePrefix,
                                   AutomationResourceProvider,
                                   runbookName).Runbooks
                               : this.automationManagementClient.Runbooks.ListByName(
                                   automationAccountName,
                                   AutomationCloudServicePrefix,
                                   AutomationResourceProvider,
                                   runbookName).Runbooks;

            if (!runbooks.Any())
            {
                throw new ResourceNotFoundException(typeof(Runbook), string.Format(CultureInfo.CurrentCulture, Resources.RunbookNotFoundByName, runbookName));
            }

            return runbooks.First();
        }

        private AutomationManagement.Models.RunbookVersion GetRunbookVersionModel(
            string automationAccountName, Guid runbookVersionId)
        {
            var runbookVersion =
                this.automationManagementClient.RunbookVersions.Get(
                    automationAccountName,
                    AutomationCloudServicePrefix,
                    AutomationResourceProvider,
                    runbookVersionId.ToString()).RunbookVersion;

            if (runbookVersion == null)
            {
                throw new ResourceNotFoundException(
                    typeof(RunbookVersion),
                    string.Format(CultureInfo.CurrentCulture, Resources.RunbookVersionNotFoundById, runbookVersionId));
            }

            return runbookVersion;
        }

        private AutomationManagement.Models.Schedule GetScheduleModel(string automationAccountName, Guid scheduleId)
        {
            var schedule = this.automationManagementClient.Schedules.Get(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                scheduleId.ToString())
                .Schedule;

            if (schedule == null)
            {
                throw new ResourceNotFoundException(typeof(Schedule), string.Format(CultureInfo.CurrentCulture, Resources.ScheduleNotFoundById, scheduleId));
            }

            return schedule;
        }

        private AutomationManagement.Models.Schedule GetScheduleModel(string automationAccountName, string scheduleName)
        {
            var schedules = this.automationManagementClient.Schedules.ListByName(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                scheduleName)
                .Schedules;

            if (!schedules.Any())
            {
                throw new ResourceNotFoundException(typeof(RunbookVersion), string.Format(CultureInfo.CurrentCulture, Resources.ScheduleNotFoundByName, scheduleName));
            }

            return schedules.First();
        }

        private string GetSkipToken(string nextLink)
        {
            string skipToken = null;
            var query = nextLink.Split('?');
            if (query.Length > 1)
            {
                skipToken = HttpUtility.ParseQueryString(query[1]).Get(Constants.SkipTokenParameterName);
            }

            if (skipToken == null)
            {
                throw new InvalidContinuationTokenException(
                    string.Format(CultureInfo.CurrentCulture, Resources.InvalidContinuationToken, nextLink));
            }

            return skipToken;
        }

        private IEnumerable<RunbookParameter> ListRunbookParameters(string automationAccountName, Guid runbookId)
        {
            var runbook = this.GetRunbook(automationAccountName, runbookId);
            if (!runbook.PublishedRunbookVersionId.HasValue)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.RunbookHasNoPublishedVersionById, runbookId));
            }

            return this.automationManagementClient.RunbookParameters.ListByRunbookVersionId(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                runbook.PublishedRunbookVersionId.Value.ToString()).RunbookParameters.Select(runbookParameter => new RunbookParameter(runbookParameter));
        }

        private IEnumerable<AutomationManagement.Models.NameValuePair> ProcessRunbookParameters(string automationAccountName, Guid runbookId, IDictionary parameters)
        {
            parameters = parameters ?? new Dictionary<string, string>();
            var runbookParameters = this.ListRunbookParameters(automationAccountName, runbookId);
            var filteredParameters = new List<AutomationManagement.Models.NameValuePair>();

            foreach (var runbookParameter in runbookParameters)
            {
                if (parameters.Contains(runbookParameter.Name))
                {
                    var paramValue = parameters[runbookParameter.Name];
                    filteredParameters.Add(
                        new AutomationManagement.Models.NameValuePair
                        {
                            Name = runbookParameter.Name,
                            Value = paramValue.ToString()
                        });
                }
                else if (runbookParameter.IsMandatory)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture, Resources.RunbookParameterValueRequired, runbookParameter.Name));
                }
            }

            if (filteredParameters.Count != parameters.Count)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, Resources.InvalidRunbookParameters));
            }

            return filteredParameters;
        }

        private RunbookDefinition UpdateRunbookDefinition(string automationAccountName, Guid runbookId, Stream runbookStream, bool overwrite)
        {
            var runbook = new Runbook(this.GetRunbookModel(automationAccountName, runbookId, false));

            if (runbook.DraftRunbookVersionId.HasValue && overwrite == false)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.RunbookAlreadyHasDraft));
            }

            var draftRunbookVersionId = runbook.DraftRunbookVersionId.HasValue
                                ? runbook.DraftRunbookVersionId.Value
                                : this.EditRunbook(automationAccountName, runbook.Id);

            var getRunbookDefinitionResponse = this.automationManagementClient.RunbookVersions.GetRunbookDefinition(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                draftRunbookVersionId.ToString());

            this.automationManagementClient.RunbookVersions.UpdateRunbookDefinition(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                new AutomationManagement.Models.RunbookVersionUpdateRunbookDefinitionParameters
                {
                    ETag = getRunbookDefinitionResponse.ETag,
                    RunbookVersionId = draftRunbookVersionId.ToString(),
                    RunbookStream = runbookStream,
                });

            var runbookVersions = this.ListRunbookDefinitionsByRunbookVersionId(automationAccountName, draftRunbookVersionId, true);
            return runbookVersions.First();
        }

        private Runbook UpdateRunbookHelper(
            string automationAccountName,
            AutomationManagement.Models.Runbook runbook,
            string description,
            string[] tags,
            bool? logDebug,
            bool? logProgress,
            bool? logVerbose)
        {
            if (description != null)
            {
                runbook.Description = description;
            }

            if (tags != null)
            {
                runbook.Tags = string.Join(Constants.RunbookTagsSeparatorString, tags);
            }

            if (logDebug.HasValue)
            {
                runbook.LogDebug = logDebug.Value;
            }

            if (logProgress.HasValue)
            {
                runbook.LogProgress = logProgress.Value;
            }

            if (logVerbose.HasValue)
            {
                runbook.LogVerbose = logVerbose.Value;
            }

            var runbookUpdateParameters = new AutomationManagement.Models.RunbookUpdateParameters
            {
                Runbook = runbook
            };

            this.automationManagementClient.Runbooks.Update(
                automationAccountName, AutomationCloudServicePrefix, AutomationResourceProvider, runbookUpdateParameters);

            var runbookId = new Guid(runbook.Id);
            return this.GetRunbook(automationAccountName, runbookId);
        }

        private Schedule UpdateScheduleHelper(
            string automationAccountName, AutomationManagement.Models.Schedule schedule, string description)
        {
            // StartTime and ExpiryTime need to specified as Utc
            schedule.StartTime = DateTime.SpecifyKind(schedule.StartTime, DateTimeKind.Utc);
            schedule.ExpiryTime = DateTime.SpecifyKind(schedule.ExpiryTime, DateTimeKind.Utc);

            schedule.Description = description;

            var scheduleUpdateParameters = new AutomationManagement.Models.ScheduleUpdateParameters()
                                               {
                                                   Schedule =
                                                       schedule
                                               };

            this.automationManagementClient.Schedules.Update(
                automationAccountName,
                AutomationCloudServicePrefix,
                AutomationResourceProvider,
                scheduleUpdateParameters);

            var scheduleId = new Guid(schedule.Id);
            return this.GetSchedule(automationAccountName, scheduleId);
        }

        // TODO: remove the helper which provides client-side schedule name validation once CDM TFS bug 662986 is resolved.
        private void ValidateScheduleName(string automationAccountName, string scheduleName)
        {
            var schedules =
                this.automationManagementClient.Schedules.ListByName(
                    automationAccountName, AutomationCloudServicePrefix, AutomationResourceProvider, scheduleName)
                    .Schedules;

            if (schedules.Any())
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.ScheduleWithNameExists, scheduleName));
            }
        }

        #endregion
    }
}
