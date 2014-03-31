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


namespace Microsoft.WindowsAzure.Commands.ScenarioTest.Common
{
    using System.Collections.Generic;
    using VisualStudio.TestTools.UnitTesting;
    using Commands.Common;
    using WindowsAzure.Utilities.HttpRecorder;
    using Utilities.Common;
    using Commands.Common.Test.Common;
    using Test.Utilities.Common;

    [TestClass]
    public class WindowsAzurePowerShellTokenTest : PowerShellTest
    {
        protected List<HttpMockServer> mockServers;
        private static string testEnvironmentName = "__test-environment";
        protected HttpRecorderMode MockServerRecordingMode = HttpRecorderMode.None;

        private void OnClientCreated(object sender, ClientCreatedArgs e)
        {
            HttpMockServer mockServer = new HttpMockServer(new SimpleRecordMatcher(), this.GetType());

            HttpMockServer.Mode = MockServerRecordingMode;
            HttpMockServer.OutputDirectory = @"D:\Code\GitHub\azure-sdk-tools-pr\WindowsAzurePowershell\src\Commands.ScenarioTest\Resources\SessionRecords";

            e.AddHandlerToClient(mockServer);
            mockServers.Add(mockServer);
        }

        public WindowsAzurePowerShellTokenTest(params string[] modules)
            : base(modules)
        { }

        [TestInitialize]
        public override void TestSetup()
        {
            base.TestSetup();
            this.mockServers = new List<HttpMockServer>();
            WindowsAzureSubscription.OnClientCreated += OnClientCreated;
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) =>
            {
                return true;
            };

            WindowsAzureProfile.Instance = new WindowsAzureProfile(new MockProfileStore());

            if (!WindowsAzureProfile.Instance.Environments.ContainsKey(testEnvironmentName))
            {
                WindowsAzureProfile.Instance.AddEnvironment(new WindowsAzureEnvironment { Name = testEnvironmentName });
            }

            SetupAzureEnvironmentFromEnvironmentVariables();
        }

        private void SetupAzureEnvironmentFromEnvironmentVariables()
        {
            ServiceManagementTestEnvironmentFactory serviceManagementTestEnvironmentFactory = new ServiceManagementTestEnvironmentFactory();
            TestEnvironment rdfeEnvironment = serviceManagementTestEnvironmentFactory.GetTestEnvironment();
            ResourceManagerTestEnvironmentFactory resourceManagerTestEnvironmentFactory = new ResourceManagerTestEnvironmentFactory();
            TestEnvironment csmEnvironment = resourceManagerTestEnvironmentFactory.GetTestEnvironment();
            string jwtToken = ((TokenCloudCredentials)csmEnvironment.Credentials).Token;

            WindowsAzureProfile.Instance.TokenProvider = new FakeAccessTokenProvider(jwtToken, csmEnvironment.UserName);

            WindowsAzureProfile.Instance.CurrentEnvironment = WindowsAzureProfile.Instance.Environments[testEnvironmentName];

            WindowsAzureProfile.Instance.CurrentEnvironment.ActiveDirectoryEndpoint =
                csmEnvironment.ActiveDirectoryEndpoint.AbsoluteUri;
            WindowsAzureProfile.Instance.CurrentEnvironment.GalleryEndpoint =
                csmEnvironment.GalleryUri.AbsoluteUri;
            WindowsAzureProfile.Instance.CurrentEnvironment.ResourceManagerEndpoint =
                csmEnvironment.BaseUri.AbsoluteUri;
            WindowsAzureProfile.Instance.CurrentEnvironment.ServiceEndpoint =
                rdfeEnvironment.BaseUri.AbsoluteUri;

            RunPowerShellTest("Add-AzureAccount -Environment " + testEnvironmentName);

            foreach (var subscription in WindowsAzureProfile.Instance.Subscriptions)
            {
                subscription.TokenProvider = WindowsAzureProfile.Instance.TokenProvider;
                if (subscription.SubscriptionId == csmEnvironment.SubscriptionId)
                {
                    subscription.IsDefault = true;
                    subscription.CurrentStorageAccountName = csmEnvironment.StorageAccount;
                }
                else
                {
                    subscription.IsDefault = false;
                }
            }
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            base.TestCleanup();
            WindowsAzureSubscription.OnClientCreated -= OnClientCreated;
            mockServers.ForEach(ms => ms.Dispose());

            WindowsAzureProfile.Instance.RemoveEnvironment(testEnvironmentName);
        }
    }
}