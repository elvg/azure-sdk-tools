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

using Microsoft.WindowsAzure.Commands.Utilities.Common;

namespace Microsoft.WindowsAzure.Commands.ScenarioTest.ResourceManagerTests
{
    using System.IO;
    using Commands.ScenarioTest.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Utilities.HttpRecorder;

    [TestClass]
    public class ResourceTests : WindowsAzurePowerShellTokenTest
    {
        private string currentDirectory;

        public ResourceTests()
            : base("ResourceManager\\Common.ps1",
                   "ResourceManager\\ResourceTests.ps1")
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
        public void TestCreatesNewSimpleResource()
        {
            RunPowerShellTest("Test-CreatesNewSimpleResource");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.ResourceManager)]
        public void TestCreatesNewComplexResource()
        {
            RunPowerShellTest("Test-CreatesNewComplexResource");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.ResourceManager)]
        public void TestGetResourcesViaPiping()
        {
            RunPowerShellTest("Test-GetResourcesViaPiping");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.ResourceManager)]
        public void TestGetResourcesFromEmptyGroup()
        {
            RunPowerShellTest("Test-GetResourcesFromEmptyGroup");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.ResourceManager)]
        public void TestGetResourcesFromNonExisingGroup()
        {
            RunPowerShellTest("Test-GetResourcesFromNonExisingGroup");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.ResourceManager)]
        public void TestGetResourcesForNonExisingType()
        {
            RunPowerShellTest("Test-GetResourcesForNonExisingType");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.ResourceManager)]
        public void TestGetResourceForNonExisingResource()
        {
            RunPowerShellTest("Test-GetResourceForNonExisingResource");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.ResourceManager)]
        public void TestGetResourcesViaPipingFromAnotherResource()
        {
            RunPowerShellTest("Test-GetResourcesViaPipingFromAnotherResource");
        }
    }
}
