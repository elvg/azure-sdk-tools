# ----------------------------------------------------------------------------------
#
# Copyright Microsoft Corporation
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# http://www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
# ----------------------------------------------------------------------------------

<#
.SYNOPSIS
Tests creating new simple resource group.
#>
function Test-CreatesNewSimpleResourceGroup
{
	# Setup
	$rgname = Get-ResourceGroupName
	$location = Get-ProviderLocation ResourceManagement

	try 
	{
		# Test
		$actual = New-AzureResourceGroup -Name $rgname -Location $location
		$expected = Get-AzureResourceGroup -Name $rgname

		# Assert
		Assert-AreEqual $expected.Name $actual.Name	
	}
	finally
	{
		# Cleanup
		Clean-ResourceGroup $rgname
	}
}

<#
.SYNOPSIS
Tests creating new simple resource group and deleting it via piping.
#>
function Test-CreatesAndRemoveResourceGroupViaPiping
{
	# Setup
	$rgname1 = Get-ResourceGroupName
	$rgname2 = Get-ResourceGroupName
	$location = Get-ProviderLocation ResourceManagement

	# Test
	New-AzureResourceGroup -Name $rgname1 -Location $location
	New-AzureResourceGroup -Name $rgname2 -Location $location
		
	Get-AzureResourceGroup | where {$_.ResourceGroupName -eq $rgname1 -or $_.ResourceGroupName -eq $rgname2} | Remove-AzureResourceGroup -Force

	# Assert
	Assert-Throws { Get-AzureResourceGroup -Name $rgname1 } "Provided resource group does not exist."
	Assert-Throws { Get-AzureResourceGroup -Name $rgname2 } "Provided resource group does not exist."
}

<#
.SYNOPSIS
Tests getting non-existing resource group.
#>
function Test-GetNonExistingResourceGroup
{
	# Setup
	$rgname = Get-ResourceGroupName

	Assert-Throws { Get-AzureResourceGroup -Name $rgname } "Provided resource group does not exist."
}

<#
.SYNOPSIS
Negative test. New resource group in non-existing location throws error.
#>
function Test-NewResourceGroupInNonExistingLocation
{
	# Setup
	$rgname = Get-ResourceGroupName

	Assert-Throws { New-AzureResourceGroup -Name $rgname -Location 'non-existing' }
}

<#
.SYNOPSIS
Negative test. New resource group in non-existing location throws error.
#>
function Test-RemoveNonExistingResourceGroup
{
	# Setup
	$rgname = Get-ResourceGroupName

	Assert-Throws { Clean-ResourceGroup $rgname } "Provided resource group does not exist."
}