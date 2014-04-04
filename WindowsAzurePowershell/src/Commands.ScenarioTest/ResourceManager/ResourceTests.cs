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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Commands.ResourceManager;
using Microsoft.Azure.Commands.ResourceManager.Models;
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
        public void TestGetResourcesViaPiping_CODE()
        {
            HttpMockServer.Initialize(this.GetType(), "TestGetResourcesViaPiping", HttpRecorderMode.Playback);

            ResourcesClient client = new ResourcesClient(WindowsAzureProfile.Instance.CurrentSubscription);
            var rgname = HttpMockServer.GetAssetName("Test-GetResourcesViaPiping", "onesdk");
	        var rnameParent = HttpMockServer.GetAssetName("Test-GetResourcesViaPiping", "onesdk");
            var rnameChild = HttpMockServer.GetAssetName("Test-GetResourcesViaPiping", "onesdk");
            var resourceTypeParent = "Microsoft.Sql/servers";
            var resourceTypeChild = "Microsoft.Sql/servers/databases";

            var rglocation = client.GetLocations().First(p => p.Name == "ResourceGroup").Locations.First();
            var location = client.GetLocations().First(p => p.Name == resourceTypeParent).Locations.First();
            var apiversion = "2014-04-01";

            // Test
            client.CreatePSResourceGroup(new CreatePSResourceGroupParameters {ResourceGroupName = rgname, Location = rglocation });
            client.CreatePSResource(new CreatePSResourceParameters
                {
                    Name = rnameParent,
                    Location = location,
                    ResourceGroupName = rgname,
                    ResourceType = resourceTypeParent,
                    PropertyObject = new Hashtable(new Dictionary<string, object>
                        {
                            {"administratorLogin", "adminuser"},
                            {"administratorLoginPassword", "P@ssword1"}
                        }),
                    ApiVersion = apiversion
                });
            client.CreatePSResource(new CreatePSResourceParameters
            {
                Name = rnameChild,
                Location = location,
                ResourceGroupName = rgname,
                ResourceType = resourceTypeChild,
                ParentResource = "servers/" + rnameParent,
                PropertyObject = new Hashtable(new Dictionary<string, object>
                        {
                            {"edition", "Web"},
                            {"collation", "SQL_Latin1_General_CP1_CI_AS"},
                            {"maxSizeBytes", "1073741824"},
                        }),
                ApiVersion = apiversion
            });

            var list = client.FilterPSResources(new BasePSResourceParameters
                {
                    ResourceGroupName = client.FilterResourceGroups(rgname).First().ResourceGroupName
                });

            var serverFromList = list.First(r => r.ResourceType == resourceTypeParent);
            var databaseFromList = list.First(r => r.ResourceType == resourceTypeChild);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(rnameParent, serverFromList.Name);
            Assert.AreEqual(rnameChild, databaseFromList.Name);
            Assert.AreEqual(resourceTypeParent, serverFromList.ResourceType);
            Assert.AreEqual(resourceTypeChild, databaseFromList.ResourceType);            
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
