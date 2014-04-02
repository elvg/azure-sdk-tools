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

namespace Microsoft.WindowsAzure.Commands.Utilities.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Authentication;
    using Commands.Common.Properties;

    /// <summary>
    /// This class is the entry point for all the persistent
    /// state related to azure connections, including
    /// current environment, subscriptions, etc.
    /// </summary>
    public class WindowsAzureProfile
    {
        // Internal state

        // Store - responsible for loading and saving a profile to a particular location
        private readonly IProfileStore profileStore;

        // Token provider - talks to Active Directory to get access tokens
        public ITokenProvider TokenProvider { get; set; }

        // Azure environments
        private readonly Dictionary<string, WindowsAzureEnvironment> environments = new Dictionary<string, WindowsAzureEnvironment>(
            WindowsAzureEnvironment.PublicEnvironments, StringComparer.OrdinalIgnoreCase);

        // And subscriptions
        private readonly List<WindowsAzureSubscription> subscriptions = new List<WindowsAzureSubscription>();

        // Func used to create the default instance
        private static readonly Func<WindowsAzureProfile> defaultCreator =
            () => new WindowsAzureProfile(new PowershellDefaultProfileStore());

        // Singleton instance management
        // The default profile
        private static readonly Lazy<WindowsAzureProfile> defaultInstance =
            new Lazy<WindowsAzureProfile>(defaultCreator);

        // The current profile
        private static WindowsAzureProfile currentInstance;

        public WindowsAzureProfile(IProfileStore profileStore)
            : this(profileStore, new AdalTokenProvider())
        {
            
        }

        /// <summary>
        /// Create an instance of <see cref="WindowsAzureProfile"/> that
        /// stores data in the given store.
        /// </summary>
        /// <param name="profileStore">Data store to read and write from.</param>
        /// <param name="tokenProvider">Token provider used to look up Active Directory tokens</param>
        public WindowsAzureProfile(IProfileStore profileStore, ITokenProvider tokenProvider)
        {
            this.profileStore = profileStore;
            this.TokenProvider = tokenProvider;
            Load();
        }

        /// <summary>
        /// The default profile - this always points to the default
        /// configuration store.
        /// </summary>
        public static WindowsAzureProfile DefaultInstance
        {
            get { return defaultInstance.Value; }
        }

        /// <summary>
        /// The default instance shared across the process.
        /// </summary>
        public static WindowsAzureProfile Instance
        {
            get
            {
                return currentInstance ?? DefaultInstance;
            }

            set { currentInstance = value; }
        }

        /// <summary>
        /// Reset the default instance, used when the instance has been replaced for testing.
        /// </summary>
        public static void ResetInstance()
        {
            currentInstance = null;
        }

        /// <summary>
        /// Destroy any saved data. Deletes the data store and removes any
        /// certificates from the certificate store.
        /// </summary>
        public void DestroyData()
        {
            foreach (var s in subscriptions)
            {
                if (s != null)
                {
                    WindowsAzureCertificate.DeleteFromStore(s.Certificate);
                }
            }
            profileStore.DestroyData();
        }

        //
        // Azure environments
        //

        public IDictionary<string, WindowsAzureEnvironment> Environments
        {
            get { return new Dictionary<string, WindowsAzureEnvironment>(environments, StringComparer.OrdinalIgnoreCase); }
        }

        private WindowsAzureEnvironment currentEnvironment;

        public WindowsAzureEnvironment CurrentEnvironment
        {
            get
            {
                if (currentEnvironment == null)
                {
                    currentEnvironment = environments[EnvironmentName.AzureCloud];
                }
                return currentEnvironment;
            }

            set
            {
                if (!environments.ContainsKey(value.Name))
                {
                    AddEnvironment(value);
                }
                currentEnvironment = environments[value.Name];

                Save();
            }
        }

        public void AddEnvironment(WindowsAzureEnvironment newEnvironment)
        {
            if (environments.ContainsKey(newEnvironment.Name))
            {
                throw new InvalidOperationException(string.Format(Resources.EnvironmentExists, newEnvironment.Name));
            }

            environments[newEnvironment.Name] = newEnvironment;

            Save();
        }

        public void UpdateEnvironment(WindowsAzureEnvironment newEnvironment)
        {
            GuardEnvironmentExistsAndNonPublic(newEnvironment.Name);
            environments[newEnvironment.Name] = newEnvironment;
            Save();
        }

        public void RemoveEnvironment(string name)
        {
            GuardEnvironmentExistsAndNonPublic(name);
            environments.Remove(name);
            Save();
        }

        private void GuardEnvironmentExistsAndNonPublic(string name)
        {
            if (IsPublicEnvironment(name))
            {
                throw new InvalidOperationException(string.Format(Resources.ChangePublicEnvironmentMessage, name));
            }
            if (!environments.ContainsKey(name))
            {
                throw new KeyNotFoundException(string.Format(Resources.EnvironmentNotFound, name));
            }
        }

        //
        // Subscriptions
        //

        public IList<WindowsAzureSubscription> Subscriptions
        {
            get
            {
                return new List<WindowsAzureSubscription>(subscriptions);
            }
        }

        private WindowsAzureSubscription currentSubscription;

        public WindowsAzureSubscription CurrentSubscription
        {
            get
            {
                return currentSubscription ?? DefaultSubscription;
            }

            set { currentSubscription = value; }
        }

        public bool CurrentSubscriptionIsSet
        {
            get { return currentSubscription != null; }
        }

        public WindowsAzureSubscription DefaultSubscription
        {
            get { return subscriptions.FirstOrDefault(s => s.IsDefault); }
        }

        public void AddSubscription(WindowsAzureSubscription s)
        {
            if (subscriptions.Contains(s) ||
                subscriptions.Any(es => string.Compare(es.SubscriptionName, s.SubscriptionName, StringComparison.OrdinalIgnoreCase) == 0))
            {
                throw new ArgumentException(
                    string.Format(Resources.SubscriptionAlreadyExists, s.SubscriptionName));
            }

            AddSubscriptionInternal(s);
            if (s.IsDefault)
            {
                UpdateDefaultSubscription(s);
            }

            Save();
        }

        public void RemoveSubscription(WindowsAzureSubscription s)
        {
            if (s == currentSubscription)
            {
                currentSubscription = null;
            }

            subscriptions.Remove(s);

            if (s.IsDefault && subscriptions.Count > 0)
            {
                subscriptions[0].IsDefault = true;
            }

            Save();
        }

        public void UpdateSubscription(WindowsAzureSubscription s)
        {
            if (!subscriptions.Contains(s))
            {
                throw new ArgumentException(
                    string.Format(Resources.CannotUpdateUnknownSubscription, 
                        s.SubscriptionName, s.SubscriptionId));
            }

            if (s.IsDefault)
            {
                UpdateDefaultSubscription(s);
            }

            Save();
        }

        private void UpdateDefaultSubscription(WindowsAzureSubscription newDefault)
        {
            foreach (var subs in subscriptions.Where(s => s != newDefault))
            {
                subs.IsDefault = false;
            }
        }

        public void ImportPublishSettings(string fileName)
        {
            using (var s = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                ImportPublishSettings(s);
            }
        }

        public void ImportPublishSettings(Stream stream)
        {
            List<WindowsAzureSubscription> newSubscriptions = PublishSettingsImporter.Import(stream).ToList();
            AddSubscriptions(newSubscriptions);
            Save();
        }

        /// <summary>
        /// Log in to the given environment, and download and add subscriptions
        /// for the given account in that environment.
        /// </summary>
        /// <param name="environment">environment that the subscription is in.</param>
        public string AddAccounts(WindowsAzureEnvironment environment)
        {
            environment = environment ?? CurrentEnvironment;
            var newSubscriptions = environment.AddAccount(TokenProvider).ToList();
            AddSubscriptions(newSubscriptions);
            Save();
            return newSubscriptions[0].ActiveDirectoryUserId;
        }

        private void AddSubscriptions(List<WindowsAzureSubscription> newSubscriptions)
        {
            if (DefaultSubscription == null && newSubscriptions.Count > 0)
            {
                newSubscriptions[0].IsDefault = true;
            }

            foreach(var newSubscription in newSubscriptions)
            {
                var existingSubscription =
                    subscriptions.FirstOrDefault(s => s.SubscriptionId == newSubscription.SubscriptionId);
                if (existingSubscription != null)
                {
                    existingSubscription.Update(newSubscription);
                }
                else
                {
                    AddSubscriptionInternal(newSubscription);
                }
            }
        }

        public void Load()
        {
            if (profileStore != null)
            {
                var profileData = profileStore.Load();
                if (profileData != null)
                {
                    LoadEnvironmentData(profileData);
                    LoadSubscriptionData(profileData);
                }
            }
        }

        private void MigrateExistingEnvironments(string environmentName)
        {
            WindowsAzureEnvironment azureEnvironment = environments[environmentName];
            if (string.IsNullOrEmpty(azureEnvironment.ResourceManagerEndpoint))
            {
                azureEnvironment.ResourceManagerEndpoint = environments[EnvironmentName.AzureCloud].ResourceManagerEndpoint;
            }
            if (string.IsNullOrEmpty(azureEnvironment.GalleryEndpoint))
            {
                azureEnvironment.GalleryEndpoint = environments[EnvironmentName.AzureCloud].GalleryEndpoint;
            }
        }

        public void Save()
        {
            var profileData = new ProfileData();
            SetEnvironmentData(profileData);
            SetSubscriptionData(profileData);
            profileStore.Save(profileData);
        }

        private static bool IsPublicEnvironment(string name)
        {
            return
                WindowsAzureEnvironment.PublicEnvironments.Keys.Any(
                    publicName => string.Compare(publicName, name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private void SetEnvironmentData(ProfileData data)
        {
            data.DefaultEnvironmentName = CurrentEnvironment.Name;
            data.Environments = Environments.Values
                .Where(e => !IsPublicEnvironment(e.Name))
                .Select(e => new AzureEnvironmentData(e)).ToList();
        }

        private void LoadEnvironmentData(ProfileData data)
        {
            if (data.Environments != null)
            {
                foreach (var e in data.Environments.Select(e => e.ToAzureEnvironment()))
                {
                    environments[e.Name] = e;
                    MigrateExistingEnvironments(e.Name);
                }
                if (environments.ContainsKey(data.DefaultEnvironmentName))
                {
                    currentEnvironment = environments[data.DefaultEnvironmentName];
                }
            }
        }

        private void SetSubscriptionData(ProfileData data)
        {
            data.Subscriptions = Subscriptions.Select(s => new AzureSubscriptionData(s)).ToList();
        }

        private void LoadSubscriptionData(ProfileData data)
        {
            if (data.Subscriptions != null)
            {
                foreach (var s in data.Subscriptions)
                {
                    var newSub = s.ToAzureSubscription();
                    AddSubscriptionInternal(newSub);
                }
            }
        }

        private void AddSubscriptionInternal(WindowsAzureSubscription subscription)
        {
            subscription.TokenProvider = TokenProvider;
            subscription.Save = Save;
            subscriptions.Add(subscription);
        }
    }
}
