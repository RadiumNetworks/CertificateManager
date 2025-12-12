<#
 .SYNOPSIS
 Creates a connection to a domaincontroller and gets some required informations about the forest and the connection

 .DESCRIPTION
 The 

 .PARAMETER LDAPPort
 389 Domain
 3268 Global Catalog

 .PARAMETER ReferralChasing
 Can be ALL, External, None, Subordinate

 .EXAMPLE
 $Connection = New-ADConnection 
 
 .NOTES
 This Sample Code is provided for the purpose of illustration only 
 and is not intended to be used in a production environment.  
 THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
 EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY 
 AND/OR FITNESS FOR A PARTICULAR PURPOSE.  
 We grant You a nonexclusive, royalty-free right to use and modify the Sample Code 
 and to reproduce and distribute the object code form of the Sample Code, 
 provided that You agree: 
 (i) to not use Our name, logo, 
 or trademarks to market Your software product in which the Sample Code is embedded; 
 (ii) to include a valid copyright notice on Your software product in which the Sample Code is embedded; 
 and 
 (iii) to indemnify, hold harmless, and defend Us and Our suppliers from and against any claims or lawsuits, 
 including attorneys’ fees, that arise or result from the use or distribution of the Sample Code.
 
#>
function New-ADConnection 
{
    [CmdletBinding()]
	param(
        $LDAPPort = 389,
        $ReferralChasing = "none"
	)
	
	$null = [System.Reflection.Assembly]::LoadWithPartialName("System.DirectoryServices.Protocols")
	
	#DCLocator
	$DirectoryCtx = New-Object System.DirectoryServices.ActiveDirectory.DirectoryContext Domain
	$Site = [System.DirectoryServices.ActiveDirectory.ActiveDirectorySite]::GetComputerSite().Name
	#$LocatorOptions = [System.DirectoryServices.ActiveDirectory.LocatorOptions]::ForceRediscovery -bor [System.DirectoryServices.ActiveDirectory.LocatorOptions]::WriteableRequired
    $LocatorOptions = [System.DirectoryServices.ActiveDirectory.LocatorOptions]::WriteableRequired
	if ($Site)
	{
		try 
        { 
            $DC = [System.DirectoryServices.ActiveDirectory.DomainController]::FindOne($DirectoryCtx,$Site,$LocatorOptions)
        }
        catch [System.DirectoryServices.ActiveDirectory.ActiveDirectoryObjectNotFoundException]
        {
            $DC = [System.DirectoryServices.ActiveDirectory.DomainController]::FindOne($DirectoryCtx,$LocatorOptions)
        }
	}
	else
	{
		$DC = [System.DirectoryServices.ActiveDirectory.DomainController]::FindOne($DirectoryCtx,$LocatorOptions)
	}
	
	$ldapDirectoryID = New-Object System.DirectoryServices.Protocols.LdapDirectoryIdentifier $DC.Name, $LDAPPort, $true, $false
	$LDAPConnection = New-Object System.DirectoryServices.Protocols.LdapConnection $ldapDirectoryID
	
	#Get NamingContexts
	$LDAPFilter = "(objectclass=*)"
	$SearchScope = [System.DirectoryServices.Protocols.SearchScope]::Base
	$SearchRequest = New-Object System.DirectoryServices.Protocols.SearchRequest "",$LDAPFilter,$SearchScope,$null
	$SearchResponse = $LDAPConnection.SendRequest($SearchRequest)
	
	$dNC = $SearchResponse.Entries[0].Attributes["defaultnamingcontext"][0]
	$cNC = $SearchResponse.Entries[0].Attributes["configurationnamingcontext"][0]
	$sNC = $SearchResponse.Entries[0].Attributes["schemanamingcontext"][0]
	
	$Site = $DC.SiteName
	$DCNetBiosName = ($DC.Name -split "\.")[0]
	$QueryPolicy = "CN=Default Query Policy,CN=Query-Policies,CN=Directory Service,CN=Windows NT,CN=Services,$cNC"

	#Query Policy Linked to Server
	$LDAPFilter = "(objectclass=ntdsdsa)"
	$SearchScope = [System.DirectoryServices.Protocols.SearchScope]::Subtree
	$SearchRoot = "CN=NTDS Settings,CN=$DCNetBiosName,CN=Servers,CN=$Site,CN=Sites,$cNC"
	$SearchRequest = New-Object System.DirectoryServices.Protocols.SearchRequest $SearchRoot,$LDAPFilter,$SearchScope,$null
	[void]$SearchRequest.Attributes.Add("Query-Policy-Object")
	$SearchResponse = $LDAPConnection.SendRequest($SearchRequest)
	
	if ($Searchresponse.entries[0].Attributes."Query-Policy-Object")
	{ 
		$QueryPolicy = $Searchresponse.entries[0].Attributes."Query-Policy-Object"
	}
	else
	{
		#Query Policy Linked to Site
		$LDAPFilter = "(objectclass=ntdsdsa)"
		$SearchScope = [System.DirectoryServices.Protocols.SearchScope]::Subtree
		$SearchRoot = "CN=NTDS Site Settings,CN=$Site,CN=Sites,$cNC"
		$SearchRequest = New-Object System.DirectoryServices.Protocols.SearchRequest $SearchRoot,$LDAPFilter,$SearchScope,$null
		[void]$SearchRequest.Attributes.Add("Query-Policy-Object")
		$SearchResponse = $LDAPConnection.SendRequest($SearchRequest)

		if ($Searchresponse.entries[0].Attributes."Query-Policy-Object")
		{
			$QueryPolicy = $Searchresponse.entries[0].Attributes."Query-Policy-Object"
		}
	}

	#Default Query Policy
	$LDAPConnection = New-Object System.DirectoryServices.Protocols.LdapConnection $DC.Name
	$LDAPFilter = "(objectclass=queryPolicy)"
	$SearchScope = [System.DirectoryServices.Protocols.SearchScope]::Subtree
	$SearchRoot = $QueryPolicy
	$SearchRequest = New-Object System.DirectoryServices.Protocols.SearchRequest $SearchRoot,$LDAPFilter,$SearchScope,$null
	$SearchResponse = $LDAPConnection.SendRequest($SearchRequest)
	
	$MaxPageSize = $SearchResponse.entries[0].attributes.ldapadminlimits | ForEach-Object { -join [Char[]]$_ } | ForEach-Object { if ($_ -like "MaxPageSize*")  {($_ -split "=")[1]} } 
	
	$LDAPConnection = New-Object System.DirectoryServices.Protocols.LdapConnection  $DC.Name
	$LDAPConnection.SessionOptions.ReferralChasing = [System.DirectoryServices.Protocols.ReferralChasingOptions]::$ReferralChasing
	
	$properties = @{defaultNC=$dNC;schemaNC=$sNC;configurationNC=$cNC;LDAPConnection=$LDAPConnection;DomainController=$DC;PageSize = $MaxPageSize}
	$result = New-Object -Type PSObject -Property $properties
	$result
}

$ADConnectionInfo = New-ADConnection 



