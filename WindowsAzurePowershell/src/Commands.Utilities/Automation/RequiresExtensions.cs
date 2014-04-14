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
    using System.Globalization;
    using System.Text.RegularExpressions;

    internal static class RequiresExtensions
    {
        #region Constants

        private const string AccountNameValidator = "^[A-Za-z][-A-Za-z0-9]{4,48}[A-Za-z0-9]$";

        private const string RunbookNameValidator = "^[A-Za-z][-A-Za-z0-9_]{0,63}$";

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Validates that the provided automation account name is valid.
        /// </summary>
        /// <param name="argument">
        /// The argument.
        /// </param>
        /// <returns>
        /// The <see cref="Requires.ArgumentRequirements{T}"/>.
        /// </returns>
        public static Requires.ArgumentRequirements<string> ValidAutomationAccountName(this Requires.ArgumentRequirements<string> argument)
        {
            Requires.Argument(argument.Name, argument.Value).NotNull();

            string stringValue = argument.Value;

            if (!new Regex(AccountNameValidator).IsMatch(stringValue))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.InvalidAutomationAccountName,
                        argument.Name,
                        argument.Value));
            }

            return argument;
        }

        /// <summary>
        /// Validates that the provided automation account name is valid.
        /// </summary>
        /// <param name="argument">
        /// The argument.
        /// </param>
        /// <returns>
        /// The <see cref="Requires.ArgumentRequirements{T}"/>.
        /// </returns>
        public static Requires.ArgumentRequirements<string> ValidRunbookName(this Requires.ArgumentRequirements<string> argument)
        {
            Requires.Argument(argument.Name, argument.Value).NotNull();

            string stringValue = argument.Value;

            if (!new Regex(RunbookNameValidator).IsMatch(stringValue))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture, Resources.InvalidRunbookName, argument.Name, argument.Value));
            }

            return argument;
        }

        #endregion
    }
}
