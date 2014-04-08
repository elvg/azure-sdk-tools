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

namespace Microsoft.WindowsAzure.Commands.ScenarioTest.ResourceManagerTests
{
    using System.IO;
    using Commands.ScenarioTest.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Utilities.HttpRecorder;

    [TestClass]
    public class ResourceGroupTests : WindowsAzurePowerShellTokenTest
    {
        private string currentDirectory;

        public ResourceGroupTests()
            : base("ResourceManager\\Common.ps1",
                   "ResourceManager\\ResourceGroupTests.ps1")
        { }

        [TestInitialize]
        public override void TestSetup()
        {
            base.TestSetup();
            currentDirectory = Directory.GetCurrentDirectory();
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            base.TestCleanup();
            Directory.SetCurrentDirectory(currentDirectory);
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.ResourceManager)]
        [TestCategory(Category.CheckIn)]
        public void TestCreatesNewSimpleResourceGroup()
        {
            RunPowerShellTest("Test-CreatesNewSimpleResourceGroup");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.ResourceManager)]
        [TestCategory(Category.CheckIn)]
        public void TestCreatesAndRemoveResourceGroupViaPiping()
        {
            RunPowerShellTest("Test-CreatesAndRemoveResourceGroupViaPiping");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.ResourceManager)]
        [TestCategory(Category.CheckIn)]
        public void TestGetNonExistingResourceGroup()
        {
            RunPowerShellTest("Test-GetNonExistingResourceGroup");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.ResourceManager)]
        [TestCategory(Category.CheckIn)]
        public void TestNewResourceGroupInNonExistingLocation()
        {
            RunPowerShellTest("Test-NewResourceGroupInNonExistingLocation");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.ResourceManager)]
        //[TestCategory(Category.CheckIn)]
        public void TestRemoveNonExistingResourceGroup()
        {
            RunPowerShellTest("Test-RemoveNonExistingResourceGroup");
        }
    }
}
