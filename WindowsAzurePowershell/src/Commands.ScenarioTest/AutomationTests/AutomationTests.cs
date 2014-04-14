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

namespace Microsoft.WindowsAzure.Commands.ScenarioTest.AutomationTests
{
    using Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AutomationTests : WindowsAzurePowerShellCertificateTest
    {
        public AutomationTests() : base("Automation\\AutomationTests.ps1") { }
        
        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Automation)]
        public void TestAutomationStartAndStopRunbook()
        {
            RunPowerShellTest("Test-AutomationStartAndStopRunbook -runbookPath Automation\\Test-Workflow.ps1");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Automation)]
        public void TestAutomationPublishAndEditRunbook()
        {
            RunPowerShellTest("Test-AutomationPublishAndEditRunbook -runbookPath Automation\\Test-Workflow.ps1 -editRunbookPath Automation\\Test-WorkflowV2.ps1");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Automation)]
        public void TestAutomationConfigureRunbook()
        {
            RunPowerShellTest("Test-AutomationConfigureRunbook -runbookPath Automation\\Write-DebugAndVerboseOutput.ps1");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Automation)]
        public void TestAutomationSuspendAndResumeJob()
        {
            RunPowerShellTest("Test-AutomationSuspendAndResumeJob -runbookPath Automation\\Use-WorkflowCheckpointSample.ps1");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Automation)]
        public void TestAutomationStartRunbookOnASchedule()
        {
            RunPowerShellTest("Test-AutomationStartRunbookOnASchedule -runbookPath Automation\\Test-Workflow.ps1");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Automation)]
        public void TestAutomationStartUnpublishedRunbook()
        {
            RunPowerShellTest("Test-AutomationStartUnpublishedRunbook -runbookPath Automation\\Test-WorkFlowWithVariousParameters.ps1");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Automation)]
        public void TestAutomationRunbookWithParameter()
        {
            RunPowerShellTest("Test-RunbookWithParameter -runbookPath Automation\\fastJob.ps1  @{'nums'='[1,2,3,4,5,6,7]'}  28");
        }
    }
}
