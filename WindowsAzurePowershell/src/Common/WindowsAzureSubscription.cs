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
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using Authentication;
    using Management;
    using WindowsAzure.Common;
    using Commands.Common.Properties;
    using Commands.Common;
    using Microsoft.Azure.Management.Resources;
    using Microsoft.Azure.Management.Resources.Models;

    /// <summary>
    /// Representation of a subscription in memory
    /// </summary>
    public class WindowsAzureSubscription
    {
        private bool registerProvidersOnCreateClient;

        public WindowsAzureSubscription() : this(true)
        { }

        public WindowsAzureSubscription(bool registerProviders)
        {
            registerProvidersOnCreateClient = registerProviders;
        }

        public string SubscriptionName { get; set; }
        
        public string SubscriptionId { get; set; }
        
        public Uri ServiceEndpoint { get; set; }
        
        public Uri ResourceManagerEndpoint { get; set; }

        public Uri GalleryEndpoint { get; set; }

        public string ActiveDirectoryEndpoint { get; set; }

        public string ActiveDirectoryTenantId { get; set; }

        public bool IsDefault { get; set; }
        
        public X509Certificate2 Certificate { get; set; }
        internal object currentCloudStorageAccount;
        internal string currentStorageAccountName;

        private readonly List<string> registeredResourceProviders = new List<string>();

        internal List<string> RegisteredResourceProviders
        {
            get { return registeredResourceProviders; }
        }

        internal List<Provider> ResourceManagerProviders { get; set; }

        /// <summary>
        /// Delegate used to trigger profile to save itself, used
        /// when cached list of resource providers is updated.
        /// </summary>
        internal Action Save { get; set; }

        /// <summary>
        /// Event that's trigged when a new client has been created.
        /// </summary>
        public static event EventHandler<ClientCreatedArgs> OnClientCreated;

        public string CurrentStorageAccountName
        {
            get { return currentStorageAccountName; }
            set
            {
                if (currentStorageAccountName != value)
                {
                    currentStorageAccountName = value;
                }
            }
        }

        // Access token / account name for Active Directory
        public string ActiveDirectoryUserId { get; set; }
        public ITokenProvider TokenProvider { get; set; }

        private IAccessToken accessToken;
        
        /// <summary>
        /// Set the access token to use for authentication
        /// when creating azure management clients from this
        /// subscription. This also updates the <see cref="ActiveDirectoryUserId"/> field.
        /// </summary>
        /// <param name="token">The access token to use. If null,
        /// clears out the token and the active directory login information.</param>
        public void SetAccessToken(IAccessToken token)
        {
            if (token != null)
            {
                ActiveDirectoryUserId = token.UserId;
            }
            else
            {
                ActiveDirectoryUserId = null;
            }
            accessToken = token;
        }

        private SubscriptionCloudCredentials CreateCredentials()
        {
            if (accessToken == null && ActiveDirectoryUserId == null)
            {
                return new CertificateCloudCredentials(SubscriptionId, Certificate);
            }
            if (accessToken == null)
            {
                accessToken = TokenProvider.GetCachedToken(this, ActiveDirectoryUserId);
            }
            return new AccessTokenCredential(SubscriptionId, accessToken);
        }

        /// <summary>
        /// Update the contents of this subscription with the data from the
        /// given new subscription. Does a merge of the data, leaving for example
        /// existing certificate if subscription is also download from azure AD.
        /// </summary>
        /// <param name="newSubscription">Subscription data to update from</param>
        public void Update(WindowsAzureSubscription newSubscription)
        {
            // AD Data - if present in new subscription, take it else preserve existing
            ActiveDirectoryEndpoint = newSubscription.ActiveDirectoryEndpoint ??
                ActiveDirectoryEndpoint;
            ActiveDirectoryTenantId = newSubscription.ActiveDirectoryTenantId ??
                ActiveDirectoryTenantId;
            ActiveDirectoryUserId = newSubscription.ActiveDirectoryUserId ??
                ActiveDirectoryUserId;

            // Wipe out current access token - it will be reloaded from 
            // token provider when needed to get new access/refresh tokens
            accessToken = null;

            // Certificate - if present in new take it, else preserve
            Certificate = newSubscription.Certificate ??
                Certificate;

            // One of them is the default
            IsDefault = newSubscription.IsDefault || IsDefault;

            // And overwrite the rest
            SubscriptionId = newSubscription.SubscriptionId;
            ServiceEndpoint = newSubscription.ServiceEndpoint;
            ResourceManagerEndpoint = newSubscription.ResourceManagerEndpoint;
            SubscriptionName = newSubscription.SubscriptionName;
            GalleryEndpoint = newSubscription.GalleryEndpoint;
        }

        /// <summary>
        /// Create a service management client for this subscription,
        /// with appropriate credentials supplied.
        /// </summary>
        /// <typeparam name="TClient">Type of client to create, must be derived from <see cref="ServiceClient{T}"/></typeparam>
        /// <returns>The service client instance</returns>
        public TClient CreateClient<TClient>() where TClient : ServiceClient<TClient>
        {
            return CreateClientFromEndpoint<TClient>(ServiceEndpoint);
        }

        /// <summary>
        /// Create a service management client for this subscription,
        /// with appropriate credentials supplied.
        /// </summary>
        /// <typeparam name="TClient">Type of client to create, must be derived from <see cref="ServiceClient{T}"/></typeparam>
        /// <returns>The service client instance</returns>
        public TClient CreateGalleryClientFromGalleryEndpoint<TClient>() where TClient : ServiceClient<TClient>
        {
            if (GalleryEndpoint == null)
            {
                throw new ArgumentException(Resources.InvalidSubscriptionState);
            }
            return CreateClientFromEndpoint<TClient>(GalleryEndpoint, false);
        }

        public TClient CreateClientFromResourceManagerEndpoint<TClient>() where TClient : ServiceClient<TClient>
        {
            if (ResourceManagerEndpoint == null)
            {
                throw new ArgumentException(Resources.InvalidSubscriptionState);
            }
            return CreateClientFromEndpoint<TClient>(ResourceManagerEndpoint);
        }

        public TClient CreateClientFromEndpoint<TClient>(Uri endpoint) where TClient : ServiceClient<TClient>
        {
            return CreateClientFromEndpoint<TClient>(endpoint, registerProvidersOnCreateClient);
        }

        public TClient CreateClientFromEndpoint<TClient>(Uri endpoint, bool registerProviders) where TClient : ServiceClient<TClient>
        {
            var credential = CreateCredentials();

            if (registerProviders)
            {
                RegisterRequiredResourceProviders<TClient>(credential);
            }

            var constructor = typeof(TClient).GetConstructor(new[] { typeof(SubscriptionCloudCredentials), typeof(Uri) });
            if (constructor == null)
            {
                throw new InvalidOperationException(string.Format(Resources.InvalidManagementClientType, typeof(TClient).Name));
            }

            TClient client = (TClient)constructor.Invoke(new object[] { credential, endpoint });
            client.UserAgent.Add(ApiConstants.UserAgentValue);
            EventHandler<ClientCreatedArgs> clientCreatedHandler = OnClientCreated;
            if (clientCreatedHandler != null)
            {
                ClientCreatedArgs args = new ClientCreatedArgs { CreatedClient = client, ClientType = typeof(TClient) };
                clientCreatedHandler(this, args);
                client = (TClient)args.CreatedClient;
            }

            // Add the logging handler
            var withHandlerMethod = typeof(TClient).GetMethod("WithHandler", new[] { typeof(DelegatingHandler) });
            TClient finalClient = (TClient)withHandlerMethod.Invoke(client, new object[] { new HttpRestCallLogger() });
            client.Dispose();

            return finalClient;
        }

        private void RegisterRequiredResourceProviders<T>(SubscriptionCloudCredentials credentials) where T : ServiceClient<T>
        {
            RegisterServiceManagementProviders<T>(credentials);

            if (ResourceManagerEndpoint != null)
            {
                RegisterResourceManagerProviders<T>(credentials);
            }
        }

        /// <summary>
        /// Registers resource providers for Sparta.
        /// </summary>
        /// <typeparam name="T">The client type</typeparam>
        /// <param name="credentials">The subscription credentials</param>
        private void RegisterResourceManagerProviders<T>(SubscriptionCloudCredentials credentials) where T : ServiceClient<T>
        {
            List<string> requiredProviders = RequiredResourceLookup.RequiredProvidersForResourceManager<T>()
                .Where(p => !RegisteredResourceProviders.Contains(p)).ToList();
            if (requiredProviders.Count > 0)
            {
                using (IResourceManagementClient client = new ResourceManagementClient(credentials, ResourceManagerEndpoint))
                {
                    foreach (string provider in requiredProviders)
                    {
                        try
                        {
                            client.Providers.Register(provider);
                            RegisteredResourceProviders.Add(provider);
                        }
                        catch
                        {
                            // Ignore this as the user may not have access to Sparta endpoint or the provider is already registered
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Registers resource providers for RDFE.
        /// </summary>
        /// <typeparam name="T">The client type</typeparam>
        /// <param name="credentials">The subscription credentials</param>
        private void RegisterServiceManagementProviders<T>(SubscriptionCloudCredentials credentials) where T : ServiceClient<T>
        {
            var requiredProviders = RequiredResourceLookup.RequiredProvidersForServiceManagement<T>();
            var unregisteredProviders = requiredProviders.Where(p => !RegisteredResourceProviders.Contains(p)).ToList();

            if (unregisteredProviders.Count > 0)
            {
                using (var client = new ManagementClient(credentials, ServiceEndpoint))
                {
                    foreach (var provider in unregisteredProviders)
                    {
                        try
                        {
                            client.Subscriptions.RegisterResource(provider);
                        }
                        catch (CloudException ex)
                        {
                            if (ex.Response.StatusCode != HttpStatusCode.Conflict && ex.Response.StatusCode != HttpStatusCode.NotFound)
                            {
                                // Conflict means already registered, that's OK.
                                // NotFound means there is no registration support, like Windows Azure Pack.
                                // Otherwise it's a failure.
                                throw;
                            }
                        }
                        RegisteredResourceProviders.Add(provider);
                    }
                    Save();
                }
            }
        }
    }
}
