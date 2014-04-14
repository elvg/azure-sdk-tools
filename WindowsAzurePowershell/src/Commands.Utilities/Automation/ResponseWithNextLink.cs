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
    using System.Collections.Generic;

    using Microsoft.Azure.Management.Automation.Models;

    /// <summary>
    /// The response with NextLink.
    /// </summary>
    /// <typeparam name="T">
    /// </typeparam>
    public class ResponseWithNextLink<T>
    {
        /// <summary>
        /// The operation response with next link.
        /// </summary>
        private readonly OperationResponseWithNextLink operationResponseWithNextLink;

        /// <summary>
        /// The automation management models.
        /// </summary>
        private readonly IEnumerable<T> automationManagementModels;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseWithNextLink{T}"/> class.
        /// </summary>
        /// <param name="operationResponseWithNextLink">
        /// The operation response with next link.
        /// </param>
        /// <param name="automationManagementModels">
        /// The automation management models.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Argument Null Exception
        /// </exception>
        public ResponseWithNextLink(OperationResponseWithNextLink operationResponseWithNextLink, IEnumerable<T> automationManagementModels)
        {
            Requires.Argument("operationResponseWithNextLink", operationResponseWithNextLink);
            Requires.Argument("automationManagementModels", automationManagementModels);
            
            this.operationResponseWithNextLink = operationResponseWithNextLink;
            this.automationManagementModels = automationManagementModels;
        }

        /// <summary>
        /// Gets the automation management models.
        /// </summary>
        public IEnumerable<T> AutomationManagementModels
        {
            get
            {
                return this.automationManagementModels;
            }
        }

        /// <summary>
        /// Gets the next link.
        /// </summary>
        public string NextLink
        {
            get
            {
                return this.operationResponseWithNextLink.NextLink;
            }
        }
    }
}